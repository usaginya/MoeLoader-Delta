using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MoeLoaderDelta
{
    public delegate void UIdelegate(object sender);
    public delegate void VoidDel();

    internal enum ProxyType { System, Custom, None }

    internal class SessionState
    {
        public bool IsStop = false;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 主窗口的句柄
        /// </summary>
        public static IntPtr Hwnd;

        /// <summary>
        /// 程序名
        /// </summary>
        private static string programName;
        /// <summary>
        /// 程序版本
        /// </summary>
        public static Version ProgramVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// 封装的程序名
        /// </summary>
        public static string ProgramName
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                programName = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute))).Title;
                return programName;
            }
        }


        private const string IMGLOADING = "图片加载中...";

        private int num = 50, realNum = 50;
        private int page = 1, realPage = 1, lastPage = 1;

        //private Color backColor;
        //internal bool isAero = true;
        /// <summary>
        /// 使用最大化按钮最大化
        /// </summary>
        private bool ClickMaxButton = false;

        /// <summary>
        /// 是否还有下一页
        /// </summary>
        private bool HaveNextPage = false;

        /// <summary>
        /// 这个或许是同时加载缩略图的数量
        /// </summary>
        private int numOfLoading = 5;

        private Storyboard logo;

        /// <summary>
        /// 已经浏览过的位置
        /// </summary>
        private Dictionary<string, ViewedID> viewedIds;
        private int nowSelectedIndex = 0, lastSelectIndex = 0;

        internal List<Img> imgs;
        private List<int> selected = new List<int>();

        internal PreviewWnd previewFrm;
        private SessionState currentSession;
        private bool isGetting = false;

        /// <summary>
        /// 使用的地址类型
        /// </summary>
        private AddressType addressType = AddressType.Ori;

        //已加载完毕的图像索引
        private List<int> loaded = new List<int>();
        //未加载完毕的
        private LinkedList<int> unloaded = new LinkedList<int>();

        internal bool showExplicit = true;
        private bool naviMoved = false;
        private bool funcBtnShown = false;

        //Microsoft.Windows.Shell.WindowChrome chrome;

        public static MainWindow MainW;

        internal static int comboBoxIndex = 0;
        internal const string DefaultPatter = "[%site_%id_%author]%desc<!<_%imgp[5]";
        internal string namePatter = DefaultPatter;

        internal double bgOp = 0.3;
        internal ImageBrush bgImg = null;
        internal Stretch bgSt = Stretch.None;
        internal AlignmentX bgHe = AlignmentX.Right;
        internal AlignmentY bgVe = AlignmentY.Bottom;
        //private bool isStyleNone = true;

        [DllImport("user32")]
        private static extern int RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, System.Windows.Forms.Keys vk);
        [DllImport("user32")]
        private static extern int UnregisterHotKey(IntPtr hwnd, int id);

        private static System.Windows.Forms.Keys bossKey;
        public static System.Windows.Forms.Keys BossKey
        {
            set
            {
                //Warning!! The order can not be wrong!! 顺序不能错!!
                UnregisterHotKey(Hwnd, (int)bossKey);
                bossKey = value;
                RegisterHotKey(Hwnd, (int)bossKey, 0, bossKey);
            }
            get { return bossKey; }
        }

        /// <summary>
        /// 代理设置，eg. 127.0.0.1:1080
        /// </summary>
        internal static string Proxy
        {
            set;
            get;
        }

        internal static ProxyType ProxyType
        {
            get;
            set;
        }


        public MainWindow()
        {
            InitializeComponent();
            Title = ProgramName;

            if (!File.Exists(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\nofont.txt"))
            {
                FontFamily = new FontFamily("Microsoft YaHei");
            }

            //MaxWidth = System.Windows.SystemParameters.MaximizedPrimaryScreenWidth;
            //MaxHeight = System.Windows.SystemParameters.MaximizedPrimaryScreenHeight;
            /////////////////////////////////////// init image site list //////////////////////////////////
            Dictionary<string, MenuItem> dicSites = new Dictionary<string, MenuItem>();
            List<MenuItem> tempSites = new List<MenuItem>();
            List<ImageSite> tmpISites = SiteManager.Instance.Sites;

            int index = 0;
            foreach (ImageSite site in SiteManager.Instance.Sites)
            {
                MenuItem menuItem = null;
                //group by shortName
                if (dicSites.ContainsKey(site.ShortName))
                {
                    menuItem = dicSites[site.ShortName];
                }
                else
                {
                    int space = site.SiteName.IndexOf('[');

                    menuItem = new MenuItem()
                    {
                        Header = (
                        space > 0
                        ? site.SiteName.Substring(0, space)
                        : site.SiteName
                        )
                    };

                    menuItem.Style = (Style)Resources["SimpleMenuItem"];
                    dicSites.Add(site.ShortName, menuItem);
                }
                MenuItem subItem = new MenuItem() { Header = site.SiteName, ToolTip = site.ToolTip, DataContext = index++ };
                subItem.Click += new RoutedEventHandler(menuItem_Click);
                subItem.Style = (Style)Resources["SimpleMenuItem"];
                menuItem.Items.Add(subItem);
            }

            foreach (ImageSite site in SiteManager.Instance.Sites)
            {
                MenuItem menuItem = dicSites[site.ShortName];
                if (menuItem == null) continue;
                if (menuItem.Items.Count == 1)
                {
                    menuItem = menuItem.Items[0] as MenuItem;
                }

                //menuItem.Icon = new BitmapImage(new Uri("/Images/site" + (index++) + ".ico", UriKind.Relative));
                Stream iconStr = site.IconStream;
                if (iconStr != null)
                {
                    BitmapImage ico = new BitmapImage();
                    ico.CacheOption = BitmapCacheOption.Default;
                    ico.BeginInit();
                    ico.StreamSource = site.IconStream;
                    ico.EndInit();
                    menuItem.Icon = ico;
                }
                tempSites.Add(menuItem);

                dicSites[site.ShortName] = null;
            }

            if (SiteManager.Instance.Sites.Count > 0)
            {
                siteMenu.ItemsSource = tempSites;
                siteMenu.Header = SiteManager.Instance.Sites[comboBoxIndex].ShortName;
                siteMenu.Icon = tempSites[0].Icon;
                siteText.Text = "当前站点 " + SiteManager.Instance.Sites[comboBoxIndex].ShortName;
            }
            //comboBox1.ItemsSource = tempSites;
            //comboBox1.SelectedIndex = 0;
            /////////////////////////////////////////////////////////////////////////////////////////////

            viewedIds = new Dictionary<string, ViewedID>(SiteManager.Instance.Sites.Count);

            Proxy = "127.0.0.1:1080";
            ProxyType = ProxyType.System;
            bossKey = System.Windows.Forms.Keys.F9;

            LoadConfig();
            //itmxExplicit.IsChecked = !showExplicit;

            MainW = this;
        }

        void menuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SiteManager.Instance.Sites.Count < 1)
                return;

            MenuItem item = sender as MenuItem;
            comboBoxIndex = (int)(item.DataContext);
            siteMenu.Header = SiteManager.Instance.Sites[comboBoxIndex].ShortName + " " + SiteManager.Instance.Sites[comboBoxIndex].ShortType;
            siteMenu.Icon = (item.Parent as MenuItem).Header.ToString() == item.Header.ToString() ? item.Icon : (item.Parent as MenuItem).Icon;
            //functionality support check
            if (SiteManager.Instance.Sites[comboBoxIndex].IsSupportCount)
            {
                stackPanel1.IsEnabled = true;
            }
            else
            {
                stackPanel1.IsEnabled = false;
            }

            if (SiteManager.Instance.Sites[comboBoxIndex].IsSupportScore)
            {
                itmMaskScore.IsEnabled = true;
            }
            else
            {
                itmMaskScore.IsEnabled = false;
            }

            if (SiteManager.Instance.Sites[comboBoxIndex].IsSupportRes)
            {
                itmMaskRes.IsEnabled = true;
            }
            else
            {
                itmMaskRes.IsEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logo = FindResource("logoRotate") as Storyboard;

            BossKey = bossKey;

            GlassHelper.EnableBlurBehindWindow(containerB, this);
            (new Thread(new ThreadStart(LoadBgImg))).Start();

        }

        private void LoadBgImg()
        {
            string bgPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\bg.png";
            bool hasBg = false;
            if (File.Exists(bgPath))
            {
                hasBg = true;
            }
            else
            {
                bgPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\bg.jpg";
                if (File.Exists(bgPath))
                {
                    hasBg = true;
                }
            }
            if (hasBg)
            {
                Dispatcher.Invoke(new VoidDel(delegate
                {
                    bgImg = new ImageBrush(new BitmapImage(new Uri(bgPath, UriKind.Absolute)))
                    {
                        Stretch = bgSt,
                        AlignmentX = bgHe,
                        AlignmentY = bgVe,
                        Opacity = bgOp,
                    };
                    grdBg.Background = bgImg;
                }));
            }
        }

        public static string IsNeedReferer(string url)
        {
            List<ImageSite> ISites = SiteManager.Instance.Sites;

            foreach (ImageSite site in SiteManager.Instance.Sites)
            {
                if (site.SubReferer != null)
                {
                    string[] subrefs = site.SubReferer.Split(',');
                    foreach (string sref in subrefs)
                    {
                        if (url.Contains(sref))
                            return site.Referer;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 载入配置
        /// </summary>
        private void LoadConfig()
        {
            string configFile = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Moe_config.ini";

            //读取配置文件
            if (File.Exists(configFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(configFile);
                    downloadC.NumOnce = int.Parse(lines[0]);

                    if (lines[1] != "." && Directory.Exists(lines[1]))
                        DownloadControl.SaveLocation = lines[1];

                    if (lines[2].Contains(';'))
                    {
                        string[] parts = lines[2].Split(';');
                        //itmJpg.IsChecked = parts[0].Equals("1");
                        addressType = (AddressType)Enum.Parse(typeof(AddressType), parts[0]);

                        if (parts.Length > 1)
                        {
                            downloadC.IsSaSave = parts[1].Equals("1");
                        }

                        if (parts.Length > 2)
                        {
                            numOfLoading = Int32.Parse(parts[2]);
                            if (numOfLoading < 4) numOfLoading = 5;
                        }
                        if (parts.Length > 3)
                        {
                            itmMaskViewed.IsChecked = parts[3].Equals("1");
                        }
                        if (parts.Length > 4)
                        {
                            string[] words = parts[4].Split('|');
                            foreach (string word in words)
                            {
                                //if (word.Trim().Length > 0)
                                //txtSearch.Items.Add(word);
                                searchControl.AddUsedItem(word);
                            }
                        }
                        //if (!txtSearch.Items.Contains("thighhighs"))
                        //txtSearch.Items.Add("thighhighs");
                        if (parts.Length > 5)
                        {
                            Proxy = parts[5];
                        }
                        if (parts.Length > 6)
                        {
                            bossKey = (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), parts[6]);
                        }
                        if (parts.Length > 7)
                        {
                            itmSmallPre.IsChecked = parts[7].Equals("1");
                        }
                        if (parts.Length > 8)
                        {
                            ProxyType = (ProxyType)Enum.Parse(typeof(ProxyType), parts[8]);
                        }
                        if (parts.Length > 9)
                        {
                            try
                            {
                                //Size pos = Size.Parse(parts[9]);
                                var posItem = parts[9].Split(',');
                                Size pos = new Size(int.Parse(posItem[0]), int.Parse(posItem[1]));
                                if (pos.Width > MinWidth && pos.Height > MinHeight)
                                {
                                    //rememberPos = true;
                                    //Left = pos.X;
                                    //Top = pos.Y;
                                    //startPos.Width = pos.Width;
                                    //startPos.Height = pos.Height;
                                    Width = pos.Width;
                                    Height = pos.Height;
                                }
                            }
                            catch { }
                        }
                        if (parts.Length > 10)
                        {
                            togglePram.IsChecked = parts[10].Equals("1");
                            if (togglePram.IsChecked.Value)
                            {
                                togglePram.ToolTip = "显示搜索设置";
                            }
                            else
                            {
                                grdParam.Width = 479;
                                grdParam.Opacity = 1;
                                togglePram.ToolTip = (string)togglePram.Tag;
                            }
                        }
                        if (parts.Length > 11)
                        {
                            PreFetcher.CachedImgCount = int.Parse(parts[11]);
                        }
                        if (parts.Length > 12)
                        {
                            downloadC.IsSepSave = parts[12].Equals("1");
                        }
                        if (parts.Length > 13)
                        {
                            itmxExplicit.IsChecked = parts[13].Equals("1");
                            showExplicit = !itmxExplicit.IsChecked;
                        }
                        if (parts.Length > 14)
                        {
                            namePatter = parts[14];
                        }
                        if (parts.Length > 15)
                        {
                            txtNum.Text = parts[15];
                        }
                        if (parts.Length > 16)
                        {
                            bgSt = (Stretch)Enum.Parse(typeof(Stretch), parts[16]);
                        }
                        if (parts.Length > 17)
                        {
                            bgHe = (AlignmentX)Enum.Parse(typeof(AlignmentX), parts[17]);
                        }
                        if (parts.Length > 18)
                        {
                            bgVe = (AlignmentY)Enum.Parse(typeof(AlignmentY), parts[18]);
                        }
                        if (parts.Length > 19)
                        {
                            bgOp = double.Parse(parts[19]);
                        }
                    }
                    //else itmJpg.IsChecked = lines[2].Trim().Equals("1");
                    else addressType = (AddressType)Enum.Parse(typeof(AddressType), lines[2].Trim());

                    for (int i = 3; i < lines.Length; i++)
                    {
                        if (lines[i].Trim().Length > 0)
                        {
                            if (lines[i].Contains(':'))
                            {
                                string[] parts = lines[i].Trim().Split(':');
                                viewedIds[parts[0]] = new ViewedID();
                                viewedIds[parts[0]].AddViewedRange(parts[1]);
                            }
                            else
                            {
                                //向前兼容
                                if (i - 3 >= SiteManager.Instance.Sites.Count) break;
                                else if (SiteManager.Instance.Sites.Count > 0)
                                {
                                    viewedIds[SiteManager.Instance.Sites[i - 3].ShortName] = new ViewedID();
                                    viewedIds[SiteManager.Instance.Sites[i - 3].ShortName].AddViewedRange(lines[i].Trim());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "读取配置文件失败\r\n" + ex.Message, MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            switch (addressType)
            {
                case AddressType.Ori:
                    itmTypeOri.IsChecked = true;
                    break;
                case AddressType.Jpg:
                    itmTypeJpg.IsChecked = true;
                    break;
                case AddressType.Pre:
                    itmTypePreview.IsChecked = true;
                    break;
                case AddressType.Small:
                    itmTypeSmall.IsChecked = true;
                    break;
            }

            //string logoPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\logo.png";
            //if (System.IO.File.Exists(logoPath))
            //{
            //image.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
            //}
            //else image.Source = new BitmapImage(new Uri("Images/logo1.png", UriKind.Relative));
        }

        /// <summary>
        /// 更改翻页按钮状态
        /// </summary>
        /// <param name="btnid">0上一页, 1下一页</param>
        private void UpdatePreNextEnable(int btnid)
        {
            switch (btnid)
            {
                case 0:
                    btnPrev.IsEnabled = realPage > 1;
                    btnPrev.Visibility = (realPage > 1 ? Visibility.Visible : Visibility.Hidden);
                    break;
                case 1:
                    btnNext.IsEnabled = HaveNextPage;
                    btnNext.Visibility = (HaveNextPage ? Visibility.Visible : Visibility.Hidden);
                    break;
            }
            PlayPreNextAnimation(btnid);
        }
        /// <summary>
        /// 更改上一页按钮状态
        /// </summary>
        private void UpdatePreNextEnable()
        {
            UpdatePreNextEnable(0);
        }

        /// <summary>
        /// 翻页按钮动画
        /// </summary>
        /// <param name="btnid">0 上一页, 1下一页</param>
        private void PlayPreNextAnimation(int btnid)
        {
            Thickness mrg = (scrList.ComputedVerticalScrollBarVisibility == Visibility.Collapsed) ? new Thickness(0) : new Thickness(0, 0, 15, 0);
            ThicknessAnimation btna = new ThicknessAnimation();
            btna.To = mrg;
            btna.Duration = TimeSpan.FromMilliseconds(666);
            switch (btnid)
            {

                case 0:
                    {
                        Storyboard sb = FindResource("sbShowPageBtnPrev") as Storyboard;
                        Storyboard.SetTargetProperty(btna, new PropertyPath("(Button.Margin)"));
                        Storyboard.SetTarget(btna, btnPrev);
                        sb.Children.Add(btna);
                        sb.Begin();
                        break;
                    }
                case 1:
                    {
                        Storyboard sb = (Storyboard)FindResource("sbShowPageBtnNext");
                        Storyboard.SetTargetProperty(btna, new PropertyPath("(Button.Margin)"));
                        Storyboard.SetTarget(btna, btnNext);
                        sb.Children.Add(btna);
                        sb.Begin();
                        break;
                    }
            }
        }
        /// <summary>
        /// 上一页按钮动画
        /// </summary>
        private void PlayPreNextAnimation()
        {
            PlayPreNextAnimation(0);
        }

        /// <summary>
        /// 图片信息已获取
        /// </summary>
        /// <param name="sender"></param>
        public void LoadComplete(object sender)
        {
            if (sender == null)
            {
                currentSession.IsStop = true;
                statusText.Text = "加载完毕，取得 0 张图片";

                txtGet.Text = "搜索";
                btnGet.ToolTip = "获取图片列表";
                isGetting = false;
                imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

                logo.Stop();
                bgLoading.Visibility = Visibility.Hidden;
                //itmThunder.IsEnabled = false;
                //itmLst.IsEnabled = false;

                itmSelectInverse.IsEnabled = false;
                itmSelectAll.IsEnabled = false;
                itmUnSelectAll.IsEnabled = false;
                itmReload.IsEnabled = false;
                //重新读取RToolStripMenuItem.Enabled = false;

                imgPanel.Children.Clear();
            }
            else
            {
                imgs = (List<Img>)sender;
                selected.Clear();
                ShowOrHideFuncBtn(true);
                loaded.Clear();
                unloaded.Clear();
                imgPanel.Children.Clear();

                if (previewFrm != null && previewFrm.IsLoaded)
                {
                    previewFrm.Close();
                    //previewFrm = null;
                }

                //itmThunder.IsEnabled = true;
                //itmLst.IsEnabled = true;

                statusText.Text = IMGLOADING;
                //if (nowSelectedIndex == 0 || nowSelectedIndex == 1)
                //{
                //itmSmallPre.IsEnabled = true;
                //}
                //else itmSmallPre.IsEnabled = false;

                itmSelectInverse.IsEnabled = true;
                itmSelectAll.IsEnabled = true;
                itmUnSelectAll.IsEnabled = true;
                itmReload.IsEnabled = true;
                //重新读取RToolStripMenuItem.Enabled = true;

                if (imgs.Count == 0)
                {
                    DocumentCompleted();
                    return;
                }

                //生成缩略图控件
                for (int i = 0; i < imgs.Count; i++)
                {
                    //int id = Int32.Parse(imgs[i].Id);

                    ImgControl img = new ImgControl(
                        imgs[i], i,
                        SiteManager.Instance.Sites[nowSelectedIndex].Referer,
                        SiteManager.Instance.Sites[nowSelectedIndex].IsSupportScore);

                    img.imgDLed += img_imgDLed;
                    img.imgClicked += img_Click;
                    img.ImgLoaded += img_ImgLoaded;
                    img.checkedChanged += img_checkedChanged;

                    // Default: 160x183 Large: 310x333
                    //if ((nowSelectedIndex == 0 || nowSelectedIndex == 1) && !itmSmallPre.IsChecked)
                    if (!itmSmallPre.IsChecked)
                    {
                        img.Width = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.X;
                        img.Height = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.Y;
                    }

                    //WrapPanel.SetZIndex(img, imgs.Count - i);
                    imgPanel.Children.Add(img);

                    if (i < numOfLoading)
                    {
                        //初始加载
                        img.DownloadImg();
                    }
                    else unloaded.AddLast(i);
                    //}
                }
                scrList.ScrollToTop();
            }
        }

        /// <summary>
        /// 将某个图片加入下载队列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_imgDLed(object sender, EventArgs e)
        {
            int index = (int)sender;

            if (!toggleDownload.IsChecked.Value)
                toggleDownload.IsChecked = true;

            toggleDownload_Click(null, null);

            Img dlimg = imgs[index];
            List<string> oriUrls = GetImgAddress(dlimg);
            for (int c = 0; c < oriUrls.Count; c++)
            {
                //设图册页数
                if (oriUrls.Count > 1)
                {
                    imgs[index].ImgP = c + 1 + "";
                }
                string fileName = GenFileName(dlimg);
                string domain = SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                downloadC.AddDownload(new MiniDownloadItem[] { new MiniDownloadItem(fileName, oriUrls[c], domain, dlimg.Author, "", "", dlimg.Id) });
            }
            //string url = GetImgAddress(imgs[index]);
            //string fileName = GenFileName(imgs[index]);
            //downloadC.AddDownload(new MiniDownloadItem[] { new MiniDownloadItem(fileName, url) });

            //System.Media.SystemSounds.Exclamation.Play();
        }

        /// <summary>
        /// 缩略图被选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_checkedChanged(object sender, EventArgs e)
        {
            int preid = selected.Count == 0 ? -1 : selected[selected.Count - 1];

            int id = (int)sender;
            if (selected.Contains(id))
                selected.Remove(id);
            else selected.Add(id);

            if (IsShiftDown())
            {
                //批量选择
                for (int i = preid + 1; i < id; i++)
                {
                    bool enabled = ((ImgControl)imgPanel.Children[i]).SetChecked(true);
                    if (enabled && !selected.Contains(i))
                        selected.Add(i);
                }
            }

            ShowOrHideFuncBtn(selected.Count < 1);
        }

        /// <summary>
        /// 右下角按钮显示处理
        /// </summary>
        /// <param name="hide"></param>
        private void ShowOrHideFuncBtn(bool hide)
        {
            selText.Text = "选中图片 " + selected.Count;

            Storyboard sb = new Storyboard();

            //显示or隐藏按钮
            if (hide && funcBtnShown)
            {
                sb = (Storyboard)FindResource("hideFuncBtns");
            }
            else if (!hide && !funcBtnShown)
            {
                sb = (Storyboard)FindResource("showFuncBtns");
            }

            grdFuncBtns.IsEnabled = funcBtnShown = !hide;
            sb.Begin();
        }

        /// <summary>
        /// 某个缩略图加载完毕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_ImgLoaded(object sender, EventArgs e)
        {
            loaded.Add((int)sender);

            if (loaded.Count == imgs.Count)
                DocumentCompleted();

            if (unloaded.Count > 0)
            {
                ((ImgControl)imgPanel.Children[unloaded.First.Value]).DownloadImg();
                unloaded.RemoveFirst();
            }

            //加载完第一张图时
            if (loaded.Count < 2)
            {
                //显示上一页按钮
                UpdatePreNextEnable();
                //重设缩略图大小
                itmSmallPre_Click(null, null);
            }
        }

        /// <summary>
        /// 预加载缩略图列表结束时
        /// </summary>
        /// <param name="sender">这有结果数量</param>
        private void Fetcher_PreListLoaded(object sender, EventArgs e)
        {
            //按照结果更新翻页按钮状态
            Dispatcher.Invoke(
                new Action(
                    delegate
                    {
                        //防止多次设置按钮状态
                        bool tmphave = HaveNextPage;
                        HaveNextPage = (int)sender > 0;

                        if (HaveNextPage && !tmphave && IsLoaded)
                        {
                            //等滚动条
                            for (int i = 0; i < 3; i++)
                            {
                                if (scrList.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                                    break;
                                Thread.Sleep(999);
                            }
                            UpdatePreNextEnable(1);
                        }
                    }));
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SiteManager.Instance.Sites.Count < 1)
                return;

            //获取
            if (!isGetting)
            {
                if (!naviMoved)
                {
                    Storyboard sbNavi = (Storyboard)FindResource("sbNavi");
                    sbNavi.Begin();
                    naviMoved = true;
                }

                isGetting = true;
                HaveNextPage = false;
                txtGet.Text = "停止";
                btnGet.ToolTip = "停止搜索";
                imgGet.Source = new BitmapImage(new Uri("/Images/stop.png", UriKind.Relative));

                //隐藏翻页按钮
                btnPrev.IsEnabled = btnNext.IsEnabled = false;
                btnPrev.Visibility = btnNext.Visibility = Visibility.Hidden;

                if (sender != null)
                {
                    //记录上一次选择，用于当缩略图尚未加载就停止时恢复
                    lastSelectIndex = nowSelectedIndex;
                    lastPage = realPage;

                    //由点击搜索按钮触发，所以使用界面上的设定
                    realNum = num;
                    realPage = page;
                    nowSelectedIndex = comboBoxIndex;
                    siteText.Text = "当前站点 " + SiteManager.Instance.Sites[nowSelectedIndex].SiteName;
                }
                //btnNext.Content = "下一页 (" + (realPage + 1) + ")";
                //btnPrev.Content = "上一页 (" + (realPage - 1) + ")";
                pageText.Text = "当前页码 " + realPage;

                bgLoading.Visibility = Visibility.Visible;
                logo.Begin();

                //nowSelectedIndex = comboBoxIndex;

                statusText.Text = "与伺服器通迅，请稍候...";

                if (searchControl.Text.Length != 0)
                {
                    //一次最近搜索词
                    searchControl.AddUsedItem(searchControl.Text);
                }

                showExplicit = !itmxExplicit.IsChecked;
                string word = searchControl.Text;
                //string url = PrepareUrl(realPage);
                //nowSession = new ImgSrcProcessor(MaskInt, MaskRes, url, SrcType, LastViewed, MaskViewed);
                //nowSession.processComplete += new EventHandler(ProcessHTML_processComplete);
                //(new System.Threading.Thread(new System.Threading.ThreadStart(nowSession.ProcessSingleLink))).Start();
                currentSession = new SessionState();

                //尝试预加载
                StartPreLoad();

                (new Thread(new ParameterizedThreadStart((o) =>
                {
                    List<Img> imgList = null;
                    try
                    {
                        //prefetch
                        string pageString = PreFetcher.Fetcher.GetPreFetchedPage(
                            realPage, realNum, Uri.EscapeDataString(word), SiteManager.Instance.Sites[nowSelectedIndex]);
                        if (pageString != null)
                        {
                            imgList = SiteManager.Instance.Sites[nowSelectedIndex].GetImages(pageString, WebProxy);
                        }
                        else
                        {
                            imgList = SiteManager.Instance.Sites[nowSelectedIndex].GetImages(realPage, realNum, Uri.EscapeDataString(word), WebProxy);
                        }

                        //过滤图片列表
                        imgList = SiteManager.Instance.Sites[nowSelectedIndex].FilterImg(
                            imgList, MaskInt, MaskRes, LastViewed, MaskViewed, showExplicit, true);

                    }
                    catch (Exception ex)
                    {
                        if (!(o as SessionState).IsStop)
                        {
                            Dispatcher.Invoke(new VoidDel(() =>
                            {
                                MessageBox.Show(this, "获取图片遇到错误: " + ex.Message,
                                    ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            }));
                        }
                    }
                    if (!(o as SessionState).IsStop)
                    {
                        Dispatcher.Invoke(new UIdelegate(LoadComplete), imgList);
                    }
                }))).Start(currentSession);

                GC.Collect();
            }
            else
            {
                if (statusText.Text == IMGLOADING)
                {
                    for (int i = 0; i < imgs.Count; i++)
                    {
                        if (!loaded.Contains(i))
                            ((ImgControl)imgPanel.Children[i]).StopLoadImg();
                    }
                    unloaded.Clear();
                }
                else
                {
                    currentSession.IsStop = true;
                    statusText.Text = "加载完毕，取得 0 张图片";
                    //恢复站点选择
                    nowSelectedIndex = lastSelectIndex;
                    siteText.Text = "当前站点 " + SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                    realPage = lastPage;

                    //尝试加载下一页
                    StartPreLoad();

                    //显示上一页按钮
                    UpdatePreNextEnable();

                    isGetting = false;
                    txtGet.Text = "搜索";
                    btnGet.ToolTip = "获取图片列表";
                    imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

                    logo.Stop();
                    bgLoading.Visibility = Visibility.Hidden;
                }
            }
        }

        /// <summary>
        /// 执行预加载
        /// </summary>
        private void StartPreLoad()
        {
            PreFetcher.Fetcher.PreListLoaded += Fetcher_PreListLoaded;
            PreFetcher.Fetcher.PreFetchPage(realPage + 1, realNum,
                Uri.EscapeDataString(searchControl.Text), SiteManager.Instance.Sites[nowSelectedIndex]);
        }

        public int MaskInt
        {
            get
            {
                int maskInt = -1;
                Dispatcher.Invoke(new VoidDel(delegate
                {
                    if (itm5.IsChecked)
                        maskInt = 5;
                    else if (itm10.IsChecked)
                        maskInt = 10;
                    else if (itm20.IsChecked)
                        maskInt = 20;
                    else if (itm30.IsChecked)
                        maskInt = 30;
                    else if (itm0.IsChecked)
                        maskInt = 0;
                }));

                return maskInt;
            }
        }

        public bool MaskViewed
        {
            get
            {
                bool mask = false;
                Dispatcher.Invoke(new VoidDel(delegate { mask = itmMaskViewed.IsChecked && searchControl.Text.Length == 0; })); return mask;
            }
        }
        public ViewedID LastViewed
        {
            get
            {
                if (!viewedIds.ContainsKey(SiteManager.Instance.Sites[nowSelectedIndex].ShortName))
                {
                    //maybe newly added site
                    viewedIds[SiteManager.Instance.Sites[nowSelectedIndex].ShortName] = new ViewedID();
                }
                return viewedIds[SiteManager.Instance.Sites[nowSelectedIndex].ShortName];
            }
        }

        //public ImgSrcProcessor.SourceType SrcType { get { return srcTypes[nowSelectedIndex]; } }

        public int MaskRes
        {
            get
            {
                int maskRes = -1;
                Dispatcher.Invoke(new VoidDel(delegate
                {
                    if (itmx5.IsChecked)
                        maskRes = 1024 * 768; //1024x768
                    else if (itmx10.IsChecked)
                        maskRes = 1280 * 720; //1280x720
                    else if (itmx20.IsChecked)
                        maskRes = 1680 * 1050; //1680x1050
                    else if (itmx30.IsChecked)
                        maskRes = 1920 * 1080; //1920x1080
                    else if (itmx0.IsChecked)
                        maskRes = 800 * 600; //800x600
                }));
                return maskRes;
            }
        }

        public ImageSource CreateImageSrc(Stream str)
        {
            ImageSource imgS = null;
            Dispatcher.Invoke(new VoidDel(delegate
            {
                imgS = BitmapDecoder.Create(str, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default).Frames[0];
            }));
            return imgS;
        }

        /// <summary>
        /// 所有缩略图加载完毕
        /// </summary>
        void DocumentCompleted()
        {
            logo.Stop();
            bgLoading.Visibility = Visibility.Hidden;

            int viewedC = 0;
            try
            {
                viewedC = imgs[imgs.Count - 1].Id - LastViewed.ViewedBiggestId;
            }
            catch { }
            if (viewedC < 5 || searchControl.Text.Length > 0)
                statusText.Text = "加载完毕，取得 " + imgs.Count + " 张图片";
            else
                statusText.Text = "加载完毕，取得 " + imgs.Count + " 张图片 (剩余约 " + viewedC + " 张未浏览)";

            //statusText.Text = "搜索完成！取得 " + imgs.Count + " 张图片信息 (上次浏览至 " + viewedIds[nowSelectedIndex].ViewedBiggestId + " )";
            txtGet.Text = "搜索";
            btnGet.ToolTip = "获取图片列表";
            isGetting = false;
            imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

            //System.Media.SystemSounds.Beep.Play();
            if (GlassHelper.GetForegroundWindow() != Hwnd)
                GlassHelper.FlashWindow(Hwnd, true);

            //无图时禁用菜单
            if (imgs.Count < 1)
            {
                itmSelectInverse.IsEnabled =
                    itmSelectAll.IsEnabled =
                    itmUnSelectAll.IsEnabled =
                    itmReload.IsEnabled = false;
            }

        }

        /// <summary>
        /// 显示下载列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggleDownload_Click(object sender, RoutedEventArgs e)
        {
            if (toggleDownload.IsChecked.Value)
            {
                toggleDownload.ToolTip = "隐藏下载面板";
                Storyboard sb = (Storyboard)FindResource("showDownload");

                if (IsCtrlDown())
                {
                    double rmrg = MainW.Width / 2;
                    ((ThicknessAnimationUsingKeyFrames)sb.Children[0]).KeyFrames[0].Value = new Thickness(0, 0, 2000, 0);
                    ((ThicknessAnimationUsingKeyFrames)sb.Children[1]).KeyFrames[0].Value = new Thickness(0, 0, rmrg, 0);
                    ((DoubleAnimationUsingKeyFrames)sb.Children[3]).KeyFrames[0].Value = rmrg;
                    if (grdNavi.HorizontalAlignment == HorizontalAlignment.Center) grdNavi.Visibility = Visibility.Hidden;
                }
                else
                {
                    ((ThicknessAnimationUsingKeyFrames)sb.Children[0]).KeyFrames[0].Value = new Thickness(0, 0, 219, 0);
                    ((ThicknessAnimationUsingKeyFrames)sb.Children[1]).KeyFrames[0].Value = new Thickness(0, 0, 220, 0);
                    ((DoubleAnimationUsingKeyFrames)sb.Children[3]).KeyFrames[0].Value = 219;
                }

                sb.Begin();
            }
            else
            {
                grdNavi.Visibility = Visibility.Visible;
                toggleDownload.ToolTip = "显示下载面板(按住Ctrl隐藏缩略图)";
                Storyboard sb = (Storyboard)FindResource("closeDownload");
                sb.Begin();
            }
        }

        #region Window Related
        private void Window_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            //maxmize
            if (e.OriginalSource is Grid && e.GetPosition(this).Y < bdDecorate.ActualHeight) Max_Click(null, null);
        }

        /// <summary>
        /// 窗口资源初始化
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(Hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        /// <summary>
        /// 按键监听事件
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                // 老板键
                if (wParam.ToInt32() == (int)bossKey)
                {
                    this.Visibility = this.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
                }
            }
            else if (msg == 0x0024)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            else if (msg == 0x0112)
            {
                //WM_SYSCOMMAND   0x0112
                if (wParam.ToInt32() == 0xF020)
                {
                    //SC_MINIMIZE  0xF020
                    //WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                    //GWL_STYLE -16
                    int nStyle = GlassHelper.GetWindowLong(hwnd, -16);
                    nStyle |= 0x00C00000;
                    //WS_CAPTION 0x00C00000L
                    GlassHelper.SetWindowLong(hwnd, -16, nStyle);
                    //isStyleNone = false;

                    WindowState = WindowState.Minimized;
                    handled = true;
                }
            }

            return wParam;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            GlassHelper.MINMAXINFO mmi = (GlassHelper.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(GlassHelper.MINMAXINFO));

            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = GlassHelper.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                GlassHelper.MONITORINFO monitorInfo = new GlassHelper.MONITORINFO();
                GlassHelper.GetMonitorInfo(monitor, monitorInfo);
                GlassHelper.RECT rcWorkArea = monitorInfo.rcWork;
                GlassHelper.RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left) - 6;
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top) - 6;
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left) + 18;
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top) + 13;
                //mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left) - 12;
                //mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top) - 16;
                //int maxHeight = Math.Abs(rcWorkArea.bottom - rcWorkArea.top) + 43;
                //mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left) + 27;
                //mmi.ptMaxSize.y = maxHeight;
                mmi.ptMinTrackSize.x = (int)MinWidth;
                mmi.ptMinTrackSize.y = (int)MinHeight;
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        /// <summary>
        /// 限制页码设置只能输入数字的一种方法
        /// </summary>
        private void txtPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 || e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key == Key.Back || e.Key == Key.Enter
                || e.Key == Key.Tab || e.Key == Key.LeftShift || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down))
            {
                e.Handled = true;
            }
        }

        private void txtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt.Text.Length == 0)
                return;
            try
            {
                num = int.Parse(txtNum.Text);
                page = int.Parse(txtPage.Text);

                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
            catch (NullReferenceException) { }
            catch (FormatException)
            {
                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
        }

        private void txtPage_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            try
            {
                num = int.Parse(txtNum.Text);
                page = int.Parse(txtPage.Text);

                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
            catch (NullReferenceException) { }
            catch (FormatException)
            {
                txtNum.Text = num.ToString();
                txtPage.Text = page.ToString();
            }
        }

        private void pageUp_Click(object sender, RoutedEventArgs e)
        {
            if (page < 99999)
                txtPage.Text = (page + 1).ToString();
        }

        private void pageDown_Click(object sender, RoutedEventArgs e)
        {
            if (page > 1)
                txtPage.Text = (page - 1).ToString();
        }

        private void numUp_Click(object sender, RoutedEventArgs e)
        {
            if (num < 999)
                txtNum.Text = (num + 1).ToString();
        }

        private void numDown_Click(object sender, RoutedEventArgs e)
        {
            if (num > 1)
                txtNum.Text = (num - 1).ToString();
        }
        #endregion

        #region keyCheck
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys key);
        public static bool IsKeyDown(System.Windows.Forms.Keys key)
        {
            if ((GetAsyncKeyState(key) & 0x8000) == 0x8000)
            {
                return true;
            }
            else return false;
        }
        public static bool IsCtrlDown()
        {
            if (IsKeyDown(System.Windows.Forms.Keys.LControlKey) || IsKeyDown(System.Windows.Forms.Keys.RControlKey))
                return true;
            else return false;
        }
        public static bool IsShiftDown()
        {
            if (IsKeyDown(System.Windows.Forms.Keys.LShiftKey) || IsKeyDown(System.Windows.Forms.Keys.RShiftKey))
                return true;
            else return false;
        }
        #endregion

        /// <summary>
        /// 预览图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_Click(object sender, EventArgs e)
        {
            int index = (int)sender;

            //排除不支持预览的格式
            string supportformat = "jpg jpeg png bmp gif";
            string[] arr = imgs[index].SampleUrl.Split('.');
            string ext = arr[arr.Length - 1];
            if (!supportformat.Contains(ext))
            {
                MessageBox.Show(this, "未支持" + ext + "格式的预览显示，请下载后使用其它程序方式打开文件预览",
                    ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (previewFrm == null || !previewFrm.IsLoaded)
            {
                previewFrm = new PreviewWnd(this);
                previewFrm.Show();
                this.Focus();
                //System.GC.Collect();
            }
            previewFrm.AddPreview(imgs[index], index, SiteManager.Instance.Sites[nowSelectedIndex].Referer);
            //System.Media.SystemSounds.Exclamation.Play();
        }

        public void SelectByIndex(int index)
        {
            (imgPanel.Children[index] as ImgControl).SetChecked(true);
            if (!selected.Contains(index))
                selected.Add(index);
        }

        /// <summary>
        /// 反选
        /// </summary>
        private void itmSelectInverse_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ImgControl imgc = (ImgControl)imgPanel.Children[i];

                if (selected.Contains(i))
                {
                    imgc.SetChecked(false);
                    selected.Remove(i);
                }
                else
                {
                    imgc.SetChecked(true);
                    selected.Add(i);
                }
            }
            ShowOrHideFuncBtn(selected.Count < 1);
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void itmSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                bool enabled = ((ImgControl)imgPanel.Children[i]).SetChecked(true);
                if (enabled && !selected.Contains(i))
                    selected.Add(i);
            }
            ShowOrHideFuncBtn(selected.Count < 1);
        }

        /// <summary>
        /// 全不选
        /// </summary>
        private void itmUnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ((ImgControl)imgPanel.Children[i]).SetChecked(false);
                if (selected.Contains(i))
                    selected.Remove(i);
            }
            ShowOrHideFuncBtn(true);
        }

        /// <summary>
        /// 重试
        /// </summary>
        private void itmReload_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ((ImgControl)imgPanel.Children[i]).RetryLoad();
                StartPreLoad();
            }
        }

        /// <summary>
        /// 屏蔽图片rate 菜单勾选状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itm5_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == itm5)
            {
                if (itm5.IsChecked)
                {
                    itm10.IsChecked = false;
                    itm20.IsChecked = false;
                    itm30.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm10)
            {
                if (itm10.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm20.IsChecked = false;
                    itm30.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm20)
            {
                if (itm20.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm10.IsChecked = false;
                    itm30.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm30)
            {
                if (itm30.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm10.IsChecked = false;
                    itm20.IsChecked = false;
                    itm0.IsChecked = false;
                }
            }
            else if (sender == itm0)
            {
                if (itm0.IsChecked)
                {
                    itm5.IsChecked = false;
                    itm10.IsChecked = false;
                    itm20.IsChecked = false;
                    itm30.IsChecked = false;
                }
            }
        }

        /// <summary>
        /// 屏蔽图片res 菜单勾选状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmx5_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == itmx5)
            {
                if (itmx5.IsChecked)
                {
                    itmx10.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx30.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx10)
            {
                if (itmx10.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx30.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx20)
            {
                if (itmx20.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx10.IsChecked = false;
                    itmx30.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx30)
            {
                if (itmx30.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx10.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx0.IsChecked = false;
                }
            }
            else if (sender == itmx0)
            {
                if (itmx0.IsChecked)
                {
                    itmx5.IsChecked = false;
                    itmx10.IsChecked = false;
                    itmx20.IsChecked = false;
                    itmx30.IsChecked = false;
                }
            }
        }

        /// <summary>
        /// 生成选中图片的下载列表Lst文件
        /// </summary>
        private void itmLst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selected.Count == 0)
                {
                    MessageBox.Show(this, "未选择图片", MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
                {
                    DefaultExt = "lst",
                    FileName = "MoeLoaderList.lst",
                    Filter = "lst文件|*.lst",
                    OverwritePrompt = false
                };
                if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string text = "";
                    int success = 0, repeat = 0;
                    //读存在的lst内容
                    string[] flst = null;
                    bool havelst = File.Exists(saveFileDialog1.FileName);
                    bool isexists = false;

                    if (havelst)
                    {
                        flst = File.ReadAllLines(saveFileDialog1.FileName);
                    }

                    foreach (int i in selected)
                    {
                        Img selectimg = imgs[i];
                        string host = SiteManager.Instance.Sites[comboBoxIndex].ShortName;

                        //查找重复项
                        try
                        {
                            isexists = havelst && flst.Any(x => x.Split('|')[2] == host && x.Split('|')[4] == selectimg.Id.ToSafeString());
                        }
                        catch { }

                        if (!isexists)
                        {
                            List<string> oriUrls = GetImgAddress(selectimg);
                            for (int c = 0; c < oriUrls.Count; c++)
                            {
                                //设图册页数
                                if (oriUrls.Count > 1)
                                {
                                    selectimg.ImgP = c + 1 + "";
                                }

                                //url|文件名|域名|上传者|ID(用于判断重复)
                                text += oriUrls[c]
                                    + "|" + GenFileName(selectimg)
                                    + "|" + host
                                    + "|" + selectimg.Author
                                    + "|" + selectimg.Id
                                    + "\r\n";
                                success++;
                            }
                        }
                        else
                            repeat++;
                    }
                    File.AppendAllText(saveFileDialog1.FileName, text);
                    MessageBox.Show("成功保存 " + success + " 个地址\r\n" + repeat + " 个地址已在列表中\r\n", MainWindow.ProgramName,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(this, "保存失败", MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (downloadC.IsWorking)
            {
                if (
                    MessageBox.Show(this, "正在下载图片，确定要关闭程序吗？未下载完成的图片将丢失",
                    ProgramName,
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question) == MessageBoxResult.Cancel
                    )
                {
                    e.Cancel = true;
                    return;
                }
                //else { isClose = true; }
            }

            if (previewFrm != null && previewFrm.IsLoaded)
            {
                previewFrm.Close();
                //previewFrm = null;
            }
            //prevent from saving invalid window size
            WindowState = WindowState.Normal;
        }

        /// <summary>
        /// setting
        /// </summary>
        private void TextBlock_MouseDown1(object sender, MouseButtonEventArgs e)
        {
            OptionWnd c = new OptionWnd(this);
            c.ShowDialog();
            return;
        }

        /// <summary>
        /// help
        /// </summary>
        private void TextBlock_MouseDown2(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://usaginya.lofter.com/post/1d56d69b_d6b14fd");
            }
            catch { }
        }

        /// <summary>
        /// 使用小缩略图
        /// </summary>
        private void itmSmallPre_Click(object sender, RoutedEventArgs e)
        {
            if (itmSmallPre.IsChecked)
            {
                foreach (UIElement ele in imgPanel.Children)
                {
                    ImgControl img = (ImgControl)ele;
                    int smallx = SiteManager.Instance.Sites[nowSelectedIndex].SmallImgSize.X;
                    int smally = SiteManager.Instance.Sites[nowSelectedIndex].SmallImgSize.Y;
                    if (img != null)
                    {
                        //如果比默认大小还小就用默认大小
                        img.Width = smallx < 170 ? img.Width > 170 ? 170 : 170 : smallx;
                        img.Height = smally < 190 ? img.Height > 190 ? 190 : 190 : smally;
                    }
                    //自适应评分数字区
                    img.brdScr.Width = img.Width / 4;
                }
            }
            else
            {
                foreach (UIElement ele in imgPanel.Children)
                {
                    ImgControl img = (ImgControl)ele;
                    int smallx = SiteManager.Instance.Sites[nowSelectedIndex].SmallImgSize.X;
                    int smally = SiteManager.Instance.Sites[nowSelectedIndex].SmallImgSize.Y;
                    if (img != null)
                    {
                        img.Width = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.X;
                        img.Height = SiteManager.Instance.Sites[nowSelectedIndex].LargeImgSize.Y;
                        img.Width = img.Width <= smallx ? smallx * 1.5 : img.Width;
                        img.Height = img.Height <= smally ? smally * 1.5 : img.Height;
                    }
                    img.brdScr.Width = img.Width / 4;
                }
            }
        }

        /// <summary>
        /// 图片地址类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmTypeOri_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == itmTypeOri)
            {
                if (itmTypeOri.IsChecked)
                {
                    itmTypeJpg.IsChecked = false;
                    itmTypePreview.IsChecked = false;
                    itmTypeSmall.IsChecked = false;
                    addressType = AddressType.Ori;

                }
            }
            else if (sender == itmTypeJpg)
            {
                if (itmTypeJpg.IsChecked)
                {
                    itmTypeOri.IsChecked = false;
                    itmTypePreview.IsChecked = false;
                    itmTypeSmall.IsChecked = false;
                    addressType = AddressType.Jpg;
                }
            }
            else if (sender == itmTypePreview)
            {
                if (itmTypePreview.IsChecked)
                {
                    itmTypeOri.IsChecked = false;
                    itmTypeJpg.IsChecked = false;
                    itmTypeSmall.IsChecked = false;
                    addressType = AddressType.Pre;
                }
            }
            else if (sender == itmTypeSmall)
            {
                if (itmTypeSmall.IsChecked)
                {
                    itmTypeOri.IsChecked = false;
                    itmTypeJpg.IsChecked = false;
                    itmTypePreview.IsChecked = false;
                    addressType = AddressType.Small;
                }
            }

            if (!itmTypeJpg.IsChecked && !itmTypeOri.IsChecked && !itmTypePreview.IsChecked && !itmTypeSmall.IsChecked)
            {
                itmTypeOri.IsChecked = true;
                addressType = AddressType.Ori;
            }
        }

        /// <summary>
        /// 使用的地址类型
        /// </summary>
        enum AddressType { Ori, Jpg, Pre, Small }

        /// <summary>
        /// 获取Img的地址
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private List<string> GetImgAddress(Img img)
        {
            if (img.OrignalUrlList.Count > 0)
            {
                return img.OrignalUrlList;
            }
            else
            {
                string url = img.OriginalUrl;
                switch (addressType)
                {
                    case AddressType.Jpg:
                        url = img.JpegUrl;
                        break;
                    case AddressType.Pre:
                        url = img.SampleUrl;
                        break;
                    case AddressType.Small:
                        url = img.PreviewUrl;
                        break;
                }
                List<string> urls = new List<string>();
                urls.Add(url);
                return urls;
            }
        }

        /// <summary>
        /// 显示或隐藏左下角搜索设置
        /// </summary>
        private void togglePram_Click(object sender, RoutedEventArgs e)
        {
            if (togglePram.IsChecked.Value)
            {
                togglePram.ToolTip = "显示搜索设置";
                Storyboard sb = FindResource("closeParam") as Storyboard;
                sb.Begin();
            }
            else
            {
                togglePram.ToolTip = (string)togglePram.Tag;
                Storyboard sb = FindResource("showParam") as Storyboard;
                sb.Begin();
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (realPage > 1)
            {
                DelayPageTurn(1);
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            DelayPageTurn(2);
        }


        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void bDownload_MouseUp(object sender, MouseButtonEventArgs e)
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (!toggleDownload.IsChecked.Value)
                toggleDownload.IsChecked = true;

            toggleDownload_Click(null, null);

            //添加下载
            if (selected.Count > 0)
            {
                List<MiniDownloadItem> urls = new List<MiniDownloadItem>();
                foreach (int i in selected)
                {
                    Img dlimg = imgs[i];
                    List<string> oriUrls = GetImgAddress(dlimg);
                    for (int c = 0; c < oriUrls.Count; c++)
                    {
                        //设图册页数
                        if (oriUrls.Count > 1)
                        {
                            imgs[i].ImgP = c + 1 + "";
                        }
                        string fileName = GenFileName(dlimg);
                        string domain = SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                        urls.Add(new MiniDownloadItem(fileName, oriUrls[c], domain, dlimg.Author, "", "", dlimg.Id));
                    }
                }
                downloadC.AddDownload(urls);
            }
        }

        /// <summary>
        /// 构建文件名
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private string GenFileName(Img img)
        {
            //namePatter
            string file = namePatter;

            //%site站点 %id编号 %tag标签 %desc描述 %author作者 %date图片时间 %imgid[2]图册中图片编号[补n个零]
            file = file.Replace("%site", SiteManager.Instance.Sites[nowSelectedIndex].ShortName);
            file = file.Replace("%id", img.Id.ToSafeString());
            file = file.Replace("%tag", img.Tags.Replace("\r\n", ""));
            file = file.Replace("%desc", img.Desc.Replace("\r\n", ""));
            file = file.Replace("%author", img.Author);
            file = file.Replace("%date", FormatFileDateTime(img.Date));
            #region 图册页数格式化
            try
            {
                Regex reg = new Regex(@"(?<all>%imgp\[(?<zf>[0-9]+)\])");
                MatchCollection mc = reg.Matches(file);
                Match result;
                string imgpPatter = "";
                int zerofill = 0;
                int resc = mc.Count;

                for (int i = 0; i < resc; i++)
                {
                    result = mc[i];
                    imgpPatter = result.Groups["all"].ToString();
                    zerofill = int.Parse(result.Groups["zf"].ToString());
                    if (string.IsNullOrWhiteSpace(img.ImgP))
                        file = file.Replace(imgpPatter, "0".PadLeft(zerofill, '0'));
                    else
                        file = file.Replace(imgpPatter, img.ImgP.PadLeft(zerofill, '0'));
                }

                if (resc < 1)
                {
                    //如果图册有数量就强制加序号
                    if (int.Parse(img.ImgP) > 0)
                        file += img.ImgP.PadLeft(4, '0');

                    //移除错误的标签格式
                    reg = new Regex(@"%imgp\[.*?\]+");
                    file = reg.Replace(file, "");
                }
            }
            catch { }
            #endregion

            return file;
        }

        /// <summary>
        /// 格式化杂乱字符串为适用于文件名的时间格式
        /// </summary>
        /// <param name="timeStr">时间字符串</param>
        /// <returns></returns>
        private string FormatFileDateTime(string timeStr)
        {
            if (timeStr.Trim() == "") return timeStr;
            //空格切分日期时间
            timeStr = Regex.Replace(timeStr, @"\s", ">");
            //替换英文月份
            string emonth = "Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sept,Oct,Nov,Dec";
            string esmonth = "January,February,March,April,May,June,July,August,September,October,November,December";
            for (int j = 0; j < 2; j++)
            {
                string[] smonth = Regex.Split(j > 0 ? emonth : esmonth, @",");
                int smos = smonth.Length;
                for (int i = 0; i < smos; i++)
                {
                    if (timeStr.Contains(smonth[i], StringComparison.OrdinalIgnoreCase))
                    {
                        timeStr = timeStr.Replace(smonth[i], i + 1 + "");
                    }
                }
            }
            //格式交换
            Match mca = Regex.Match(timeStr, @">(?<num>\d{4})$");
            Match mcb = Regex.Match(timeStr, @">(?<num>\d{4})>");
            string yeara = mca.Groups["num"].ToSafeString();
            string yearb = mcb.Groups["num"].ToSafeString();
            string month = "";
            if (yeara != "")
            {
                mca = Regex.Match(timeStr, @"(?<num>\d+)>");
                month = mca.Groups["num"].ToSafeString();
                timeStr = yeara + new Regex(@"(\d+)>").Replace(timeStr, month, 1);
                timeStr = Regex.Replace(timeStr, yeara + @".*?>" + month, yeara + "<" + month);
            }
            else if (yearb != "")
            {
                mcb = Regex.Match(timeStr, @"(?<num>\d+>\d+)");
                month = mcb.Groups["num"].ToSafeString();
                month = Regex.Replace(month, @">", "<");
                timeStr = Regex.Replace(timeStr, yearb + @">", yearb + "<" + month + ">");
                timeStr = Regex.Replace(timeStr, @".*?>" + yearb, yearb);
            }
            //杂字过滤
            timeStr = Regex.Replace(timeStr, @"[^\d|>]", "<");
            //取时间区域
            timeStr = Regex.Match(timeStr, @"\d[\d|<|>]+[<|>]+\d+").ToString();
            //缩减重复字符
            timeStr = Regex.Replace(timeStr, "<+", "<");
            timeStr = Regex.Replace(timeStr, ">+", ">");
            timeStr = Regex.Replace(timeStr, "[<|>]+[<|>]+", ">");

            if (timeStr.Contains(">"))
            {
                string[] strs = Regex.Split(timeStr, ">");
                timeStr = Regex.Replace(strs[0], "<", "-");
                timeStr = timeStr + "_" + Regex.Replace(strs[1], "<", ".");
            }
            else
            {
                timeStr = Regex.Replace(timeStr, "<", ".");
            }
            return timeStr;
        }

        /// <summary>
        /// 打开站点主页
        /// </summary>
        private void rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(SiteManager.Instance.Sites[comboBoxIndex].SiteUrl);
        }

        /// <summary>
        /// 窗口按键事件处理
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //列表有图片时
            if (imgs != null)
            {
                //列表快捷键
                //排除其它控件焦点快捷键
                if (!txtPage.IsFocused && !txtNum.IsFocused && !searchControl.Textbox.IsFocused && !downloadC.Scrollviewer.IsFocused)
                {
                    if (IsCtrlDown())
                    {
                        if (!isGetting)
                        {
                            if (e.Key == Key.I)
                            {   //反选
                                itmSelectInverse_Click(null, null);
                            }
                            else if (e.Key == Key.A)
                            {   //全选
                                itmSelectAll_Click(null, null);
                            }
                            else if (e.Key == Key.Z)
                            {   //全不选
                                itmUnSelectAll_Click(null, null);
                            }
                        }
                        else if (e.Key == Key.S)
                        {   //停止
                            Button_Click(null, null);
                        }

                        if (e.Key == Key.R)
                        {//重试
                            itmReload_Click(null, null);
                        }
                    }

                    //滚动列表
                    if (e.Key == Key.Down && scrList.ExtentHeight > 0)
                    {
                        //避免焦点跑到其它地方
                        e.Handled = true;
                        //向下滚动列表
                        scrList.ScrollToVerticalOffset(scrList.VerticalOffset + scrList.ViewportHeight * 0.5);
                    }
                    else if (e.Key == Key.Up && scrList.ExtentHeight > 0)
                    {
                        e.Handled = true;
                        //向上滚动列表
                        scrList.ScrollToVerticalOffset(scrList.VerticalOffset - scrList.ViewportHeight * 0.5);
                    }
                    else if (e.Key == Key.Home)
                    {
                        e.Handled = true;
                        //滚动列表到顶部
                        scrList.ScrollToTop();
                    }
                    else if (e.Key == Key.End)
                    {
                        e.Handled = true;
                        //滚动列表到底部
                        scrList.ScrollToBottom();
                    }

                    //左右键翻页
                    if (e.Key == Key.Left)
                    {
                        e.Handled = true;
                        //上一页
                        DelayPageTurn(1);
                    }
                    else if (e.Key == Key.Right)
                    {
                        e.Handled = true;
                        //下一页
                        DelayPageTurn(2);
                    }
                }
            }
        }

        internal static System.Net.IWebProxy WebProxy
        {
            get
            {
                if (ProxyType == ProxyType.Custom)
                {
                    if (Proxy.Length > 0)
                        return new System.Net.WebProxy(Proxy, true);
                }
                else if (ProxyType == ProxyType.None)
                {
                    return null;
                }
                return System.Net.WebRequest.DefaultWebProxy;
            }
        }

        private void itmxExplicit_Click(object sender, RoutedEventArgs e)
        {
            if (!itmxExplicit.IsChecked)
            {
                if (MessageBox.Show(this, "Explicit评分的图片含有限制级内容，请确认您已年满18周岁", MainWindow.ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    itmxExplicit.IsChecked = true;
                }
            }
        }

        private void Window_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
                if (e.ClickCount < 2)
                    ClickMaxButton = false;
            }
            catch { }
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            //WindowStyle = WindowStyle.SingleBorderWindow;
            //WindowState = WindowState.Minimized;
            GlassHelper.SendMessage(Hwnd, 0x0112, 0xF020, IntPtr.Zero);
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                ClickMaxButton = true;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            (new Thread(
                    new ThreadStart(delegate ()
                    {
                        //启动回收
                        GC.Collect();
                        //删除临时目录
                        string tmpath = System.IO.Path.GetTempPath() + "\\Moeloadelta";
                        if (Directory.Exists(tmpath))
                            try
                            {
                                Directory.Delete(tmpath, true);
                            }
                            catch { }
                    })
            )).Start();
            Close();
        }

        /// <summary>
        /// 最大化时拖动还原窗口
        /// </summary>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !ClickMaxButton && WindowState == WindowState.Maximized)
            {
                Max_Click(null, null);
                GlassHelper.POINT mousep = new GlassHelper.POINT();
                GlassHelper.GetCursorPos(out mousep);
                this.Top = mousep.y - 50;
                this.Left = mousep.x - this.Width / 2;
                Window_MouseDown_1(null, null);
            }
        }

        private void Window_StateChanged_1(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                maxBtn.Fill = FindResource("maxB") as DrawingBrush;
            }
            else
            {
                maxBtn.Fill = FindResource("restoreB") as DrawingBrush;
            }

            if (WindowState != WindowState.Minimized)
            {
                int nStyle = GlassHelper.GetWindowLong(Hwnd, -16);
                nStyle &= ~(0x00C00000);
                //WS_CAPTION 0x00C00000L
                GlassHelper.SetWindowLong(Hwnd, -16, nStyle);
            }
            GlassHelper.EnableBlurBehindWindow(containerB, this);
        }

        private void Window_Activated_1(object sender, EventArgs e)
        {
            containerB.BorderBrush = new SolidColorBrush(Color.FromRgb(0x35, 0x85, 0xe4));
        }

        private void Window_Deactivated_1(object sender, EventArgs e)
        {
            byte gray = 0xbc;
            containerB.BorderBrush = new SolidColorBrush(Color.FromRgb(gray, gray, gray));
        }

        private void contentWnd_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            ContentPresenter cp = sender as ContentPresenter;
            Rectangle sn = this.Template.FindName("shadowN", this) as Rectangle;
            sn.Width = cp.ActualWidth + 3;
            Rectangle ss = this.Template.FindName("shadowS", this) as Rectangle;
            ss.Width = cp.ActualWidth + 3;
            Rectangle se = this.Template.FindName("shadowE", this) as Rectangle;
            se.Height = cp.ActualHeight + 3;
            Rectangle sw = this.Template.FindName("shadowW", this) as Rectangle;
            sw.Height = cp.ActualHeight + 3;

            GlassHelper.EnableBlurBehindWindow(containerB, this);
        }

        /// <summary>
        /// 关闭程序
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (currentSession != null)
                currentSession.IsStop = true;

            downloadC.StopAll();

            try
            {
                if (!IsCtrlDown())
                {
                    string words = "";
                    foreach (string word in searchControl.UsedItems)
                    {
                        words += word + "|";
                    }

                    string text = downloadC.NumOnce + "\r\n"
                        + (DownloadControl.SaveLocation == System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                        ? "." : DownloadControl.SaveLocation) + "\r\n" + addressType + ";"
                        + (downloadC.IsSaSave ? "1" : "0") + ";"
                        + numOfLoading + ";"
                        + (itmMaskViewed.IsChecked ? "1" : "0") + ";"
                        + words + ";"
                        + Proxy + ";"
                        + BossKey + ";"
                        + (itmSmallPre.IsChecked ? "1" : "0") + ";"
                        + ProxyType + ";"
                        + (int)ActualWidth + "," + (int)ActualHeight + ";"
                        + (togglePram.IsChecked.Value ? "1" : "0") + ";"
                        + PreFetcher.CachedImgCount + ";"
                        + (downloadC.IsSepSave ? "1" : "0") + ";"
                        + (itmxExplicit.IsChecked ? "1" : "0") + ";"
                        + namePatter + ";"
                        + num + ";"
                        + bgSt + ";"
                        + bgHe + ";"
                        + bgVe + ";"
                        + bgOp + "\r\n";
                    foreach (KeyValuePair<string, ViewedID> id in viewedIds)
                    {
                        text += id.Key + ":" + id.Value + "\r\n";
                    }
                    File.WriteAllText(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                        + "\\Moe_config.ini", text);
                }
            }
            catch { }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Environment.Exit(0);
        }

        #region 线程延迟执行翻页
        /// <summary>
        /// 线程延迟执行翻页
        /// </summary>
        /// <param name="operating">1上一页 2下一页</param>
        private void DelayPageTurn(int operating)
        {
            Thread newThread = null;
            if (operating == 1)
            {
                newThread = new Thread(new ThreadStart(RDelayP));
                newThread.Name = "RDelayP";
            }
            else if (operating == 2 && HaveNextPage)
            {
                newThread = new Thread(new ThreadStart(RDelayN));
                newThread.Name = "RDelayN";
            }

            if (newThread != null)
                newThread.Start();
        }

        private void RDelayP()
        {
            // 如果正在搜索就先停止
            if (isGetting)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
            Thread.Sleep(666);
            if (!isGetting)
            {
                lastPage = realPage;
                realPage--;
                Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
        }

        private void RDelayN()
        {
            // 如果正在搜索就先停止
            if (isGetting)
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
            Thread.Sleep(666);
            if (!isGetting)
            {
                lastPage = realPage;
                realPage++;
                this.Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
        }
        #endregion

    }
}
