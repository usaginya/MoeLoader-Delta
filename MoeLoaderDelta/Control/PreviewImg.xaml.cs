using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MoeLoaderDelta.Control
{
    /// <summary>
    /// PreviewImg.xaml 的交互逻辑
    /// by YIU
    /// </summary>
    public partial class PreviewImg : UserControl
    {

        //======== 私有变量 =========
        //预览窗口
        private PreviewWnd prew;
        //图片信息结构
        private Img img;
        //网络请求组
        private Dictionary<int, HttpWebRequest> reqs = new Dictionary<int, HttpWebRequest>();
        private Stream strs;
        //图片是否载入完成
        private bool imgloaded;
        //是否缩放
        private bool iszoom;
        //预览图类型
        private string imgType;
        //用于静态图片格式转化
        Dictionary<string, ImageFormat> imgf = new Dictionary<string, ImageFormat>();

        #region ==== 封装 =======
        public Dictionary<int, HttpWebRequest> Reqs
        {
            get
            {
                return reqs;
            }

            set
            {
                reqs = value;
            }
        }

        public bool ImgLoaded
        {
            get
            {
                return imgloaded;
            }
        }

        public bool isZoom
        {
            get
            {
                return iszoom;
            }
            set
            {
                iszoom = value;
            }
        }

        /// <summary>
        /// 预览图类型
        /// bmp, jpg, png, gif, webm, mpeg, zip, rar, 7z
        /// </summary>
        public string ImgType
        {
            get
            {
                return imgType;
            }
        }

        public Stream Strs
        {
            get
            {
                return strs;
            }

            set
            {
                strs = value;
            }
        }
        #endregion

        //======= 委托 =======
        //进度条数值
        private delegate void ProgressBarSetter(double value);

        public PreviewImg(PreviewWnd prew, Img img)
        {
            this.InitializeComponent();
            this.prew = prew;
            this.img = img;
            //格式初始化
            imgf.Add("bmp", ImageFormat.Bmp);
            imgf.Add("jpg", ImageFormat.Jpeg);
            imgf.Add("png", ImageFormat.Png);
        }


        /// <summary>
        /// 下载图片
        /// </summary>
        public void DownloadImg(int id, string url, string needReferer)
        {
            try
            {
                #region 创建请求数据
                SessionClient ss = new SessionClient();
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Proxy = MainWindow.WebProxy;
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                if (needReferer != null)
                    req.Referer = needReferer;
                req.AllowAutoRedirect = true;
                req.CookieContainer = ss.CookieContainer;

                //将请求加入请求组
                reqs.Add(id, req);
                #endregion

                //异步下载开始
                req.BeginGetResponse(new AsyncCallback(RespCallback), new KeyValuePair<int, HttpWebRequest>(id, req));
            }
            catch (Exception ex)
            {
                Program.Log(ex, "Download sample failed");
                StopLoadImg(id, true, "创建下载失败");
            }
        }

        /// <summary>
        /// 异步下载
        /// </summary>
        /// <param name="req"></param>
        private void RespCallback(IAsyncResult req)
        {
            KeyValuePair<int, HttpWebRequest> re = (KeyValuePair<int, HttpWebRequest>)(req.AsyncState);
            try
            {
                Dispatcher.Invoke(new UIdelegate(delegate (object sender)
                {
                    try
                    {
                        //取响应数据
                        HttpWebResponse res = (HttpWebResponse)re.Value.EndGetResponse(req);
                        string resae = res.Headers[HttpResponseHeader.ContentEncoding];
                        Stream str = res.GetResponseStream();

                        //响应长度
                        double reslength = res.ContentLength, restmplength = 0;


                        //获取数据更新进度条
                        ThreadPool.QueueUserWorkItem((obj) =>
                        {
                            //缓冲块长度
                            byte[] buffer = new byte[1024];
                            //读到的字节长度
                            int realReadLen = str.Read(buffer, 0, 1024);
                            //进度条字节进度
                            long progressBarValue = 0;
                            double progressSetValue = 0;
                            //内存流字节组
                            byte[] data = null;
                            MemoryStream ms = new MemoryStream();

                            //写流数据并更新进度条
                            while (realReadLen > 0)
                            {
                                ms.Write(buffer, 0, realReadLen);
                                progressBarValue += realReadLen;
                                if (reslength < 1)
                                {
                                    if (restmplength < progressBarValue)
                                        restmplength = progressBarValue * 2;
                                    progressSetValue = progressBarValue / restmplength;
                                }
                                else
                                { progressSetValue = progressBarValue / reslength; }

                                pdload.Dispatcher.BeginInvoke(new ProgressBarSetter(SetProgressBar), progressSetValue);
                                try
                                {
                                    realReadLen = str.Read(buffer, 0, 1024);
                                }
                                catch
                                {
                                    Dispatcher.Invoke(new UIdelegate(delegate (object sende) { StopLoadImg(re.Key, "数据中止"); }), "");
                                    return;
                                }
                            }

                            data = ms.ToArray();

                            //解压gzip
                            if (resae != null && resae.Contains("gzip", StringComparison.OrdinalIgnoreCase))
                            {
                                ungzip(ref data);
                            }

                            //将字节组转为流
                            ms = new MemoryStream(data);

                            //读完数据传递图片流并显示
                            Dispatcher.Invoke(new UIdelegate(delegate (object sende) { AssignImg(ms, re.Key); }), "");

                            str.Dispose();
                            str.Close();
                        }, null);
                    }
                    catch (WebException e)
                    {
                        Dispatcher.Invoke(new UIdelegate(delegate (object sende) { StopLoadImg(re.Key, true, "缓冲失败"); }), e);
                    }
                }), this);
            }
            catch (Exception ex2)
            {
                Program.Log(ex2, "Download sample failed");
                Dispatcher.Invoke(new UIdelegate(delegate (object sender) { StopLoadImg(re.Key, true, "下载失败"); }), ex2);
            }
        }

        /// <summary>
        /// 解压gzip
        /// </summary>
        /// <param name="data">字节组</param>
        /// <returns></returns>
        private static byte[] ungzip(ref byte[] data)
        {
            try
            {
                MemoryStream js = new MemoryStream();                       // 解压后的流   
                MemoryStream ms = new MemoryStream(data);                   // 用于解压的流   
                GZipStream g = new GZipStream(ms, CompressionMode.Decompress);
                byte[] buffer = new byte[5120];                                // 5K缓冲区      
                int l = g.Read(buffer, 0, 5120);
                while (l > 0)
                {
                    js.Write(buffer, 0, l);
                    l = g.Read(buffer, 0, 5120);
                }
                data = js.ToArray();
                g.Dispose();
                ms.Dispose();
                js.Dispose();
                g.Close();
                ms.Close();
                js.Close();
                return data;
            }
            catch
            {
                return data;
            }
        }

        /// <summary>
        /// 设进度条进度值
        /// </summary>
        /// <param name="value"></param>
        private void SetProgressBar(double value)
        {
            pdload.Value = value;
        }

        #region 停止加载预览图
        /// <summary>
        /// 停止加载
        /// </summary>
        /// <param name="id"></param>
        /// <param name="Failed">是否失败</param>
        /// <param name="FMsg">失败提示</param>
        public void StopLoadImg(int id, bool Failed, string FMsg)
        {
            try
            {
                //清理请求数据
                if (reqs.ContainsKey(id))
                {
                    if (reqs[id] != null)
                    {
                        reqs[id].Abort();
                        reqs.Remove(id);
                    }
                }

                if (strs != null)
                {
                    strs.Flush();
                    strs.Dispose();
                    strs.Close();
                }
            }
            catch { }
            finally
            {
                pdtext.Text = FMsg;
                pdload.BorderBrush = Failed
                    ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(238, 255, 55, 90))
                    : new SolidColorBrush(System.Windows.Media.Color.FromArgb(238, 255, 210, 0));
                pdload.Background = Failed
                    ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(238, 255, 197, 197))
                    : new SolidColorBrush(System.Windows.Media.Color.FromArgb(238, 255, 237, 192));
            }
        }
        /// <summary>
        /// 停止加载2
        /// </summary>
        /// <param name="id"></param>
        /// <param name="FMsg">失败提示</param>
        public void StopLoadImg(int id, string FMsg)
        {
            StopLoadImg(id, false, FMsg);
        }
        #endregion

        /// <summary>
        /// 显示预览
        /// </summary>
        /// <param name="str">各种Stream</param>
        /// <param name="key">img.ID</param>
        private void AssignImg(Stream str, int key)
        {
            //流下载完毕
            try
            {
                //提取图片规格类型
                string type = GetImgType(str);
                imgType = type;

                //记录Stream
                strs = str;

                //分显示容器
                switch (type)
                {
                    case "bmp":
                    case "jpg":
                    case "png":
                        //静态图片格式转化
                        System.Drawing.Image dimg = System.Drawing.Image.FromStream(str);
                        MemoryStream ms = new MemoryStream();
                        dimg.Save(ms, imgf[type]);
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.StreamSource = new MemoryStream(ms.ToArray()); //ms不能直接用
                        bi.EndInit();
                        ms.Close();

                        //创建静态图片控件
                        Image img = new Image()
                        {
                            Source = bi,
                            Stretch = Stretch.Uniform,
                            SnapsToDevicePixels = true,
                            StretchDirection = StretchDirection.Both,
                            Margin = new Thickness() { Top = 0, Right = 0, Bottom = 0, Left = 0 }
                        };

                        //将预览控件加入布局
                        prewimg.Children.Add(img);
                        break;

                    case "gif":
                        //创建GIF动图控件
                        AnimatedGIF gif = new AnimatedGIF()
                        {
                            GIFSource = str,
                            Stretch = Stretch.Uniform,
                            SnapsToDevicePixels = true,
                            StretchDirection = StretchDirection.Both
                        };

                        //将预览控件加入布局
                        prewimg.Children.Add(gif);
                        break;

                    default:
                        //未支持预览的格式
                        Dispatcher.Invoke(new UIdelegate(delegate (object ss) { StopLoadImg(key, "不支持" + type); }), "");
                        return;
                }

                //选中的预览图显示出来
                if (key == prew.SelectedId)
                {
                    this.Visibility = Visibility.Visible;
                }

                //隐藏进度条
                ProgressPlate.Visibility = Visibility.Hidden;

                ImgZoom(true);
                imgloaded = true;
            }
            catch (Exception ex1)
            {
                Program.Log(ex1, "Read sample img failed");
                Dispatcher.Invoke(new UIdelegate(delegate (object ss) { StopLoadImg(key, true, "读取数据失败"); }), ex1);
            }
        }

        #region 设置图片缩放
        /// <summary>
        /// 设置图片缩放
        /// </summary>
        /// <param name="zoom">true自适应</param>
        /// <param name="begin">首次显示</param>
        public void ImgZoom(bool zoom, bool begin)
        {
            if (imgType == null) return;

            double imgw = 0, imgh = 0;
            AnimatedGIF gifo = null;
            bool isani = false;

            UIElement imgui = prewimg.Children[0];
            //分类型取值
            switch (imgType)
            {
                case "bmp":
                case "jpg":
                case "png":
                    Image img = (Image)imgui;
                    BitmapImage bi = (BitmapImage)img.Source;
                    imgw = bi.PixelWidth;
                    imgh = bi.PixelHeight;
                    break;
                case "gif":
                    AnimatedGIF gif = (AnimatedGIF)imgui;
                    gifo = gif;
                    isani = true;
                    break;
            }

            if (begin)
            {
                if (isani)
                {
                    imgw = prew.Descs[prew.SelectedId].Width;
                    imgh = prew.Descs[prew.SelectedId].Height;
                    if (imgw < 1 || imgh < 1)
                        imgw = imgh = double.NaN;
                }
                if (zoom && imgw > prew.imgGrid.ActualWidth || zoom && imgh > prew.imgGrid.ActualHeight)
                {
                    Width = Height = double.NaN;
                }
                else
                {
                    Width = imgw;
                    Height = imgh;
                    zoom = false;
                }
            }
            else
            {
                if (zoom)
                {
                    Width = Height = double.NaN;
                }
                else if (isani)
                {
                    AnimatedGIF.GetWidthHeight(gifo, ref imgw, ref imgh);
                    Width = imgw;
                    Height = imgh;
                }
                else
                {
                    Width = imgw;
                    Height = imgh;
                }
            }

            isZoom = zoom;
        }

        /// <summary>
        /// 设置图片缩放首次模式自适应
        /// </summary>
        public void ImgZoom(bool begin) { ImgZoom(true, begin); }

        /// <summary>
        /// 设置图片缩放到自适应
        /// </summary>
        public void ImgZoom() { ImgZoom(true, false); }
        #endregion

        /// <summary>
        /// 简单的获取图片类型，失败返回空
        /// </summary>
        /// <param name="str">Stream</param>
        /// <returns>bmp,jpg,png,gif,webm,mpeg,zip,rar,7z</returns>
        private string GetImgType(Stream str)
        {
            if (str == null) return "";

            //由自带对象判断类型
            ImageFormat dwimgformat = System.Drawing.Image.FromStream(str).RawFormat;
            if (dwimgformat.Equals(ImageFormat.Bmp))
            {
                return "bmp";
            }
            else if (dwimgformat.Equals(ImageFormat.Jpeg)
               || dwimgformat.Equals(ImageFormat.Exif))
            {
                return "jpg";
            }
            else if (dwimgformat.Equals(ImageFormat.Png))
            {
                return "png";
            }
            else if (dwimgformat.Equals(ImageFormat.Gif))
            {
                return "gif";
            }

            //如果对象无法判断就取文件头字节判断
            //图片类型特征字节
            Dictionary<string, string> itype = new Dictionary<string, string>();
            itype.Add("bmp", "424D");
            itype.Add("jpg", "FFD8");
            itype.Add("png", "89504E470D0A");
            itype.Add("gif", "47494638");
            itype.Add("webm", "1A45DFA3");
            itype.Add("mpeg", "66747970");
            itype.Add("zip", "504B0304");
            itype.Add("rar", "52617221");
            itype.Add("7z", "377ABCAF271C");

            //取数据头一部分
            byte[] head = DataConverter.LocalStreamToByte(str, 32);
            //找出符合的格式
            foreach (string type in itype.Keys)
            {
                if (DataHelpers.SearchBytes(head, DataConverter.strHexToByte(itype[type])) >= 0)
                {
                    return type;
                }
            }
            return "";
        }

    }
}
