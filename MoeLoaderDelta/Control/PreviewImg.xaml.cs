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
    /// PreviewImg.xaml 的交互邏輯
    /// by YIU
    /// </summary>
    public partial class PreviewImg : UserControl
    {

        //======== 私有變數 =========
        //預覽視窗
        private PreviewWnd prew;
        //圖片訊息結構
        private Img img;
        //網路請求組
        private Dictionary<int, HttpWebRequest> reqs = new Dictionary<int, HttpWebRequest>();
        private Stream strs;
        //圖片是否載入完成
        private bool imgloaded;
        //是否縮放
        private bool iszoom;
        //預覽圖類型
        private string imgType;
        //用於靜態圖片格式轉化
        Dictionary<string, ImageFormat> imgf = new Dictionary<string, ImageFormat>();

        #region ==== 封裝 =======
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
        /// 預覽圖類型
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

        //======= 委託 =======
        //進度條數值
        private delegate void ProgressBarSetter(double value);

        public PreviewImg(PreviewWnd prew, Img img)
        {
            InitializeComponent();
            this.prew = prew;
            this.img = img;
            //格式初始化
            imgf.Add("bmp", ImageFormat.Bmp);
            imgf.Add("jpg", ImageFormat.Jpeg);
            imgf.Add("png", ImageFormat.Png);
        }


        /// <summary>
        /// 下載圖片
        /// </summary>
        public void DownloadImg(int id, string url, string needReferer)
        {
            try
            {
                #region 創建請求資料
                SessionClient ss = new SessionClient();
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Proxy = MainWindow.WebProxy;
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                if (needReferer != null)
                    req.Referer = needReferer;
                req.AllowAutoRedirect = true;
                req.CookieContainer = ss.CookieContainer;

                //將請求加入請求組
                reqs.Add(id, req);
                #endregion

                //非同步下載開始
                req.BeginGetResponse(new AsyncCallback(RespCallback), new KeyValuePair<int, HttpWebRequest>(id, req));
            }
            catch (Exception ex)
            {
                Program.Log(ex, "Download sample failed");
                StopLoadImg(id, true, "創建下載失敗");
            }
        }

        /// <summary>
        /// 非同步下載
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
                        //取響應資料
                        HttpWebResponse res = (HttpWebResponse)re.Value.EndGetResponse(req);
                        string resae = res.Headers[HttpResponseHeader.ContentEncoding];
                        Stream str = res.GetResponseStream();

                        //響應長度
                        double reslength = res.ContentLength, restmplength = 0;


                        //獲取資料更新進度條
                        ThreadPool.QueueUserWorkItem((obj) =>
                        {
                            //緩衝塊長度
                            byte[] buffer = new byte[1024];
                            //讀到的位元組長度
                            int realReadLen = str.Read(buffer, 0, 1024);
                            //進度條位元組進度
                            long progressBarValue = 0;
                            double progressSetValue = 0;
                            //記憶體流位元組組
                            byte[] data = null;
                            MemoryStream ms = new MemoryStream();

                            //寫流資料並更新進度條
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
                                    Dispatcher.Invoke(new UIdelegate(delegate (object sende) { StopLoadImg(re.Key, "資料中止"); }), "");
                                    return;
                                }
                            }

                            data = ms.ToArray();

                            //解壓gzip
                            if (resae != null && resae.Contains("gzip", StringComparison.OrdinalIgnoreCase))
                            {
                                ungzip(ref data);
                            }

                            //將位元組組轉為流
                            ms = new MemoryStream(data);

                            //讀完資料傳遞圖片流並顯示
                            Dispatcher.Invoke(new UIdelegate(delegate (object sende) { AssignImg(ms, re.Key); }), "");

                            str.Dispose();
                            str.Close();
                        }, null);
                    }
                    catch (WebException e)
                    {
                        Dispatcher.Invoke(new UIdelegate(delegate (object sende) { StopLoadImg(re.Key, true, "緩衝失敗"); }), e);
                    }
                }), this);
            }
            catch (Exception ex2)
            {
                Program.Log(ex2, "Download sample failed");
                Dispatcher.Invoke(new UIdelegate(delegate (object sender) { StopLoadImg(re.Key, true, "下載失敗"); }), ex2);
            }
        }

        /// <summary>
        /// 解壓gzip
        /// </summary>
        /// <param name="data">位元組組</param>
        /// <returns></returns>
        private static byte[] ungzip(ref byte[] data)
        {
            try
            {
                MemoryStream js = new MemoryStream();                       // 解壓後的流   
                MemoryStream ms = new MemoryStream(data);                   // 用於解壓的流   
                GZipStream g = new GZipStream(ms, CompressionMode.Decompress);
                byte[] buffer = new byte[5120];                                // 5K緩衝區      
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
        /// 設進度條進度值
        /// </summary>
        /// <param name="value"></param>
        private void SetProgressBar(double value)
        {
            pdload.Value = value;
        }

        #region 停止載入預覽圖
        /// <summary>
        /// 停止載入
        /// </summary>
        /// <param name="id"></param>
        /// <param name="Failed">是否失敗</param>
        /// <param name="FMsg">失敗提示</param>
        public void StopLoadImg(int id, bool Failed, string FMsg)
        {
            try
            {
                //清理請求資料
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
        /// 停止載入2
        /// </summary>
        /// <param name="id"></param>
        /// <param name="FMsg">失敗提示</param>
        public void StopLoadImg(int id, string FMsg)
        {
            StopLoadImg(id, false, FMsg);
        }
        #endregion

        /// <summary>
        /// 顯示預覽
        /// </summary>
        /// <param name="str">各種Stream</param>
        /// <param name="key">img.ID</param>
        private void AssignImg(Stream str, int key)
        {
            //流下載完畢
            try
            {
                //提取圖片規格類型
                string type = GetImgType(str);
                imgType = type;

                //記錄Stream
                strs = str;

                //分顯示容器
                switch (type)
                {
                    case "bmp":
                    case "jpg":
                    case "png":
                        //靜態圖片格式轉化
                        System.Drawing.Image dimg = System.Drawing.Image.FromStream(str);
                        MemoryStream ms = new MemoryStream();
                        dimg.Save(ms, imgf[type]);
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.StreamSource = new MemoryStream(ms.ToArray()); //ms不能直接用
                        bi.EndInit();
                        ms.Close();

                        //創建靜態圖片控制項
                        Image img = new Image()
                        {
                            Source = bi,
                            Stretch = Stretch.Uniform,
                            SnapsToDevicePixels = true,
                            StretchDirection = StretchDirection.Both,
                            Margin = new Thickness() { Top = 0, Right = 0, Bottom = 0, Left = 0 }
                        };

                        //將預覽控制項加入布局
                        prewimg.Children.Add(img);
                        break;

                    case "gif":
                        //創建GIF動圖控制項
                        AnimatedGIF gif = new AnimatedGIF()
                        {
                            GIFSource = str,
                            Stretch = Stretch.Uniform,
                            SnapsToDevicePixels = true,
                            StretchDirection = StretchDirection.Both
                        };

                        //將預覽控制項加入布局
                        prewimg.Children.Add(gif);
                        break;

                    default:
                        //未支援預覽的格式
                        Dispatcher.Invoke(new UIdelegate(delegate (object ss) { StopLoadImg(key, "不支援" + type); }), "");
                        return;
                }

                //選中的預覽圖顯示出來
                if (key == prew.SelectedId)
                {
                    Visibility = Visibility.Visible;
                }

                //隱藏進度條
                ProgressPlate.Visibility = Visibility.Hidden;

                ImgZoom(true);
                imgloaded = true;
            }
            catch (Exception ex1)
            {
                Program.Log(ex1, "Read sample img failed");
                Dispatcher.Invoke(new UIdelegate(delegate (object ss) { StopLoadImg(key, true, "讀取資料失敗"); }), ex1);
            }
        }

        #region 設定圖片縮放
        /// <summary>
        /// 設定圖片縮放
        /// </summary>
        /// <param name="zoom">true自適應</param>
        /// <param name="begin">首次顯示</param>
        public void ImgZoom(bool zoom, bool begin)
        {
            if (imgType == null) return;

            double imgw = 0, imgh = 0;
            AnimatedGIF gifo = null;
            bool isani = false;

            UIElement imgui = prewimg.Children[0];
            //分類型取值
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
        /// 設定圖片縮放首次模式自適應
        /// </summary>
        public void ImgZoom(bool begin) { ImgZoom(true, begin); }

        /// <summary>
        /// 設定圖片縮放到自適應
        /// </summary>
        public void ImgZoom() { ImgZoom(true, false); }
        #endregion

        /// <summary>
        /// 簡單的獲取圖片類型，失敗返回空
        /// </summary>
        /// <param name="str">Stream</param>
        /// <returns>bmp,jpg,png,gif,webm,mpeg,zip,rar,7z</returns>
        private string GetImgType(Stream str)
        {
            if (str == null) return "";

            //由自帶對象判斷類型
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

            //如果對象無法判斷就取檔案頭位元組判斷
            //圖片類型特徵位元組
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

            //取資料頭一部分
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
