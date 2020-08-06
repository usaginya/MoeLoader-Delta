using MoeLoaderDelta.Control;
using MoeLoaderDelta.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

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
    /// Interaction logic for xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 下载工作委托
        /// </summary>
        private delegate void downlwork();

        /// <summary>
        /// 主窗口的句柄
        /// </summary>
        public static IntPtr Hwnd;

        /// <summary>
        /// 程序版本
        /// </summary>
        public static Version ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// 封装当前程序运行目录
        /// </summary>
        public static string ProgramRunPath => System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// 程序名
        /// </summary>
        public static string ProgramName
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string programName = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute))).Title;
                return programName;
            }
        }
        /// <summary>
        /// 收藏夹文件路径
        /// </summary>
        private string FavoritePath => $"{ProgramRunPath}\\MLD_fav.mld";

        private const string IMGLOADING = "少女加载中...";

        private int num = 50, realNum = 50;
        private int page = 1, realPage = 1, lastPage = 1;
        private static string SearchWord = string.Empty;

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
        private int nowSelectedIndex = 0;

        internal List<Img> imgs;
        internal List<int> selected = new List<int>();

        internal FavoriteWnd favoriteWnd;
        internal FavoriteAddWnd favoriteAddWnd;
        internal PreviewWnd previewFrm;
        private SessionState currentSession;
        private LoginSiteArgs loginSiteArgs = new LoginSiteArgs();
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

        //缩略图大小
        private int thumbSize = 300;

        //收藏按钮长按事件任务
        private Thread fav_thread;

        #region -- 拖框选功能变量 --
        private Border dragSelectBorder = null;
        private bool canDrag = false, dragIsCtrl = false, dragIsClear = false;
        private Point dragStartPoint;
        #endregion

        #region ////// 公开可调用对象 ///////
        public static MainWindow MainW;
        public MenuItem SelectedSite;

        /// <summary>
        /// 收藏夹列表
        /// </summary>
        public TreeViewModel FavoriteTreeView = new TreeViewModel();
        #endregion //////////////////////////

        internal int comboBoxIndex = 0;
        internal const string DefaultPatter = "[%site_%id_%author]%desc<!<_%imgp[5]";
        private const string NoFoundMsg = "没有找到图片喔~";
        internal string namePatter = DefaultPatter;

        internal double bgOp = 0.5;
        internal ImageBrush bgImg = null;
        internal Stretch bgSt = Stretch.None;
        internal AlignmentX bgHe = AlignmentX.Right;
        internal AlignmentY bgVe = AlignmentY.Bottom;

        public BitmapImage ExtSiteIconOff { get; set; } = null;
        public BitmapImage ExtSiteIconOn { get; set; } = null;

        #region Register/Unregister HotKey
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
        #endregion

        /// <summary>
        /// 代理设置，eg. 127.0.0.1:1080
        /// </summary>
        internal static string Proxy { set; get; }
        internal static ProxyType ProxyType { get; set; }

        public static string SearchWordPu => DownloadControl.ReplaceInvalidPathChars(SearchWord);
        WindowData.MainLoginSite loginsitedata = new WindowData.MainLoginSite();


        #region Public Functions
        /// <summary>
        /// 当前选则的站点唯一原名、无结果将返回null
        /// </summary>
        public string SelectedSiteName()
        {
            string siteName = SiteManager.Instance.Sites.Count < 1 ? null : SiteManager.Instance.Sites[comboBoxIndex].SiteName;
            if (string.IsNullOrWhiteSpace(siteName)) { return null; }
            int space = siteName.IndexOf('[');
            return space > 0 ? siteName.Substring(0, space).Trim() : siteName;
        }

        /// <summary>
        /// 全部站点唯一原名表
        /// </summary>
        public List<string> AllSitesName()
        {
            List<string> sitesName = new List<string>();
            string before = null;
            foreach (MenuItem item in siteMenu.Items)
            {
                //如果站点和之前重复则跳过
                if ((string)item.Header == before) { continue; }
                before = (string)item.Header;
                sitesName.Add(before);
            }
            return sitesName;
        }
        #endregion

        /// <summary>
        /// ################### Main Start ###################
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = ProgramName;

            btnGet.ToolTip = btnGet.Tag as string;

            if (!File.Exists($"{ProgramRunPath}\\nofont.txt"))
            {
                FontFamily = new FontFamily("Microsoft YaHei");
            }

            //////////////////////////////////// animation style /////////////////////////////////
            LinearGradientBrush brush = new LinearGradientBrush
            {
                StartPoint = new Point(1, 1),
                EndPoint = new Point(0, 0)
            };
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 0, 0, 0), 1));
            Window.OpacityMask = brush;

            /////////////////////////////////////// init image site list //////////////////////////////////
            SiteManager.Mainproxy = WebProxy;
            Dictionary<string, MenuItem> dicSites = new Dictionary<string, MenuItem>();
            ObservableCollection<FrameworkElement> tempSites = new ObservableCollection<FrameworkElement>();

            int index = 0, extindex = 0;
            Stream siteIconStream = null;
            BitmapImage siteIcon = null;

            #region 加载扩展设置图标
            BitmapImage CreateBitmapImage(string resourceName)
            {
                using (siteIconStream = GetType().Assembly.GetManifestResourceStream(resourceName))
                {
                    if (siteIconStream != null)
                    {
                        siteIcon = new BitmapImage { CacheOption = BitmapCacheOption.Default };
                        siteIcon.BeginInit();
                        siteIcon.StreamSource = siteIconStream;
                        siteIcon.EndInit();
                    }
                }
                return siteIcon;
            }
            ExtSiteIconOff = CreateBitmapImage("MoeLoaderDelta.Images.extsetting0.ico");
            ExtSiteIconOn = CreateBitmapImage("MoeLoaderDelta.Images.extsetting1.ico");
            #endregion

            foreach (IMageSite site in SiteManager.Instance.Sites)
            {
                MenuItem menuItem = null;
                MenuItem subItem = null;
                siteIconStream = null;

                //group by shortName
                if (dicSites.ContainsKey(site.ShortName))
                {
                    menuItem = dicSites[site.ShortName];
                }
                else
                {
                    extindex = 0;
                    int space = site.SiteName.IndexOf('[');
                    string siteName = space > 0 ? site.SiteName.Substring(0, space).Trim() : site.SiteName;
                    menuItem = new MenuItem()
                    {
                        Header = siteName
                    };
                    BitmapImage icon = new BitmapImage();
                    icon.BeginInit();
                    icon.StreamSource = site.IconStream;
                    icon.EndInit();
                    TreeViewModel.AddSites(siteName, icon);

                    menuItem.Style = (Style)Resources["SimpleMenuItem"];
                    dicSites.Add(site.ShortName, menuItem);

                    #region 获取站点图标
                    siteIconStream = site.IconStream;
                    if (siteIconStream != null)
                    {
                        siteIcon = new BitmapImage { CacheOption = BitmapCacheOption.Default };
                        siteIcon.BeginInit();
                        siteIcon.StreamSource = site.IconStream;
                        siteIcon.EndInit();
                        menuItem.Icon = siteIcon;
                    }
                    #endregion
                }

                #region 添加主站子菜单项
                if (site.SiteName.Contains("ExtStteing"))
                {
                    if (menuItem.Items.Count > 0 && extindex < 1)
                    {
                        menuItem.Items.Add(new Separator());
                    }
                    subItem = new MenuItem()
                    {
                        Icon = site.ExtendedSettings[extindex].Enable ? ExtSiteIconOn : ExtSiteIconOff,
                        Header = site.ExtendedSettings[extindex].Title,
                        DataContext = index++,
                        Tag = extindex++
                    };
                    subItem.Click += new RoutedEventHandler(RunSiteExtendedSettingAction);
                }
                else
                {
                    subItem = new MenuItem() { Icon = siteIcon, Header = site.SiteName, ToolTip = site.ToolTip, DataContext = index++ };
                    subItem.Click += new RoutedEventHandler(MenuItem_Click);
                }
                subItem.Style = (Style)Resources["SimpleMenuItem"];
                menuItem.Items.Add(subItem);
                #endregion
            }

            #region 添加主站菜单
            foreach (IMageSite site in SiteManager.Instance.Sites)
            {
                MenuItem menuItem = dicSites[site.ShortName];
                if (menuItem == null) { continue; }
                if (menuItem.Items.Count == 1)
                {
                    menuItem = menuItem.Items[0] as MenuItem;
                }
                tempSites.Add(menuItem);
            }
            #endregion

            #region 站点加载自检
            if (SiteManager.Instance.Sites.Count > 0)
            {
                SelectedSite = (MenuItem)tempSites[0];
                siteMenu.ItemsSource = tempSites;
                siteMenu.Header = SiteManager.Instance.Sites[comboBoxIndex].ShortName;
                if (!string.IsNullOrWhiteSpace(SiteManager.Instance.Sites[comboBoxIndex].ShortType))
                {
                    siteMenu.Header += " " + SiteManager.Instance.Sites[comboBoxIndex].ShortType;
                }
                siteMenu.Icon = SelectedSite.Icon;
                siteText.Text = "当前站点 " + SiteManager.Instance.Sites[comboBoxIndex].ShortName;
            }
            else
            {
                string configPath = $"{ProgramRunPath}\\MoeLoaderDelta.exe.config";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(configPath);

                XmlNode root = xmlDoc.SelectSingleNode("/configuration/runtime");
                if (root.SelectSingleNode("loadFromRemoteSources") == null)
                {
                    MessageBox.Show("哎呀 Σ(>Д<。 )");      //不知原因这里消息框会被跳过一次
                    MessageBoxResult msgSelect = MessageBox.Show("初始化站点库发生错误、要尝试备用方案吗？", ProgramName,
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (msgSelect == MessageBoxResult.Yes)
                    {
                        XmlElement insertNode = xmlDoc.CreateElement("loadFromRemoteSources");
                        insertNode.SetAttribute("enabled", "true");
                        root.AppendChild(insertNode);

                        xmlDoc.Save(configPath);
                        Application.Current.Shutdown();
                        System.Windows.Forms.Application.Restart();
                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                    }
                }
                for (int i = 0; i < 2; i++) { MessageBox.Show("站点加载失败 (QAQ )", ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            #endregion

            //comboBox1.ItemsSource = tempSites;
            //comboBox1.SelectedIndex = 0;
            /////////////////////////////////////////////////////////////////////////////////////////////
            /********************  Binding Control Data ***************************/
            itmLoginSite.DataContext = loginsitedata;
            UpdateLoginInfo();
            /******************************************************************/


            viewedIds = new Dictionary<string, ViewedID>(SiteManager.Instance.Sites.Count);

            Proxy = "127.0.0.1:1080";
            ProxyType = ProxyType.System;
            bossKey = System.Windows.Forms.Keys.F9;

            LoadConfig();

            //itmxExplicit.IsChecked = !showExplicit;

            MainW = this;

            //删除上次临时目录
            DelTempDirectory();

            //载入标签收藏、必须在MainW后
            LoadFavorite();
        }

        /// <summary>
        /// 执行站点扩展设置方法
        /// </summary>
        /// <param name="siteSesd"></param>
        private void RunSiteExtendedSettingAction(object sender, RoutedEventArgs e)
        {
            if (SiteManager.Instance.Sites.Count < 1) { return; }

            MenuItem item = sender as MenuItem;
            int index = (int)item.DataContext;

            SiteExtendedSetting siteExtended = SiteManager.Instance.Sites[index].ExtendedSettings[(int)item.Tag];

            Dispatcher.Invoke(siteExtended.SettingAction);
            item.Icon = siteExtended.Enable ? ExtSiteIconOn : ExtSiteIconOff;

            Control_Toast.Show($"{siteExtended.Title}已{(siteExtended.Enable ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 点击选择站点
        /// </summary>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SiteManager.Instance.Sites.Count < 1) { return; }

            SelectedSite = sender as MenuItem;
            comboBoxIndex = (int)SelectedSite.DataContext;
            siteMenu.Header = SiteManager.Instance.Sites[comboBoxIndex].ShortName;
            if (!string.IsNullOrWhiteSpace(SiteManager.Instance.Sites[comboBoxIndex].ShortType))
            {
                siteMenu.Header += " " + SiteManager.Instance.Sites[comboBoxIndex].ShortType;
            }
            siteMenu.Icon = (SelectedSite.Parent as MenuItem).Header.ToString() == SelectedSite.Header.ToString()
                ? SelectedSite.Icon : (SelectedSite.Parent as MenuItem).Icon;
            //functionality support check
            itmLoginSite.IsEnabled = !string.IsNullOrWhiteSpace(SiteManager.Instance.Sites[comboBoxIndex].LoginURL);
            stackPanel1.IsEnabled = SiteManager.Instance.Sites[comboBoxIndex].IsSupportCount;
            itmMaskScore.IsEnabled = SiteManager.Instance.Sites[comboBoxIndex].IsSupportScore;
            itmMaskRes.IsEnabled = SiteManager.Instance.Sites[comboBoxIndex].IsSupportRes;
            UpdateLoginInfo();
        }

        /// <summary>
        /// 窗口载入完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logo = FindResource("logoRotate") as Storyboard;

            BossKey = bossKey;

            GlassHelper.EnableBlurBehindWindow(containerB, this);
            new Thread(new ThreadStart(LoadBgImg)).Start();
            //初始化右键菜单
            scrList.ContextMenu.IsOpen = true;
            scrList.ContextMenu.IsOpen = false;
        }

        /// <summary>
        /// 更新菜单中登录站点的用户名
        /// </summary>
        private void UpdateLoginInfo()
        {
            string tmp_user = null;
            IMageSite site = SiteManager.Instance.Sites[comboBoxIndex];
            if (SiteManager.Instance.Sites.Count > 0)
            {
                itmLoginSite.IsEnabled = !string.IsNullOrWhiteSpace(site.LoginURL);
            }

            if (itmLoginSite.IsEnabled && site.LoginSiteIsLogged)
            {
                tmp_user = site.LoginUser;
            }
            loginsitedata.Loginuser = string.IsNullOrWhiteSpace(tmp_user) ? "登录站点" : $"{tmp_user}已登录";
        }

        /// <summary>
        /// 创建不占用文件的BitmapImage
        /// </summary>
        private BitmapImage AsynBitmapImage(string filePath)
        {
            BitmapImage bitmapImage;
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                FileInfo fi = new FileInfo(filePath);
                byte[] bytes = reader.ReadBytes((int)fi.Length);
                reader.Close();

                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(bytes);
                bitmapImage.EndInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                reader.Dispose();
            }
            return bitmapImage;
        }

        /// <summary>
        /// 加载背景图
        /// </summary>
        private void LoadBgImg()
        {
            string bgPath = $"{ProgramRunPath}\\bg.";
            bool hasBg = false;
            string[] bgExt = { "png", "jpg", "gif" };
            int bgExtLength = bgExt.Length;

            for (int i = 0; i < bgExtLength; i++)
            {
                hasBg = File.Exists(bgPath + bgExt[i]);
                if (hasBg)
                {
                    bgPath += bgExt[i];
                    break;
                }
            }
            if (!hasBg) { return; }

            Dispatcher.Invoke(new VoidDel(delegate
            {
                bgImg = new ImageBrush(AsynBitmapImage(bgPath))
                {
                    Stretch = bgSt,
                    AlignmentX = bgHe,
                    AlignmentY = bgVe,
                    Opacity = bgOp,
                };
                grdBg.Background = bgImg;
            }));
        }

        /// <summary>
        /// 更改窗口背景
        /// </summary>
        /// <param name="loadBg">是否从文件加载背景图片</param>
        public void ChangeBg(double opacity, bool loadBg = false)
        {
            opacity = opacity < 0.1 ? 0.1 : opacity > 1 ? 1 : opacity;
            bgOp = opacity;
            //从文件加载更改
            if (loadBg) { LoadBgImg(); return; }

            //从内存更改
            if (bgImg == null) { return; }
            Dispatcher.Invoke(new VoidDel(delegate
            {
                ImageBrush newBg = bgImg.Clone();
                newBg.Opacity = opacity;
                grdBg.Background = newBg;
            }));
        }

        public static string IsNeedReferer(string url)
        {
            Uri uri = new Uri(url);
            List<IMageSite> ISites = SiteManager.Instance.Sites;

            foreach (IMageSite site in SiteManager.Instance.Sites)
            {
                if (site.SubReferer != null)
                {
                    string[] subrefs = site.SubReferer.Split(',');
                    foreach (string sref in subrefs)
                    {
                        if (uri.Host.Contains(sref))
                        {
                            return site.Referer;
                        }
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
            string configFile = $"{ProgramRunPath}\\Moe_config.ini";

            //读取配置文件
            if (File.Exists(configFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(configFile);

                    if (Regex.IsMatch(lines[0], @"^[+-]?\d*$"))
                    {
                        try
                        {
                            checked { downloadC.NumOnce = int.Parse(lines[0]); }
                        }
                        catch (OverflowException)
                        {
                            downloadC.NumOnce = 2;
                        }
                    }
                    else
                    {
                        downloadC.NumOnce = 2;
                    }

                    if (lines[1] != "." && Directory.Exists(lines[1]))
                        DownloadControl.SaveLocation = lines[1];

                    if (lines[2].Contains(';'))
                    {
                        string[] parts = lines[2].Split(';');
                        //itmJpg.IsChecked = parts[0].Equals("1");
                        addressType = (AddressType)Enum.Parse(typeof(AddressType), parts[0]);

                        if (parts.Length > 1)
                        {
                            try
                            {
                                int tpart = Convert.ToInt32(parts[1]);
                                downloadC.IsSscSave = tpart > 1;
                                downloadC.IsSaSave = tpart > 0 && tpart < 3;
                            }
                            catch { }
                        }
                        if (parts.Length > 2)
                        {
                            if (Regex.IsMatch(parts[2], @"^[+-]?\d*$"))
                            {
                                try
                                {
                                    checked
                                    {
                                        numOfLoading = int.Parse(parts[2]);
                                        if (numOfLoading < 4) numOfLoading = 5;
                                    }
                                }
                                catch (OverflowException)
                                {
                                    numOfLoading = 5;
                                }
                            }
                            else
                            {
                                numOfLoading = 5;
                            }
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
                                searchControl.LoadUsedItems(word);
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
                            thumbSize = int.Parse(parts[7]);
                            thumbSize = thumbSize < 150 ? 150 : thumbSize > 500 ? 500 : thumbSize;
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
                                togglePram.ToolTip = "显示搜索设定";
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
                    MessageBox.Show(this, "读取配置文件失败\r\n" + ex.Message, ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
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
        /// 获取子控件
        /// </summary>
        public List<T> GetChildObjects<T>(DependencyObject obj) where T : FrameworkElement
        {
            DependencyObject child = null;
            List<T> childList = new List<T>();

            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);

                if (child is T)
                {
                    childList.Add((T)child);
                }
                childList.AddRange(GetChildObjects<T>(child));
            }
            return childList;
        }

        /// <summary>
        /// 载入收藏夹
        /// </summary>
        private void LoadFavorite()
        {
            if (SiteManager.Instance.Sites.Count < 1) { return; }
            TreeViewModel.LoadFavoriteFile(FavoritePath);
            TreeViewModel.HideEmptySite();
        }

        /// <summary>
        /// 保存收藏夹
        /// </summary>
        public void SaveFavorite()
        {
            if (SiteManager.Instance.Sites.Count < 1) { return; }
            TreeViewModel.SaveFavoriteFile(FavoritePath);
        }

        /// <summary>
        /// 收藏夹按钮按下事件 长按
        /// </summary>
        private void BtnFav_PreviewMouseDown(object sender, MouseButtonEventArgs args)
        {
            btnFav.Tag = 1;
            if (fav_thread != null) { try { fav_thread.Abort(); } catch { } }

            fav_thread = new Thread(() =>
            {
                Thread.Sleep(500);

                Dispatcher.Invoke(new Action(() =>
                {
                    if (btnFav.Tag != null && (int)btnFav.Tag == 1)
                    {
                        btnFav.Tag = 0;
                        Point pMouse = args.GetPosition(btnFav);
                        if (pMouse.X >= 0 && pMouse.X < btnFav.ActualWidth && pMouse.Y >= 0 && pMouse.Y < btnFav.ActualHeight)
                        {
                            if (favoriteAddWnd != null && favoriteAddWnd.IsLoaded)
                            {
                                favoriteAddWnd.Activate();
                                favoriteAddWnd.Top = Top + (ActualHeight / 2) - (favoriteAddWnd.ActualHeight / 2);
                                favoriteAddWnd.Left = Left + (ActualWidth / 2) - (favoriteAddWnd.ActualWidth / 2);
                            }
                            else if (!searchControl.Text.IsNullOrEmptyOrWhiteSpace())
                            {
                                favoriteAddWnd = new FavoriteAddWnd(searchControl.Text, FavoriteAddWnd.AddMode.Add, null, null, SelectedSiteName(), this);
                                favoriteAddWnd.ShowDialog();
                            }
                            else
                            {
                                Control_Toast.Show("搜索框中没有可以收藏的关键词", Toast.MsgType.Warning, 2000);
                            }
                        }
                    }
                }));

            });
            fav_thread.Start();
        }

        /// <summary>
        /// 收藏夹按钮放开事件 短按
        /// </summary>
        private void BtnFav_PreviewMouseUp(object sender, MouseButtonEventArgs args)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (btnFav.Tag != null && (int)btnFav.Tag == 1)
                {
                    btnFav.Tag = 2;

                    Point pMouse = args.GetPosition(btnFav);
                    if (pMouse.X >= 0 && pMouse.X < btnFav.ActualWidth && pMouse.Y >= 0 && pMouse.Y < btnFav.ActualHeight)
                    {
                        if (favoriteWnd != null && favoriteWnd.IsLoaded)
                        {
                            favoriteWnd.Activate();
                        }
                        else
                        {
                            favoriteWnd = new FavoriteWnd(this);
                            favoriteWnd.Show();
                        }
                    }
                }
            }));
        }

        /// <summary>
        /// 启用翻页按钮
        /// </summary>
        /// <param name="btnid">0上一页, 1下一页</param>
        private void UpdatePreNextEnable(int btnid)
        {
            switch (btnid)
            {
                case 0:
                    if (realPage > 1 && !btnPrev.IsEnabled)
                    {
                        btnPrev.IsEnabled = true;
                        btnPrev.Visibility = Visibility.Visible;
                        PlayPreNextAnimation(btnid);
                    }
                    break;
                case 1:
                    if (!btnNext.IsEnabled)
                    {
                        btnNext.IsEnabled = true;
                        btnNext.Visibility = Visibility.Visible;
                        PlayPreNextAnimation(btnid);
                    }
                    break;
            }
        }
        /// <summary>
        /// 启用上一页按钮
        /// </summary>
        private void UpdatePreNextEnable()
        {
            UpdatePreNextEnable(0);
        }

        /// <summary>
        /// 禁用翻页按钮
        /// </summary>
        private void UpdatePreNextDisable()
        {
            btnPrev.IsEnabled = btnNext.IsEnabled = false;
            btnPrev.Visibility = btnNext.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 翻页按钮动画
        /// </summary>
        /// <param name="btnid">0 上一页, 1下一页</param>
        private void PlayPreNextAnimation(int btnid)
        {
            Thickness mrg = (scrList.ComputedVerticalScrollBarVisibility == Visibility.Collapsed) ? new Thickness(0) : new Thickness(0, 0, 15, 0);
            ThicknessAnimation btna = new ThicknessAnimation
            {
                To = mrg,
                Duration = TimeSpan.FromMilliseconds(666)
            };
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
                statusText.Text = "加载完毕，得到 0 张图片";

                txtGet.Text = "搜索";
                btnGet.ToolTip = btnGet.Tag as string;
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
                Control_Toast.Show(NoFoundMsg);
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
                    Control_Toast.Show(NoFoundMsg);
                    return;
                }

                //生成缩略图控件
                for (int i = 0; i < imgs.Count; i++)
                {
                    //int id = Int32.Parse(imgs[i].Id);

                    ImgControl img = new ImgControl(imgs[i], i, SiteManager.Instance.Sites[nowSelectedIndex])
                    {
                        Width = thumbSize,
                        Height = thumbSize
                    };
                    img.imgDLed += Img_imgDLed;
                    img.imgClicked += Img_Click;
                    img.ImgLoaded += Img_ImgLoaded;
                    img.checkedChanged += Img_checkedChanged;

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
        private void Img_imgDLed(object sender, EventArgs e)
        {
            int index = (int)sender;
            Img dlimg = imgs[index];
            List<string> oriUrls = GetImgAddress(dlimg);
            for (int c = 0; c < oriUrls.Count; c++)
            {
                //设图册页数
                if (oriUrls.Count > 1)
                {
                    dlimg.ImgP = c + 0 + string.Empty;
                }
                string fileName = GenFileName(dlimg, oriUrls[c]);
                string domain = SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                downloadC.AddDownload(new MiniDownloadItem[] {
                    new MiniDownloadItem(fileName, oriUrls[c], domain, dlimg.Author, string.Empty, string.Empty, dlimg.Id, dlimg.NoVerify)
                });
            }
           ((ImgControl)imgPanel.Children[index]).SetChecked(false);
            //重置重试次数
            downloadC.ResetRetryCount();
            Control_Toast.Show($"{dlimg.Id} 图片已添加到下载列表 →", Toast.MsgType.Success);
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
        void Img_checkedChanged(object sender, EventArgs e)
        {
            int preid = selected.Count == 0 ? -1 : selected[selected.Count - 1];

            int id = (int)sender;
            if (selected.Contains(id))
                selected.Remove(id);
            else selected.Add(id);
            if (previewFrm != null)
                previewFrm.ChangePreBtnText();

            if (IsShiftDown())
            {
                //批量选择
                for (int i = preid + 1; i < id; i++)
                {
                    bool enabled = ((ImgControl)imgPanel.Children[i]).SetChecked(true);
                    if (enabled && !selected.Contains(i))
                        selected.Add(i);
                    if (previewFrm != null)
                        previewFrm.ChangePreBtnText();
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
        void Img_ImgLoaded(object sender, EventArgs e)
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
                //预加载
                StartPreLoad();
                //显示上一页按钮
                UpdatePreNextEnable();
            }

            //只要有下一页就显示翻页按钮
            if (HaveNextPage)
                UpdatePreNextEnable(1);
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
                        //如果搜索结束时才有翻页就显示翻页按钮
                        if (HaveNextPage && !tmphave && IsLoaded && !isGetting)
                            UpdatePreNextEnable(1);
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
            {
                Control_Toast.Show("没有站点可以用来搜索", Toast.MsgType.Warning);
                return;
            }

            Thread thread_getting = null;

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

                UpdatePreNextDisable();

                if (sender != null)
                {
                    //记录当前页面
                    lastPage = realPage;
                    //使用搜索框关键字搜索
                    SearchWord = searchControl.Text;

                    //由点击搜索按钮触发，所以使用界面上的设定
                    realNum = num;
                    realPage = IsShiftDown() ? lastPage : page;
                    nowSelectedIndex = comboBoxIndex;
                    siteText.Text = "当前站点 " + SiteManager.Instance.Sites[nowSelectedIndex].SiteName;
                }

                pageText.Text = "当前页码 " + realPage;

                bgLoading.Visibility = Visibility.Visible;
                logo.Begin();

                //nowSelectedIndex = comboBoxIndex;

                statusText.Text = "正在搜索.. " + SearchWord;

                if (SearchWord.Length != 0)
                {
                    //一次最近搜索词
                    searchControl.AddUsedItem(SearchWord);
                }

                showExplicit = !itmxExplicit.IsChecked;
                //string url = PrepareUrl(realPage);
                //nowSession = new ImgSrcProcessor(MaskInt, MaskRes, url, SrcType, LastViewed, MaskViewed);
                //nowSession.processComplete += new EventHandler(ProcessHTML_processComplete);
                //(new System.Threading.Thread(new System.Threading.ThreadStart(nowSession.ProcessSingleLink))).Start();
                currentSession = new SessionState();

                thread_getting = new Thread(new ParameterizedThreadStart((o) =>
                {
                    List<Img> imgList = null;
                    try
                    {
                        //prefetch
                        string pageString = PreFetcher.Fetcher.GetPreFetchedPage(
                            realPage, realNum, Uri.EscapeDataString(SearchWord), SiteManager.Instance.Sites[nowSelectedIndex]);
                        imgList = !string.IsNullOrWhiteSpace(pageString)
                            ? SiteManager.Instance.Sites[nowSelectedIndex].GetImages(pageString, WebProxy)
                            : SiteManager.Instance.Sites[nowSelectedIndex].GetImages(realPage, realNum, Uri.EscapeDataString(SearchWord), WebProxy);

                        //过滤图片列表
                        imgList = SiteManager.Instance.Sites[nowSelectedIndex].FilterImg(
                            imgList, MaskInt, MaskRes, LastViewed, MaskViewed, showExplicit, true);
                    }
                    catch (Exception ex)
                    {
                        if (!(o as SessionState).IsStop)
                        {
                            Control_Toast.Show(
                                $"错误：{(string.IsNullOrWhiteSpace(ex.Message) ? "没有找到图片" : ex.Message)}"
                                + $"\r\n获取图片：{(string.IsNullOrWhiteSpace(SearchWord) ? "<默认搜索>" : SearchWord) }"
                                + $"\r\n站点名称：{SiteManager.Instance.Sites[nowSelectedIndex].SiteName}  "
                                + $"\r\n当前页码：{realPage}  每页数量：{realNum}  代理模式：{ProxyType}"
                                , Toast.MsgType.Error, 8000);
                        }
                    }
                    if (!(o as SessionState).IsStop)
                    {
                        Dispatcher.BeginInvoke(new UIdelegate(LoadComplete), imgList);
                    }
                }));
                thread_getting.Start(currentSession);
                GC.Collect(2, GCCollectionMode.Optimized);
            }
            else
            {
                try { thread_getting.Abort(); } catch { }
                if (statusText.Text == IMGLOADING)
                {
                    for (int i = 0; i < imgs.Count; i++)
                    {
                        if (!loaded.Contains(i)) { ((ImgControl)imgPanel.Children[i]).StopLoadImg(); }
                    }
                    unloaded.Clear();
                }

                currentSession.IsStop = true;
                statusText.Text = "加载完毕，得到 0 张图片";
                siteText.Text = "当前站点 " + SiteManager.Instance.Sites[nowSelectedIndex].ShortName;

                //尝试加载下一页
                if (!HaveNextPage) { StartPreLoad(); }

                //显示上一页按钮
                UpdatePreNextEnable();

                isGetting = false;
                txtGet.Text = "搜索";
                btnGet.ToolTip = btnGet.Tag as string;
                imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

                logo.Stop();
                bgLoading.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 执行预加载
        /// </summary>
        private void StartPreLoad()
        {
            PreFetcher.Fetcher.PreListLoaded += Fetcher_PreListLoaded;
            PreFetcher.Fetcher.PreFetchPage(realPage + 1, realNum,
                Uri.EscapeDataString(SearchWord), SiteManager.Instance.Sites[nowSelectedIndex]);
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
                Dispatcher.Invoke(new VoidDel(delegate { mask = itmMaskViewed.IsChecked && SearchWord.Length == 0; })); return mask;
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
        private void DocumentCompleted()
        {
            logo.Stop();
            bgLoading.Visibility = Visibility.Hidden;

            int viewedC = 0;
            try
            {
                viewedC = imgs[imgs.Count - 1].Id - LastViewed.ViewedBiggestId;
            }
            catch { }
            string strSW = SearchWord.Length > 0 ? "，" + SearchWord : string.Empty;
            if (viewedC < 5 || SearchWord.Length > 0)
                statusText.Text = "加载完毕，得到 " + imgs.Count + " 张图片";
            else
                statusText.Text = "加载完毕，得到 " + imgs.Count + " 张图片(剩余约 " + viewedC + " 张未浏览)";
            statusText.Text += strSW;

            //statusText.Text = "搜索完成！得到 " + imgs.Count + " 张图片信息 (上次浏览至 " + viewedIds[nowSelectedIndex].ViewedBiggestId + " )";
            txtGet.Text = "搜索";
            btnGet.ToolTip = btnGet.Tag as string;
            isGetting = false;
            imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

            //System.Media.SystemSounds.Beep.Play();
            if (GlassHelper.GetForegroundWindow() != Hwnd)
            {
                GlassHelper.FlashWindow(Hwnd, true);
            }

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
        private void ToggleDownload_Click(object sender, RoutedEventArgs e)
        {
            Storyboard sb;

            if (toggleDownload.IsChecked.Value)
            {
                toggleDownload.ToolTip = "隐藏下载面板";
                sb = (Storyboard)FindResource("showDownload");

                if (IsCtrlDown())
                {
                    double rmrg = MainW.Width / 2;
                    ((ThicknessAnimationUsingKeyFrames)sb.Children[0]).KeyFrames[0].Value = new Thickness(0, 0, rmrg, 0);
                    ((DoubleAnimationUsingKeyFrames)sb.Children[2]).KeyFrames[0].Value = rmrg;
                }
                else
                {
                    ((ThicknessAnimationUsingKeyFrames)sb.Children[0]).KeyFrames[0].Value = new Thickness(0, 0, 220, 0);
                    ((DoubleAnimationUsingKeyFrames)sb.Children[2]).KeyFrames[0].Value = 220;
                }

                if (grdNavi.HorizontalAlignment == HorizontalAlignment.Center) { grdNavi.Visibility = Visibility.Hidden; }

                sb.Begin();
            }
            else
            {
                grdNavi.Visibility = Visibility.Visible;
                toggleDownload.ToolTip = (string)toggleDownload.Tag;
                sb = (Storyboard)FindResource("closeDownload");
                sb.Begin();
            }
            sb.Completed += ToggleDownloadAni_Completed;
        }

        /// <summary>
        /// 点击缩略图列表时收起下载列表
        /// </summary>
        private void HiddenToggleDownload(object sender, MouseButtonEventArgs e)
        {
            if (e.Source.GetType() == typeof(DownloadControl)) { return; }
            if (toggleDownload.IsChecked.Value)
            {
                toggleDownload.IsChecked = false;
                ToggleDownload_Click(sender, null);
            }
        }

        /// <summary>
        /// 下载列表动画结束时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleDownloadAni_Completed(object sender, EventArgs e)
        {
            PlayPreNextAnimation();
            PlayPreNextAnimation(1);
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
                    Visibility = Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
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
        private void TxtPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 || e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key == Key.Back
                || e.Key == Key.Delete || e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.LeftShift || e.Key == Key.Left
                || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down))
            {
                e.Handled = true;
            }
        }

        private void TxtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt.Text.Length == 0) return;
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

        private void TxtPage_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            try
            {
                num = int.Parse(txtNum.Text);
                page = int.Parse(txtPage.Text);

                num = num > 0 ? (num > 600 ? 600 : num) : 1;
                page = page > 0 ? (page > 99999 ? 99999 : page) : 1;

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

        private void PageUp_Click(object sender, RoutedEventArgs e)
        {
            if (page < 99999)
                txtPage.Text = (page + 1).ToString();
        }

        private void PageDown_Click(object sender, RoutedEventArgs e)
        {
            if (page > 1)
                txtPage.Text = (page - 1).ToString();
        }

        private void NumUp_Click(object sender, RoutedEventArgs e)
        {
            if (num < 600)
                txtNum.Text = (num + 1).ToString();
        }

        private void NumDown_Click(object sender, RoutedEventArgs e)
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
            return (GetAsyncKeyState(key) & 0x8000) == 0x8000 ? true : false;
        }
        public static bool IsCtrlDown()
        {
            return IsKeyDown(System.Windows.Forms.Keys.LControlKey) || IsKeyDown(System.Windows.Forms.Keys.RControlKey) ? true : false;
        }
        public static bool IsShiftDown()
        {
            return IsKeyDown(System.Windows.Forms.Keys.LShiftKey) || IsKeyDown(System.Windows.Forms.Keys.RShiftKey) ? true : false;
        }
        #endregion

        /// <summary>
        /// 预览图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Img_Click(object sender, EventArgs e)
        {
            int index = (int)sender;

            //不支持预览的格式使用浏览来源页
            string supportFormat = "jpg jpeg png bmp gif",
             videoFormat = "mp4 webm avi mpg flv",
             ext = BooruProcessor.FormattedImgUrl(string.Empty, imgs[index].PreviewUrl.Substring(imgs[index].PreviewUrl.LastIndexOf('.') + 1)),
             videoExe = DataHelpers.GetFileExecutable("mp4");

            //使用关联视频播放预览
            if (videoFormat.Contains(ext) && !string.IsNullOrWhiteSpace(videoExe))
            {
                try
                {
                    if (imgs[index].DetailUrl.Length > 0)
                        System.Diagnostics.Process.Start(videoExe, imgs[index].OriginalUrl);
                }
                catch (Exception) { }
                return;
            }
            else if (!supportFormat.Contains(ext) || videoFormat.Contains(ext))
            {
                try
                {
                    if (imgs[index].DetailUrl.Length > 0)
                        System.Diagnostics.Process.Start(imgs[index].DetailUrl);
                }
                catch (Exception) { }
                return;
            }

            if (previewFrm == null || !previewFrm.IsLoaded)
            {
                previewFrm = new PreviewWnd(this);
                previewFrm.Show();
                if (!IsShiftDown()) { Focus(); }
            }
            previewFrm.AddPreview(imgs[index], index, SiteManager.Instance.Sites[nowSelectedIndex].Referer);
            if (IsShiftDown())
            {
                previewFrm.Focus();
                previewFrm.SwitchPreview(imgs[index].Id);
            }
        }

        /// <summary>
        /// excuse me?
        /// </summary>
        /// <param name="index"></param>
        public void SelectByIndex(int index)
        {
            //判断是否选中
            //(imgPanel.Children[index] as ImgControl).SetChecked(true);
            if (selected.Contains(index))
            {
                //下面会添加或删除index
                (imgPanel.Children[index] as ImgControl).SetChecked(false);
            }
            else
            {
                (imgPanel.Children[index] as ImgControl).SetChecked(true);
            }
        }

        /// <summary>
        /// 反选
        /// </summary>
        private void ItmSelectInverse_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ImgControl imgc = (ImgControl)imgPanel.Children[i];

                imgc.SetChecked(!selected.Contains(i));
                if (previewFrm != null)
                    previewFrm.ChangePreBtnText();
            }
            ShowOrHideFuncBtn(selected.Count < 1);
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void ItmSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                bool enabled = ((ImgControl)imgPanel.Children[i]).SetChecked(true);
                if (enabled && !selected.Contains(i))
                    selected.Add(i);
                if (previewFrm != null)
                    previewFrm.ChangePreBtnText();
            }
            ShowOrHideFuncBtn(selected.Count < 1);
        }

        /// <summary>
        /// 全不选
        /// </summary>
        private void ItmUnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ((ImgControl)imgPanel.Children[i]).SetChecked(false);
                if (selected.Contains(i))
                    selected.Remove(i);
                if (previewFrm != null)
                    previewFrm.ChangePreBtnText();
            }
            ShowOrHideFuncBtn(true);
        }

        /// <summary>
        /// 重试
        /// </summary>
        private void ItmReload_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ((ImgControl)imgPanel.Children[i]).RetryLoad();
            }
            StartPreLoad();
        }

        /// <summary>
        /// 屏蔽图片rate 菜单勾选状态
        /// </summary>
        private void Itm5_Checked(object sender, RoutedEventArgs e)
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
        private void Itmx5_Checked(object sender, RoutedEventArgs e)
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
        /// 登录站点
        /// </summary>
        private void ItmLoginSite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IMageSite site = SiteManager.Instance.Sites[comboBoxIndex];
                string LoginURL = site.LoginURL, helpUrl = site.LoginHelpUrl;

                if (!string.IsNullOrWhiteSpace(LoginURL))
                {
                    //显示登录教程提示
                    if (!string.IsNullOrWhiteSpace(helpUrl))
                    {
                        MessageBoxResult result = MessageBox.Show("需要查看登录教程吗？", $"登录{site.ShortName}",
                           MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(helpUrl);
                        }
                        else if (result == MessageBoxResult.Cancel) { return; }
                    }

                    //登录信息输入
                    SingleTextInputWnd inputWnd;
                    string inputTitle = $"登录{site.ShortName}";
                    if (LoginURL == SiteManager.SiteLoginType.FillIn.ToSafeString())
                    {
                        inputWnd = new SingleTextInputWnd(this, inputTitle, null, "输入登录账号");
                        inputWnd.InputResultEvent += new SingleTextInputWnd.InputValueHandler(LoginInputEvent);
                        inputWnd.ShowDialog();
                        if (string.IsNullOrWhiteSpace(loginSiteArgs.User)) { return; }

                        inputWnd = new SingleTextInputWnd(this, inputTitle, null, "输入登录密码");
                        inputWnd.InputResultEvent += new SingleTextInputWnd.InputValueHandler(LoginInputEvent);
                        inputWnd.ShowDialog();
                        if (string.IsNullOrWhiteSpace(loginSiteArgs.Pwd)) { loginSiteArgs.User = null; ItmLoginSite_Click(sender, e); }
                        SiteManager.LoginSiteCall(site, loginSiteArgs);
                    }
                    else if (LoginURL == SiteManager.SiteLoginType.Cookie.ToSafeString())
                    {
                        inputWnd = new SingleTextInputWnd(this, inputTitle, null, "输入Cookie");
                        inputWnd.InputResultEvent += new SingleTextInputWnd.InputValueHandler(LoginInputEvent);
                        inputWnd.ShowDialog();
                        if (string.IsNullOrWhiteSpace(loginSiteArgs.Cookie)) { return; }
                        SiteManager.LoginSiteCall(site, loginSiteArgs);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 登录站点输入完成事件
        /// </summary>
        private void LoginInputEvent(object sender, SingleTextInputEventArgs e)
        {
            string LoginURL = SiteManager.Instance.Sites[comboBoxIndex].LoginURL;

            if (LoginURL == SiteManager.SiteLoginType.FillIn.ToSafeString())
            {
                if (string.IsNullOrWhiteSpace(loginSiteArgs.User))
                {
                    string siteuser = e.ToStringArray()[0];
                    if (string.IsNullOrWhiteSpace(siteuser)) { siteuser = string.Empty; }
                    loginSiteArgs.User = siteuser;
                }
                else
                {
                    string sitepwd = e.ToStringArray()[0];
                    if (string.IsNullOrWhiteSpace(sitepwd)) { sitepwd = string.Empty; }
                    loginSiteArgs.Pwd = sitepwd;
                }
            }
            else if (LoginURL == SiteManager.SiteLoginType.Cookie.ToSafeString())
            {
                string cookie = e.ToStringArray()[0];
                if (string.IsNullOrWhiteSpace(cookie)) { cookie = string.Empty; }
                loginSiteArgs.Cookie = cookie;
            }
        }

        /// <summary>
        /// 打开站点主页
        /// </summary>
        private void ItmOpenSite_Click(object sender, RoutedEventArgs e)
        {
            if (SiteManager.Instance.Sites.Count > 0) { System.Diagnostics.Process.Start(SiteManager.Instance.Sites[comboBoxIndex].SiteUrl); }
        }

        /// <summary>
        /// 弹出右键菜单时处理
        /// </summary>
        private void ScrList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            UpdateLoginInfo();
            SetThumbSize();
        }

        private void CanExecute_LoginSite(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// 生成选中图片的下载列表Lst文件
        /// </summary>
        private void ItmLst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selected.Count == 0)
                {
                    MessageBox.Show(this, "未选择图片", ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    string text = string.Empty;
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
                                    selectimg.ImgP = c + 0 + string.Empty;
                                }

                                //url|文件名|域名|上传者|ID(用于判断重复)
                                text += oriUrls[c]
                                    + "|" + GenFileName(selectimg, oriUrls[c])
                                    + "|" + host
                                    + "|" + selectimg.Author
                                    + "|" + selectimg.Id
                                    + "|" + (selectimg.NoVerify ? 'v' : 'x')
                                    + "|" + SearchWordPu
                                    + "\r\n";
                                success++;
                            }
                        }
                        else
                            repeat++;
                    }
                    File.AppendAllText(saveFileDialog1.FileName, text);
                    MessageBox.Show("成功保存 " + success + " 个地址\r\n" + repeat + " 个地址已在列表中\r\n", ProgramName,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(this, "保存失败", ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// setting
        /// </summary>
        private void TextBlock_MouseDown1(object sender, MouseButtonEventArgs e)
        {
            new OptionWnd(this).ShowDialog();
        }

        /// <summary>
        /// help
        /// </summary>
        private void TextBlock_MouseDown2(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/usaginya/MoeLoader-Delta/issues");
            }
            catch { }
        }

        private void TextBlock_MouseDownUpdateInfo(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://raw.githubusercontent.com/usaginya/mkAppUpInfo/master/MoeLoader-Delta/UpdateLog/update_history.txt");
            }
            catch { }
        }

        private void TextBlock_MouseDownDonate(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://usaginya.github.io/mkAppUpInfo/moeloaderdelta/donate");
            }
            catch { }
        }

        /// <summary>
        /// 缩略图滑块被改变
        /// </summary>
        private void SliderThumbValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            thumbSize = Convert.ToInt32(e.NewValue);
            SetThumbSize(true);
        }

        /// <summary>
        /// 设置缩略图大小
        /// </summary>
        private void SetThumbSize(bool changed = false)
        {
            if (!changed)
            {
                if (!(itmThumbSize.Template.FindName("sliThumbSize", itmThumbSize) is Slider slider)) { return; }
                slider.Value = thumbSize;
                return;
            }

            UIElementCollection imgControls = imgPanel.Children;
            if (imgControls.Count < 1) { return; }
            foreach (ImgControl imgc in imgControls)
            {
                imgc.Width = thumbSize;
                imgc.Height = thumbSize;
                imgc.brdScr.Width = thumbSize / 4;
            }
        }

        /// <summary>
        /// 图片地址类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItmTypeOri_Checked(object sender, RoutedEventArgs e)
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
                        url = img.PreviewUrl;
                        break;
                    case AddressType.Small:
                        url = img.SampleUrl;
                        break;
                }
                List<string> urls = new List<string> { url };
                return urls;
            }
        }

        /// <summary>
        /// 显示或隐藏左下角搜索设置
        /// </summary>
        private void TogglePram_Click(object sender, RoutedEventArgs e)
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
        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            DelayPageTurn(1);
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            DelayPageTurn(2);
        }


        /// <summary>
        /// 下载
        /// </summary>
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(delegate { DownloadThread(); })) { IsBackground = true };
            thread.Start();
        }

        /// <summary>
        /// 处理准备的下载工作
        /// </summary>
        private void DownloadThread()
        {
            Dispatcher.BeginInvoke(new downlwork(DownloadWork));
        }

        /// <summary>
        /// 下载工作
        /// </summary>
        private void DownloadWork()
        {
            ButtonMainDL.IsEnabled = false;
            //添加下载
            List<int> selecteds = new List<int>(selected);
            if (selecteds.Count > 0)
            {
                List<MiniDownloadItem> urls = new List<MiniDownloadItem>();
                foreach (int i in selecteds)
                {
                    Img dlimg = imgs[i];
                    List<string> oriUrls = GetImgAddress(dlimg);
                    for (int c = 0; c < oriUrls.Count; c++)
                    {
                        //设图册页数
                        if (oriUrls.Count > 1)
                        {
                            imgs[i].ImgP = c + 0 + string.Empty;
                        }
                        string fileName = GenFileName(dlimg, oriUrls[c]);
                        string domain = SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                        urls.Add(new MiniDownloadItem(fileName, oriUrls[c], domain, dlimg.Author, string.Empty, string.Empty, dlimg.Id, dlimg.NoVerify));
                    }
                    ((ImgControl)imgPanel.Children[i]).SetChecked(false);
                }
                downloadC.AddDownload(urls);
            }
            ButtonMainDL.IsEnabled = true;
            //重置重试次数
            downloadC.ResetRetryCount();
            Control_Toast.Show($"选择的图片已添加到下载列表 →", Toast.MsgType.Success);
        }

        /// <summary>
        /// 构建文件名 generate file name
        /// </summary>
        /// <param name="img"></param>
        /// <param name="url">下载链接用于提取原名</param>
        /// <returns></returns>
        private string GenFileName(Img img, string url)
        {
            //Pixiv站动图
            if (img.PixivUgoira == true)
                return img.Id.ToSafeString() + "_ugoira" + img.ImgP;
            //namePatter
            string file = namePatter;
            if (string.IsNullOrWhiteSpace(file))
                return System.IO.Path.GetFileName(url);

            //%site站点 %id编号 %tag标签 %desc描述 %author作者 %date图片时间 %imgid[2]图册中图片编号[补n个零]
            file = file.Replace("%site", SiteManager.Instance.Sites[nowSelectedIndex].ShortName);
            file = file.Replace("%id", img.Id.ToSafeString());
            file = file.Replace("%tag", DownloadControl.ReplaceInvalidPathChars(img.Tags.Replace("\r\n", string.Empty)));
            file = file.Replace("%desc", DownloadControl.ReplaceInvalidPathChars(img.Desc.Replace("\r\n", string.Empty)));
            file = file.Replace("%author", DownloadControl.ReplaceInvalidPathChars(img.Author.Replace("\r\n", string.Empty)));
            file = file.Replace("%date", FormatFileDateTime(img.Date));
            #region 图册页数格式化
            try
            {
                Regex reg = new Regex(@"(?<all>%imgp\[(?<zf>[0-9]+)\])");
                MatchCollection mc = reg.Matches(file);
                Match result;
                string imgpPatter = string.Empty;
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
                        file += img.ImgP.PadLeft(5, '0');
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
            if (string.IsNullOrWhiteSpace(timeStr)) { return timeStr; }
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
                        timeStr = timeStr.Replace(smonth[i], i + 1 + string.Empty);
                    }
                }
            }
            //格式交换
            Match mca = Regex.Match(timeStr, @">(?<num>\d{4})$");
            Match mcb = Regex.Match(timeStr, @">(?<num>\d{4})>");
            string yeara = mca.Groups["num"].ToSafeString();
            string yearb = mcb.Groups["num"].ToSafeString();
            string month = string.Empty;
            if (!string.IsNullOrWhiteSpace(yeara))
            {
                mca = Regex.Match(timeStr, @"(?<num>\d+)>");
                month = mca.Groups["num"].ToSafeString();
                timeStr = yeara + new Regex(@"(\d+)>").Replace(timeStr, month, 1);
                timeStr = Regex.Replace(timeStr, yeara + @".*?>" + month, yeara + "<" + month);
            }
            else if (!string.IsNullOrWhiteSpace(yearb))
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
                timeStr = timeStr + " " + Regex.Replace(strs[1], "<", "：");
            }
            else
            {
                timeStr = Regex.Replace(timeStr, "<", "：");
            }
            return timeStr;
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
                            switch (e.Key)
                            {
                                //反选
                                case Key.I: ItmSelectInverse_Click(null, null); break;
                                //全选
                                case Key.A: ItmSelectAll_Click(null, null); break;
                                //全不选
                                case Key.Z: ItmUnSelectAll_Click(null, null); break;
                            }
                        }
                        else if (e.Key == Key.S)
                        {   //停止
                            Button_Click(null, null);
                        }

                        switch (e.Key)
                        {
                            //重试
                            case Key.R: ItmReload_Click(null, null); break;
                            //强制上一页
                            case Key.Left:
                                e.Handled = true;
                                DelayPageTurn(1, true);
                                break;
                            //强制下一页
                            case Key.Right:
                                e.Handled = true;
                                DelayPageTurn(2, true);
                                break;
                        }
                        return;
                    }

                    //滚动列表
                    switch (e.Key)
                    {
                        case Key.Up:
                        case Key.End:
                        case Key.Down:
                        case Key.Home:
                        case Key.PageUp:
                        case Key.PageDown:
                            scrList.MoveScroll(null, e);
                            e.Handled = true;
                            break;
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

        private void ItmxExplicit_Click(object sender, RoutedEventArgs e)
        {
            if (!itmxExplicit.IsChecked)
            {
                if (MessageBox.Show(this, "Explicit评分的图片含有限制级内容，请确认您已年满18周岁", ProgramName,
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
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
                ClickMaxButton = e.ClickCount < 2;
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
                Top = mousep.y - 50;
                Left = mousep.x - Width / 2;
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

        private void ContentWnd_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            ContentPresenter cp = sender as ContentPresenter;
            Rectangle sn = Template.FindName("shadowN", this) as Rectangle;
            sn.Width = cp.ActualWidth + 3;
            Rectangle ss = Template.FindName("shadowS", this) as Rectangle;
            ss.Width = cp.ActualWidth + 3;
            Rectangle se = Template.FindName("shadowE", this) as Rectangle;
            se.Height = cp.ActualHeight + 3;
            Rectangle sw = Template.FindName("shadowW", this) as Rectangle;
            sw.Height = cp.ActualHeight + 3;

            GlassHelper.EnableBlurBehindWindow(containerB, this);
        }
        /// <summary>
        /// 删除临时缓存目录
        /// </summary>
        private void DelTempDirectory()
        {
            string tmpath = System.IO.Path.GetTempPath() + "\\Moeloadelta";
            if (Directory.Exists(tmpath))
            {
                try { Directory.Delete(tmpath, true); }
                catch { }
            }
        }

        /// <summary>
        /// 主窗口关闭中
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string CloseMsg = string.Empty;

            if (downloadC.IsWorking)
            {
                CloseMsg = "还有正在下载的图片，确定要关闭程序吗？未下载完成的图片不会保存";

            }
            else if (downloadC.NumFail > 0)
            {
                CloseMsg = "还有下载失败的图片，确定要关闭程序吗？未下载完成的图片不会保存";
            }

            if (!string.IsNullOrWhiteSpace(CloseMsg)
                && MessageBox.Show(this, CloseMsg, ProgramName,
                        MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (previewFrm != null && previewFrm.IsLoaded)
            {
                previewFrm.Close();
                //previewFrm = null;
            }
            //prevent from saving invalid window size
            WindowState = WindowState.Normal;
            #region Close animation
            OpacityMask = Resources["ClosedBrush"] as LinearGradientBrush;
            Storyboard std = Resources["ClosedStoryboard"] as Storyboard;
            std.Completed += delegate { Window_Closed(sender, e); };
            std.Begin();
            #endregion

            e.Cancel = true;
            return;
        }

        /// <summary>
        /// 关闭程序
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (currentSession != null) { currentSession.IsStop = true; }

            downloadC.StopAll();

            try
            {
                if (!IsCtrlDown())
                {
                    string words = string.Empty;
                    foreach (string word in searchControl.UsedItems)
                    {
                        words += word + "|";
                    }

                    const string qm = ";";
                    string text = downloadC.NumOnce + "\r\n"
                        + (DownloadControl.SaveLocation == ProgramRunPath
                        ? "." : DownloadControl.SaveLocation) + "\r\n" + addressType + qm
                        + (downloadC.IsSaSave ? (downloadC.IsSscSave ? "2" : "1") : (downloadC.IsSscSave ? "3" : "0")) + qm
                        + numOfLoading + qm
                        + (itmMaskViewed.IsChecked ? "1" : "0") + qm
                        + words + qm
                        + Proxy + qm
                        + BossKey + qm
                        + thumbSize + qm
                        + ProxyType + qm
                        + (int)ActualWidth + "," + (int)ActualHeight + qm
                        + (togglePram.IsChecked.Value ? "1" : "0") + qm
                        + PreFetcher.CachedImgCount + qm
                        + (downloadC.IsSepSave ? "1" : "0") + qm
                        + (itmxExplicit.IsChecked ? "1" : "0") + qm
                        + namePatter + qm
                        + num + qm
                        + bgSt + qm
                        + bgHe + qm
                        + bgVe + qm
                        + bgOp + "\r\n";
                    foreach (KeyValuePair<string, ViewedID> id in viewedIds)
                    {
                        text += id.Key + ":" + id.Value + "\r\n";
                    }
                    File.WriteAllText($"{ProgramRunPath}\\Moe_config.ini", text);
                    DeleteTheSpecifiedFile(DownloadControl.SaveLocation, null, ".moe");
                    SaveFavorite();
                }
            }
            catch { }

            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();
            Environment.Exit(0);
        }

        /// <summary>
        /// 删除指定文件 含子目录 目录文件小于1000个
        /// </summary>
        private void DeleteTheSpecifiedFile(string dirPath, string fileName = null, string fileExt = null)
        {
            if (string.IsNullOrWhiteSpace(dirPath)
                || string.IsNullOrWhiteSpace(fileName) && string.IsNullOrWhiteSpace(fileExt))
            { return; }

            DirectoryInfo d = new DirectoryInfo(dirPath);
            FileSystemInfo[] fsInfos = d.GetFileSystemInfos();
            if (fsInfos.Length > 1000) { return; }

            foreach (FileSystemInfo fsInfo in fsInfos)
            {
                //判断是否为文件夹　　
                if (fsInfo is DirectoryInfo)
                {
                    DeleteTheSpecifiedFile(fsInfo.FullName, fileExt);
                    try { Directory.Delete(fsInfo.FullName, false); } catch { }
                }
                else if ((!string.IsNullOrWhiteSpace(fileName) && fsInfo.FullName == fileName)
                    || (!string.IsNullOrWhiteSpace(fileExt) && System.IO.Path.GetExtension(fsInfo.FullName) == fileExt))
                {
                    fsInfo.Delete();
                }
            }
        }
        /// <summary>
        /// 拖放文件到窗口
        /// </summary>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop).GetType().ToSafeString() != "System.String[]") { return; }
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string ext = string.Empty, file = files.FirstOrDefault(f =>
                  {
                      ext = System.IO.Path.GetExtension(f);
                      switch (ext)
                      {
                          case ".jpg":
                          case ".png":
                          case ".gif":
                              return true;
                      }
                      return false;
                  });

                if (string.IsNullOrWhiteSpace(file)) { return; }
                try
                {
                    File.Delete($"{ ProgramRunPath}\\bg.jpg");
                    File.Delete($"{ ProgramRunPath}\\bg.png");
                    File.Delete($"{ ProgramRunPath}\\bg.gif");
                }
                catch { }
                File.Copy(file, $"{ProgramRunPath}\\bg{ext}", true);
                ChangeBg(bgOp, true);
            }
        }

        #region ====== 缩略图列表拖选 ======
        /// <summary>
        /// 缩略图列表鼠标左键按下
        /// </summary>
        private void ImgList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //记录拖选点
            if (imgPanel.Children.Count > 0)
            {
                imgListPanel.CaptureMouse();
                canDrag = e.Handled = true;
                dragIsCtrl = IsCtrlDown();
                dragStartPoint = e.GetPosition(imgListPanel);
            }
        }

        /// <summary>
        /// 缩略图列表鼠标左键放开
        /// </summary>
        private void ImgList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //选中拖选框和缩略图交叉区域中的缩略图并删除拖选框
            if (dragSelectBorder != null && dragSelectBorder.Width > 10 && dragSelectBorder.Height > 10 && imgPanel.Children.Count > 0)
            {
                e.Handled = true;
                //循环选择必须在异步过程中、否则不能选择最后一个
                UIElementCollection imgcs = imgPanel.Children;
                Point dragEndPoint = e.GetPosition(imgListPanel);
                Rect dragRect = new Rect(dragStartPoint, dragEndPoint);

                foreach (ImgControl imgc in imgcs)
                {
                    Point imgcPoint = imgc.TranslatePoint(new Point(), imgListPanel);
                    Rect imgcRect = new Rect(imgcPoint.X, imgcPoint.Y, imgc.Width, imgc.Height);

                    async void DelayCalculation()
                    {
                        await Task.Delay(1);
                        if (dragRect.IntersectsWith(imgcRect))
                        {
                            imgc.SetChecked(dragIsCtrl ? !imgc.IsChecked : true);
                        }
                    }
                    DelayCalculation();
                }
            }
            CancelDrawBox();
            imgListPanel.ReleaseMouseCapture();
        }

        /// <summary>
        /// 缩略图列表鼠标移动
        /// </summary>
        private void ImgList_MouseMove(object sender, MouseEventArgs e)
        {
            if (canDrag && imgPanel.Children.Count > 0)
            {
                Point dragEndPoint = e.GetPosition(imgListPanel);
                double dragWidth = Math.Abs(dragEndPoint.X - dragStartPoint.X),
                    dragHeight = Math.Abs(dragEndPoint.Y - dragStartPoint.Y);
                if (dragWidth < -10 || dragWidth > 10 || dragHeight < -10 || dragHeight > 10)
                {
                    //如果没按住Ctrl则取消全部缩略图选中
                    if (!dragIsCtrl && !dragIsClear) { ClearDrawSelected(); }
                    //画拖选框
                    DrawMultiselectBox(dragStartPoint, dragEndPoint);
                }
            }
        }

        /// <summary>
        /// 画拖选框
        /// </summary>
        /// <param name="dragStartPoint">起点</param>
        /// <param name="dragEndPoint">终点</param>
        private void DrawMultiselectBox(Point dragStartPoint, Point dragEndPoint)
        {
            if (dragSelectBorder == null)
            {
                dragSelectBorder = new Border
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = new SolidColorBrush(Color.FromArgb(68, 52, 163, 224)),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(52, 163, 224))
                };
                dragSelectBorder.MouseLeftButtonUp += new MouseButtonEventHandler(ImgList_MouseLeftButtonUp);
                dragSelectBorder.MouseMove += new MouseEventHandler(ImgList_MouseMove);
                dragSelectBorder.PreviewMouseDown += new MouseButtonEventHandler(ImgList_PreviewMouseDown);
                Panel.SetZIndex(dragSelectBorder, 200);
                imgListPanel.Children.Add(dragSelectBorder);
            }
            dragSelectBorder.Width = Math.Abs(dragEndPoint.X - dragStartPoint.X);
            dragSelectBorder.Height = Math.Abs(dragEndPoint.Y - dragStartPoint.Y);

            double dragLeft = 0, dragTop = 0;
            dragLeft = dragEndPoint.X - dragStartPoint.X >= 0 ? dragStartPoint.X : dragEndPoint.X;
            dragTop = dragEndPoint.Y - dragStartPoint.Y >= 0 ? dragStartPoint.Y : dragEndPoint.Y;
            dragSelectBorder.Margin = new Thickness(dragLeft, dragTop, dragSelectBorder.Margin.Right, dragSelectBorder.Margin.Bottom);
        }

        /// <summary>
        /// 缩略图列表按下任意键
        /// </summary>
        private void ImgList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //不是左键取消拖选框
            if (canDrag && e.ChangedButton != MouseButton.Left) { CancelDrawBox(); }
        }

        /// <summary>
        /// 清空多选
        /// </summary>
        private void ClearDrawSelected()
        {
            dragIsClear = true;
            UIElementCollection imgcs = imgPanel.Children;
            foreach (ImgControl imgc in imgcs)
            {
                if (imgc.IsChecked)
                {
                    imgc.SetChecked(false);
                }
            }
        }

        /// <summary>
        /// 取消拖选
        /// </summary>
        private void CancelDrawBox()
        {
            if (dragSelectBorder != null)
            {
                imgListPanel.Children.Remove(dragSelectBorder);
                dragSelectBorder = null;
            }
            canDrag = dragIsClear = false;
        }
        #endregion ============

        #region 线程延迟执行翻页
        /// <summary>
        /// 线程延迟执行翻页
        /// </summary>
        /// <param name="operating">1上一页 2下一页</param>
        /// <param name="force">强制翻页</param>
        private void DelayPageTurn(int operating, bool force)
        {
            Thread newThread = null;
            if (operating == 1)
            {
                if (realPage > 1 || force)
                {
                    newThread = new Thread(new ThreadStart(RDelayP))
                    {
                        Name = "RDelayP"
                    };
                }
            }
            else if (operating == 2)
            {
                if (HaveNextPage || force)
                {
                    newThread = new Thread(new ThreadStart(RDelayN))
                    {
                        Name = "RDelayN"
                    };
                }
            }

            if (newThread != null)
            {
                UpdatePreNextDisable();
                newThread.Start();
            }
        }

        /// <summary>
        /// 线程延迟执行翻页
        /// </summary>
        /// <param name="operating">1上一页 2下一页</param>
        private void DelayPageTurn(int operating)
        {
            DelayPageTurn(operating, false);
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
                Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
            Thread.Sleep(666);
            if (!isGetting)
            {
                realPage++;
                Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
        }
        #endregion

    }


}
