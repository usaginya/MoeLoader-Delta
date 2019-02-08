using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MoeLoaderDelta
{
    /// <summary>
    /// Interaction logic for ImgControl.xaml
    /// 缩略图面板中的图片用户控件
    /// Last change 180527
    /// </summary>
    public partial class ImgControl : UserControl
    {
        private ImageSite site;
        private Img img;
        public Img Image
        {
            get { return img; }
        }

        private int index;
        private bool canRetry = false;
        private bool isRetrievingDetail = false, isDetailSucc = false;
        private bool imgLoaded = false;
        private bool isChecked = false;


        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private HttpWebRequest req;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="img">图片</param>
        /// <param name="index">缩略图位置索引</param>
        /// <param name="site">图片站点</param>
        public ImgControl(Img img, int index, ImageSite site)
        {
            InitializeComponent();
            this.site = site;
            this.img = img;
            this.index = index;
            shc.Add("Accept-Ranges", "bytes");
            shc.Accept = null;
            shc.Referer = site.Referer;
            shc.Timeout = 8000;
            shc.ContentType = SessionHeadersValue.ContentTypeAuto;

            if (img.IsViewed)
                //statusBorder.Background = new SolidColorBrush(Color.FromArgb(0xCC, 0xFE, 0xE2, 0xE2));
                statusBorder.Background = new SolidColorBrush(Color.FromArgb(0xEE, 0xE9, 0x93, 0xAA));

            //try
            //{
            //string s = .Substring(img.Score.IndexOf(' '), img.Score.Length - img.Score.IndexOf(' '));
            //score.Text = img.Score.ToString();
            //}
            //catch { }
            /*
            if (!supportScore)
            {
                brdScr.Visibility = System.Windows.Visibility.Hidden;
            }*/

            //chk.Text = img.Dimension;

            //RenderOptions.SetBitmapScalingMode(preview, BitmapScalingMode.Fant);
            preview.DataContext = img;

            preview.SizeChanged += new SizeChangedEventHandler(preview_SizeChanged);
            preview.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(preview_ImageFailed);
            preview.MouseUp += new MouseButtonEventHandler(preview_MouseUp);
            statusBorder.MouseUp += new MouseButtonEventHandler(preview_MouseUp);
            chk.MouseUp += new MouseButtonEventHandler(preview_MouseUp);
            txtDesc.MouseUp += new MouseButtonEventHandler(preview_MouseUp);

            downBtn.MouseUp += new MouseButtonEventHandler(Border_MouseUp);
            magBtn.MouseUp += new MouseButtonEventHandler(preview_Click);

            //chk.Click += chk_Checked;

            //ToolTip tip = preview.ToolTip as ToolTip;
            //tip.PlacementTarget = preview.Parent as UIElement;
            //TextBlock desc = (tip.Content as Border).Child as TextBlock;

            //下载缩略图
            //DownloadImg();

            if (img.DownloadDetail != null)
            {
                //need detail
                LayoutRoot.IsEnabled = false;
                //isRetrievingDetail = true;
                chk.Text = "信息未加载";
            }
            else
                ShowImgDetail();
        }

        void ShowImgDetail()
        {
            chk.Text = site.IsShowRes ? img.Dimension : img.Desc;
            string type = "N/A", aniformat = "gif webm mpeg  mpg mp4 avi";

            if (img.OriginalUrl.Length > 6)
            {
                type = BooruProcessor.FormattedImgUrl(string.Empty, img.OriginalUrl.Substring(img.OriginalUrl.LastIndexOf('.') + 1)).ToUpper();
            }
            else
            {
                //url不可能这么短
                LayoutRoot.IsEnabled = false;
                chk.Text = "原始地址无效";
                preview_ImageFailed(null, null);
                return;
            }
            score.Text = img.Score.ToString();
            txtDesc.Inlines.Add(img.Id + " " + img.Desc.Replace(Environment.NewLine, string.Empty));
            txtDesc.Inlines.Add(new LineBreak());
            txtDesc.Inlines.Add(type + " " + img.Author);
            //txtDesc.Inlines.Add(new LineBreak());
            txtDesc.Inlines.Add(" " + img.FileSize);
            txtDesc.ToolTip = img.Id + " " + img.Desc + "\r\n" + img.Author + "\r\n" + type + "  " + img.FileSize + "  " + img.Date;
            //txtDesc.Inlines.Add(new LineBreak());
            //txtDesc.Inlines.Add("评分: " + img.Score);
            //txtDesc.Inlines.Add(new LineBreak());
            //txtDesc.Inlines.Add("时间: " + img.Date);
            isDetailSucc = true;

            //ANI ico
            selani.Opacity = aniformat.Contains(type, StringComparison.CurrentCultureIgnoreCase) ? .5 : 0;
        }

        /// <summary>
        /// 下载图片
        /// </summary>
        public void DownloadImg()
        {
            if (PreFetcher.Fetcher.PreFetchedImg(img.SampleUrl) != null)
            {
                preview.Source = PreFetcher.Fetcher.PreFetchedImg(img.SampleUrl);
                //preview.Source = BitmapDecoder.Create(PreFetcher.Fetcher.PreFetchedImg(img.PreUrl), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            }
            else
            {
                try
                {
                    req = Sweb.CreateWebRequest(img.SampleUrl, MainWindow.WebProxy, shc);
                    req.Proxy = MainWindow.WebProxy;

                    //异步下载开始
                    req.BeginGetResponse(new AsyncCallback(RespCallback), req);
                }
                catch (Exception ex)
                {
                    Program.Log(ex, "Start download preview failed");
                    preview_ImageFailed(null, null);
                }
            }

            if (!isDetailSucc && img.DownloadDetail != null)
            {
                canRetry = true;
                isRetrievingDetail = true;
                chk.Text = "信息加载中...";
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback((o) =>
                {
                    try
                    {
                        img.DownloadDetail(img, MainWindow.WebProxy);
                        Dispatcher.Invoke(new VoidDel(() =>
                        {
                            LayoutRoot.IsEnabled = true;

                            ShowImgDetail();

                            isRetrievingDetail = false;
                            if (imgLoaded && ImgLoaded != null)
                                ImgLoaded(index, null);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Program.Log(ex, "Download img detail failed");
                        Dispatcher.Invoke(new VoidDel(() =>
                        {
                            preview_ImageFailed(null, null);
                            isRetrievingDetail = false;
                            canRetry = true;
                            chk.Text = "信息加载失败";
                            if (imgLoaded && ImgLoaded != null)
                                ImgLoaded(index, null);
                        }));
                    }
                }));
            }
        }

        /// <summary>
        /// 异步下载结束
        /// </summary>
        /// <param name="req"></param>
        private void RespCallback(IAsyncResult req)
        {
            try
            {
                WebResponse res = ((HttpWebRequest)(req.AsyncState)).EndGetResponse(req);
                System.IO.Stream str = res.GetResponseStream();

                Dispatcher.BeginInvoke(new VoidDel(delegate ()
                {
                    //BitmapFrame bmpFrame = BitmapDecoder.Create(str, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];

                    //bmpFrame.DownloadCompleted += new EventHandler(bmpFrame_DownloadCompleted);
                    //preview.Source = bmpFrame;
                    preview.Source = BitmapFrame.Create(str, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);
                    canRetry = false;
                }));
            }
            catch (Exception ex)
            {
                Program.Log(ex, "Download sample failed");
                Dispatcher.Invoke(new UIdelegate(delegate (object sender) { preview_ImageFailed(null, null); }), string.Empty);
            }
        }

        //void bmpFrame_DownloadCompleted(object sender, EventArgs e)
        //{
        //    System.Windows.Media.Animation.Storyboard sb = FindResource("imgLoaded") as System.Windows.Media.Animation.Storyboard;
        //    //sb.Completed += ClarifyImage;
        //    sb.Begin();

        //    lt.Visibility = System.Windows.Visibility.Collapsed;

        //    if (ImgLoaded != null)
        //        ImgLoaded(index, null);
        //}

        void preview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
            {
                if (selBorder.Opacity == 0)
                {
                    chk_Checked(true);
                }
                else
                {
                    chk_Checked(false);
                }
            }
        }

        void preview_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
            {
                imgClicked?.Invoke(index, null);
            }
        }

        private void chk_Checked(bool isChecked)
        {
            //未改变
            if (this.isChecked == isChecked) return;

            if (isChecked)
            {
                selBorder.Opacity = 1;
                selRec.Opacity = 1;
            }
            else
            {
                selBorder.Opacity = 0;
                selRec.Opacity = 0;
            }

            this.isChecked = isChecked;
            checkedChanged?.Invoke(index, null);
        }

        /// <summary>
        /// 停止缩略图加载
        /// </summary>
        public void StopLoadImg()
        {
            if (req != null)
                req.Abort();
            preview.Source = new BitmapImage(new Uri("/Images/pic.png", UriKind.Relative));

            canRetry = true;
            Storyboard sb = FindResource("showFail") as Storyboard;
            sb.Begin();
        }

        /// <summary>
        /// 设置是否选择复选框
        /// </summary>
        /// <param name="isChecked"></param>
        public bool SetChecked(bool isChecked)
        {
            if (!isDetailSucc) return false;
            //chk.IsChecked = isChecked;
            chk_Checked(isChecked);
            return true;
        }

        void preview_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            StopLoadImg();
        }

        /// <summary>
        /// 图像加载完毕
        /// </summary>
        public event EventHandler ImgLoaded;
        public event EventHandler checkedChanged;
        public event EventHandler imgClicked;
        public event EventHandler imgDLed;

        void preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 1 && e.NewSize.Height > 1)
            {
                //if (GlassHelper.GetForegroundWindow() == MainWindow.Hwnd)
                //{
                //窗口有焦点才进行动画
                preview.Stretch = Stretch.Uniform;
                Storyboard sb = FindResource("imgLoaded") as Storyboard;
                //sb.Completed += new EventHandler(delegate { preview.Stretch = Stretch.Uniform; });
                sb.Begin();

                lt.Visibility = Visibility.Collapsed;
                //}
                //else
                //{
                //    preview.Stretch = Stretch.Uniform;
                //    preview.Opacity = 1;
                //    ((preview.RenderTransform as TransformGroup).Children[0] as ScaleTransform).ScaleX = 1;
                //    ((preview.RenderTransform as TransformGroup).Children[0] as ScaleTransform).ScaleY = 1;
                //}

                imgLoaded = true;
                if (!isRetrievingDetail && ImgLoaded != null)
                    ImgLoaded(index, null);
            }
        }

        /// <summary>
        /// 加入下载队列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
            {
                imgDLed?.Invoke(index, null);
            }
        }

        /// <summary>
        /// 重载缩略图
        /// </summary>
        public void RetryLoad()
        {
            if (canRetry || preview.Opacity < 1)
            {
                //itmRetry.IsEnabled = false;
                canRetry = false;
                preview.Opacity = 0;
                preview.Stretch = Stretch.None;
                lt.Visibility = Visibility.Visible;
                preview.Source = null;
                ScaleTransform trans = (ScaleTransform)(((TransformGroup)(preview.RenderTransform)).Children[0]);
                trans.ScaleX = 0.6;
                trans.ScaleY = 0.6;

                Storyboard sb = FindResource("imgLoaded") as Storyboard;
                sb.Stop();
                sb = FindResource("showFail") as Storyboard;
                sb.Stop();
                sb = FindResource("hideFail") as Storyboard;
                sb.Begin();

                DownloadImg();
            }
        }

        private void txtDesc_Click_1(object sender, RoutedEventArgs e)
        {
            //ori
            try
            {
                Clipboard.SetText(img.OriginalUrl);
            }
            catch { }
        }

        private void txtDesc_Click_2(object sender, RoutedEventArgs e)
        {
            //jpg
            try
            {
                Clipboard.SetText(img.JpegUrl);
            }
            catch { }
        }

        private void txtDesc_Click_3(object sender, RoutedEventArgs e)
        {
            //预览图
            try
            {
                Clipboard.SetText(img.PreviewUrl);
            }
            catch { }
        }

        private void txtDesc_Click_4(object sender, RoutedEventArgs e)
        {
            //缩略图
            try
            {
                Clipboard.SetText(img.SampleUrl);
            }
            catch { }
        }

        private void txtDesc_Click_5(object sender, RoutedEventArgs e)
        {
            //tag
            try
            {
                Clipboard.SetText(img.Desc.Replace("\r\n", string.Empty));
            }
            catch { }
        }

        private void txtDesc_Click_6(object sender, RoutedEventArgs e)
        {
            //source
            try
            {
                Clipboard.SetText(img.Source.Replace("\r\n", string.Empty));
            }
            catch { }
        }

        private void txtDesc_Click_copyid(object sender, RoutedEventArgs e)
        {
            //id
            try
            {
                Clipboard.SetText(img.Id.ToSafeString());
            }
            catch { }
        }

        private void txtDesc_Click_copyauthor(object sender, RoutedEventArgs e)
        {
            //author
            try
            {
                Clipboard.SetText(img.Author.ToSafeString());
            }
            catch { }
        }

        private void txtDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (img.DetailUrl.Length > 0)
                    System.Diagnostics.Process.Start(img.DetailUrl);
            }
            catch (Exception) { }
        }
    }
}