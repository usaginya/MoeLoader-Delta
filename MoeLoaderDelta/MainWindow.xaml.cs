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
    /// Interaction logic for xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 下載工作委託
        /// </summary>
        private delegate void downlwork();

        /// <summary>
        /// 主視窗的句柄
        /// </summary>
        public static IntPtr Hwnd;

        /// <summary>
        /// 程式名
        /// </summary>
        private static string programName;
        /// <summary>
        /// 程式版本
        /// </summary>
        public static Version ProgramVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// 封裝的程式名
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


        private const string IMGLOADING = "圖片載入中...";

        private int num = 50, realNum = 50;
        private int page = 1, realPage = 1, lastPage = 1;

        //private Color backColor;
        //internal bool isAero = true;
        /// <summary>
        /// 使用最大化按鈕最大化
        /// </summary>
        private bool ClickMaxButton = false;

        /// <summary>
        /// 是否還有下一頁
        /// </summary>
        private bool HaveNextPage = false;

        /// <summary>
        /// 這個或許是同時載入縮圖的數量
        /// </summary>
        private int numOfLoading = 5;

        private Storyboard logo;

        /// <summary>
        /// 已經瀏覽過的位置
        /// </summary>
        private Dictionary<string, ViewedID> viewedIds;
        private int nowSelectedIndex = 0;

        internal List<Img> imgs;
        private List<int> selected = new List<int>();

        internal PreviewWnd previewFrm;
        private SessionState currentSession;
        private bool isGetting = false;

        /// <summary>
        /// 使用的地址類型
        /// </summary>
        private AddressType addressType = AddressType.Ori;

        //已載入完畢的圖像索引
        private List<int> loaded = new List<int>();
        //未載入完畢的
        private LinkedList<int> unloaded = new LinkedList<int>();

        internal bool showExplicit = true;
        private bool naviMoved = false;
        private bool funcBtnShown = false;

        //Microsoft.Windows.Shell.WindowChrome chrome;

        public static MainWindow MainW;

        internal static int comboBoxIndex = 0;
        internal const string DefaultPatter = "[%site_%id_%author]%desc<!<_%imgp[5]";
        internal string namePatter = DefaultPatter;

        internal double bgOp = 0.5;
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
                //Warning!! The order can not be wrong!! 順序不能錯!!
                UnregisterHotKey(Hwnd, (int)bossKey);
                bossKey = value;
                RegisterHotKey(Hwnd, (int)bossKey, 0, bossKey);
            }
            get { return bossKey; }
        }

        /// <summary>
        /// 代理設定，eg. 127.0.0.1:1080
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

            btnGet.ToolTip = btnGet.Tag as string;

            if (!File.Exists(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\nofont.txt"))
            {
                FontFamily = new FontFamily("Microsoft JhengHei");
            }

            //SessionClient.ReadCookiesFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\SaveCk.mck");

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
                siteText.Text = "當前站點 " + SiteManager.Instance.Sites[comboBoxIndex].ShortName;
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
            string bgPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bg.png";
            bool hasBg = false;
            if (File.Exists(bgPath))
            {
                hasBg = true;
            }
            else
            {
                bgPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bg.jpg";
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
        /// 載入設定
        /// </summary>
        private void LoadConfig()
        {
            string configFile = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Moe_config.ini";

            //讀取設定檔案
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
                            downloadC.IsSaSave = parts[1].Equals("1");
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
                                togglePram.ToolTip = "顯示搜尋設定";
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
                                //向前相容
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
                    MessageBox.Show(this, "讀取設定檔案失敗\r\n" + ex.Message, ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
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
        /// 啟用翻頁按鈕
        /// </summary>
        /// <param name="btnid">0上一頁, 1下一頁</param>
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
        /// 啟用上一頁按鈕
        /// </summary>
        private void UpdatePreNextEnable()
        {
            UpdatePreNextEnable(0);
        }

        /// <summary>
        /// 禁用翻頁按鈕
        /// </summary>
        private void UpdatePreNextDisable()
        {
            btnPrev.IsEnabled = btnNext.IsEnabled = false;
            btnPrev.Visibility = btnNext.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 翻頁按鈕動畫
        /// </summary>
        /// <param name="btnid">0 上一頁, 1下一頁</param>
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
        /// 上一頁按鈕動畫
        /// </summary>
        private void PlayPreNextAnimation()
        {
            PlayPreNextAnimation(0);
        }

        /// <summary>
        /// 圖片訊息已獲取
        /// </summary>
        /// <param name="sender"></param>
        public void LoadComplete(object sender)
        {
            if (sender == null)
            {
                currentSession.IsStop = true;
                statusText.Text = "載入完畢，取得 0 張圖片";

                txtGet.Text = "搜尋";
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
                //重新讀取RToolStripMenuItem.Enabled = false;

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
                //重新讀取RToolStripMenuItem.Enabled = true;

                if (imgs.Count == 0)
                {
                    DocumentCompleted();
                    return;
                }

                //生成縮圖控制項
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
                        //初始載入
                        img.DownloadImg();
                    }
                    else unloaded.AddLast(i);
                    //}
                }
                scrList.ScrollToTop();
            }
        }

        /// <summary>
        /// 將某個圖片加入下載隊列
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
                //設圖冊頁數
                if (oriUrls.Count > 1)
                {
                    imgs[index].ImgP = c + 1 + "";
                }
                string fileName = GenFileName(dlimg);
                string domain = SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                downloadC.AddDownload(new MiniDownloadItem[] {
                    new MiniDownloadItem(fileName, oriUrls[c], domain, dlimg.Author, "", "", dlimg.Id, dlimg.NoVerify)
                });
            }
            //string url = GetImgAddress(imgs[index]);
            //string fileName = GenFileName(imgs[index]);
            //downloadC.AddDownload(new MiniDownloadItem[] { new MiniDownloadItem(fileName, url) });

            //System.Media.SystemSounds.Exclamation.Play();
        }

        /// <summary>
        /// 縮圖被選中
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
                //批次選擇
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
        /// 右下角按鈕顯示處理
        /// </summary>
        /// <param name="hide"></param>
        private void ShowOrHideFuncBtn(bool hide)
        {
            selText.Text = "選中圖片 " + selected.Count;

            Storyboard sb = new Storyboard();

            //顯示or隱藏按鈕
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
        /// 某個縮圖載入完畢
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

            //載入完第一張圖時
            if (loaded.Count < 2)
            {
                //預載入
                StartPreLoad();
                //顯示上一頁按鈕
                UpdatePreNextEnable();
                //重設縮圖大小
                itmSmallPre_Click(null, null);
            }

            //只要有下一頁就顯示翻頁按鈕
            if (HaveNextPage)
                UpdatePreNextEnable(1);
        }

        /// <summary>
        /// 預載入縮圖列表結束時
        /// </summary>
        /// <param name="sender">這有結果數量</param>
        private void Fetcher_PreListLoaded(object sender, EventArgs e)
        {
            //按照結果更新翻頁按鈕狀態
            Dispatcher.Invoke(
                new Action(
                    delegate
                    {
                        //防止多次設定按鈕狀態
                        bool tmphave = HaveNextPage;
                        HaveNextPage = (int)sender > 0;
                        //如果搜尋結束時才有翻頁就顯示翻頁按鈕
                        if (HaveNextPage && !tmphave && IsLoaded && !isGetting)
                            UpdatePreNextEnable(1);
                    }));
        }

        /// <summary>
        /// 搜尋
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SiteManager.Instance.Sites.Count < 1)
                return;

            //獲取
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
                btnGet.ToolTip = "停止搜尋";
                imgGet.Source = new BitmapImage(new Uri("/Images/stop.png", UriKind.Relative));

                UpdatePreNextDisable();

                if (sender != null)
                {
                    //記錄當前頁面
                    lastPage = realPage;

                    //由點擊搜尋按鈕觸發，所以使用介面上的設定
                    realNum = num;
                    realPage = IsShiftDown() ? lastPage : page;
                    nowSelectedIndex = comboBoxIndex;
                    siteText.Text = "當前站點 " + SiteManager.Instance.Sites[nowSelectedIndex].SiteName;
                }

                pageText.Text = "當前頁碼 " + realPage;

                bgLoading.Visibility = Visibility.Visible;
                logo.Begin();

                //nowSelectedIndex = comboBoxIndex;

                statusText.Text = "與伺服器通迅，請稍候...";

                if (searchControl.Text.Length != 0)
                {
                    //一次最近搜尋詞
                    searchControl.AddUsedItem(searchControl.Text);
                }

                showExplicit = !itmxExplicit.IsChecked;
                string word = searchControl.Text;
                //string url = PrepareUrl(realPage);
                //nowSession = new ImgSrcProcessor(MaskInt, MaskRes, url, SrcType, LastViewed, MaskViewed);
                //nowSession.processComplete += new EventHandler(ProcessHTML_processComplete);
                //(new System.Threading.Thread(new System.Threading.ThreadStart(nowSession.ProcessSingleLink))).Start();
                currentSession = new SessionState();


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

                        //過濾圖片列表
                        imgList = SiteManager.Instance.Sites[nowSelectedIndex].FilterImg(
                            imgList, MaskInt, MaskRes, LastViewed, MaskViewed, showExplicit, true);

                    }
                    catch (Exception ex)
                    {
                        if (!(o as SessionState).IsStop)
                        {
                            Dispatcher.Invoke(new VoidDel(() =>
                            {
                                MessageBox.Show(this, "獲取圖片遇到錯誤: " + ex.Message,
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
                currentSession.IsStop = true;
                statusText.Text = "載入完畢，取得 0 張圖片";
                siteText.Text = "當前站點 " + SiteManager.Instance.Sites[nowSelectedIndex].ShortName;

                //嘗試載入下一頁
                if (!HaveNextPage)
                    StartPreLoad();

                //顯示上一頁按鈕
                UpdatePreNextEnable();

                isGetting = false;
                txtGet.Text = "搜尋";
                btnGet.ToolTip = btnGet.Tag as string;
                imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

                logo.Stop();
                bgLoading.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 執行預載入
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
        /// 所有縮圖載入完畢
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
                statusText.Text = "載入完畢，取得 " + imgs.Count + " 張圖片";
            else
                statusText.Text = "載入完畢，取得 " + imgs.Count + " 張圖片 (剩餘約 " + viewedC + " 張未瀏覽)";

            //statusText.Text = "搜尋完成！取得 " + imgs.Count + " 張圖片訊息 (上次瀏覽至 " + viewedIds[nowSelectedIndex].ViewedBiggestId + " )";
            txtGet.Text = "搜尋";
            btnGet.ToolTip = btnGet.Tag as string;
            isGetting = false;
            imgGet.Source = new BitmapImage(new Uri("/Images/search.png", UriKind.Relative));

            //System.Media.SystemSounds.Beep.Play();
            if (GlassHelper.GetForegroundWindow() != Hwnd)
                GlassHelper.FlashWindow(Hwnd, true);

            //無圖時禁用選單
            if (imgs.Count < 1)
            {
                itmSelectInverse.IsEnabled =
                    itmSelectAll.IsEnabled =
                    itmUnSelectAll.IsEnabled =
                    itmReload.IsEnabled = false;
            }

        }

        /// <summary>
        /// 顯示下載列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggleDownload_Click(object sender, RoutedEventArgs e)
        {
            Storyboard sb;

            if (toggleDownload.IsChecked.Value)
            {
                toggleDownload.ToolTip = "隱藏下載面板";
                sb = (Storyboard)FindResource("showDownload");

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
                toggleDownload.ToolTip = "顯示下載面板(按住Ctrl隱藏縮圖)";
                sb = (Storyboard)FindResource("closeDownload");
                sb.Begin();
            }
            sb.Completed += toggleDownloadAni_Completed;
        }

        /// <summary>
        /// 下載列表動畫結束時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggleDownloadAni_Completed(object sender, EventArgs e)
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
        /// 視窗資源初始化
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(Hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        /// <summary>
        /// 按鍵監聽事件
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                // 老闆鍵
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
        /// 限制頁碼設定只能輸入數字的一種方法
        /// </summary>
        private void txtPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 || e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key == Key.Back
                || e.Key == Key.Delete || e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.LeftShift || e.Key == Key.Left
                || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down))
            {
                e.Handled = true;
            }
        }

        private void txtNum_TextChanged(object sender, TextChangedEventArgs e)
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

        private void txtPage_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            try
            {
                num = int.Parse(txtNum.Text);
                page = int.Parse(txtPage.Text);

                num = num > 0 ? num : 1;
                page = page > 0 ? page : 1;

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
            if (num < 500)
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
        /// 預覽圖片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void img_Click(object sender, EventArgs e)
        {
            int index = (int)sender;

            //排除不支援預覽的格式
            string supportformat = "jpg jpeg png bmp gif";
            string ext = BooruProcessor.FormattedImgUrl("", imgs[index].SampleUrl.Substring(imgs[index].SampleUrl.LastIndexOf('.') + 1));
            if (!supportformat.Contains(ext))
            {
                MessageBox.Show(this, "未支援" + ext + "格式的預覽顯示，請下載後使用其它程式方式打開檔案預覽",
                    ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (previewFrm == null || !previewFrm.IsLoaded)
            {
                previewFrm = new PreviewWnd(this);
                previewFrm.Show();
                Focus();
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
        /// 反選
        /// </summary>
        private void itmSelectInverse_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ImgControl imgc = (ImgControl)imgPanel.Children[i];

                imgc.SetChecked(!selected.Contains(i));
            }
            ShowOrHideFuncBtn(selected.Count < 1);
        }

        /// <summary>
        /// 全選
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
        /// 全不選
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
        /// 重試
        /// </summary>
        private void itmReload_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                ((ImgControl)imgPanel.Children[i]).RetryLoad();
            }
            StartPreLoad();
        }

        /// <summary>
        /// 封鎖圖片rate 選單勾選狀態
        /// </summary>
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
        /// 封鎖圖片res 選單勾選狀態
        /// </summary>
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
        /// 打開站點首頁
        /// </summary>
        private void itmOpenSite_Click(object sender, RoutedEventArgs e)
        {
            if (SiteManager.Instance.Sites.Count > 0)
                System.Diagnostics.Process.Start(SiteManager.Instance.Sites[comboBoxIndex].SiteUrl);
        }

        /// <summary>
        /// 生成選中圖片的下載列表Lst檔案
        /// </summary>
        private void itmLst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selected.Count == 0)
                {
                    MessageBox.Show(this, "未選擇圖片", ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
                {
                    DefaultExt = "lst",
                    FileName = "MoeLoaderList.lst",
                    Filter = "lst檔案|*.lst",
                    OverwritePrompt = false
                };
                if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string text = "";
                    int success = 0, repeat = 0;
                    //讀存在的lst內容
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

                        //尋找重複項
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
                                //設圖冊頁數
                                if (oriUrls.Count > 1)
                                {
                                    selectimg.ImgP = c + 1 + "";
                                }

                                //url|檔案名|域名|上傳者|ID(用於判斷重複)
                                text += oriUrls[c]
                                    + "|" + GenFileName(selectimg)
                                    + "|" + host
                                    + "|" + selectimg.Author
                                    + "|" + selectimg.Id
                                    + "|" + (selectimg.NoVerify ? 'v' : 'x')
                                    + "\r\n";
                                success++;
                            }
                        }
                        else
                            repeat++;
                    }
                    File.AppendAllText(saveFileDialog1.FileName, text);
                    MessageBox.Show("成功儲存 " + success + " 個地址\r\n" + repeat + " 個地址已在列表中\r\n", ProgramName,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(this, "儲存失敗", ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (downloadC.IsWorking)
            {
                if (
                    MessageBox.Show(this, "正在下載圖片，確定要關閉程式嗎？未下載完成的圖片不會儲存",
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
        /// 使用小縮圖
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
                        //如果比預設大小還小就用預設大小
                        img.Width = smallx < 170 ? img.Width > 170 ? 170 : 170 : smallx;
                        img.Height = smally < 190 ? img.Height > 190 ? 190 : 190 : smally;
                    }
                    //自適應評分數字區
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
        /// 圖片地址類型
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
        /// 使用的地址類型
        /// </summary>
        enum AddressType { Ori, Jpg, Pre, Small }

        /// <summary>
        /// 獲取Img的地址
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
        /// 顯示或隱藏左下角搜尋設定
        /// </summary>
        private void togglePram_Click(object sender, RoutedEventArgs e)
        {
            if (togglePram.IsChecked.Value)
            {
                togglePram.ToolTip = "顯示搜尋設定";
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
        /// 上一頁
        /// </summary>
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            DelayPageTurn(1);
        }

        /// <summary>
        /// 下一頁
        /// </summary>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            DelayPageTurn(2);
        }


        /// <summary>
        /// 下載
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void bDownload_MouseUp(object sender, MouseButtonEventArgs e)
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (!toggleDownload.IsChecked.Value)
                toggleDownload.IsChecked = true;
            toggleDownload_Click(null, null);

            Thread thread = new Thread(new ThreadStart(delegate {
                DownloadThread();
            }));
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 處理準備的下載工作
        /// </summary>
        private void DownloadThread()
        {
            Dispatcher.BeginInvoke(new downlwork(DownloadWork));
        }

        /// <summary>
        /// 下載工作
        /// </summary>
        private void DownloadWork()
        {
            ButtonMainDL.IsEnabled = false;
            //添加下載
            if (selected.Count > 0)
            {
                List<MiniDownloadItem> urls = new List<MiniDownloadItem>();
                foreach (int i in selected)
                {
                    Img dlimg = imgs[i];
                    List<string> oriUrls = GetImgAddress(dlimg);
                    for (int c = 0; c < oriUrls.Count; c++)
                    {
                        //設圖冊頁數
                        if (oriUrls.Count > 1)
                        {
                            imgs[i].ImgP = c + 1 + "";
                        }
                        string fileName = GenFileName(dlimg);
                        string domain = SiteManager.Instance.Sites[nowSelectedIndex].ShortName;
                        urls.Add(new MiniDownloadItem(fileName, oriUrls[c], domain, dlimg.Author, "", "", dlimg.Id, dlimg.NoVerify));
                    }
                }
                downloadC.AddDownload(urls);
            }
            ButtonMainDL.IsEnabled = true;
        }

        /// <summary>
        /// 構建檔案名 generate file name
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private string GenFileName(Img img)
        {
            //namePatter
            string file = namePatter;

            //%site站點 %id編號 %tag標籤 %desc描述 %author作者 %date圖片時間 %imgid[2]圖冊中圖片編號[補n個零]
            file = file.Replace("%site", SiteManager.Instance.Sites[nowSelectedIndex].ShortName);
            file = file.Replace("%id", img.Id.ToSafeString());
            file = file.Replace("%tag", img.Tags.Replace("\r\n", ""));
            file = file.Replace("%desc", img.Desc.Replace("\r\n", ""));
            file = file.Replace("%author", img.Author);
            file = file.Replace("%date", FormatFileDateTime(img.Date));
            #region 圖冊頁數格式化
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
                    //如果圖冊有數量就強制加序號
                    if (int.Parse(img.ImgP) > 0)
                        file += img.ImgP.PadLeft(5, '0');
                }
            }
            catch { }
            #endregion

            return file;
        }

        /// <summary>
        /// 格式化雜亂字串為適用於檔案名的時間格式
        /// </summary>
        /// <param name="timeStr">時間字串</param>
        /// <returns></returns>
        private string FormatFileDateTime(string timeStr)
        {
            if (timeStr.Trim() == "") return timeStr;
            //空格切分日期時間
            timeStr = Regex.Replace(timeStr, @"\s", ">");
            //取代英文月份
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
            //格式交換
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
            //雜字過濾
            timeStr = Regex.Replace(timeStr, @"[^\d|>]", "<");
            //取時間區域
            timeStr = Regex.Match(timeStr, @"\d[\d|<|>]+[<|>]+\d+").ToString();
            //縮減重複字元
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
        /// 視窗按鍵事件處理
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //列表有圖片時
            if (imgs != null)
            {
                //列錶快捷鍵
                //排除其它控制項焦點快捷鍵
                if (!txtPage.IsFocused && !txtNum.IsFocused && !searchControl.Textbox.IsFocused && !downloadC.Scrollviewer.IsFocused)
                {
                    if (IsCtrlDown())
                    {
                        if (!isGetting)
                        {
                            if (e.Key == Key.I)
                            {   //反選
                                itmSelectInverse_Click(null, null);
                            }
                            else if (e.Key == Key.A)
                            {   //全選
                                itmSelectAll_Click(null, null);
                            }
                            else if (e.Key == Key.Z)
                            {   //全不選
                                itmUnSelectAll_Click(null, null);
                            }
                        }
                        else if (e.Key == Key.S)
                        {   //停止
                            Button_Click(null, null);
                        }

                        if (e.Key == Key.R)
                        {//重試
                            itmReload_Click(null, null);
                        }
                        else if (e.Key == Key.Right)
                        {//強制下一頁
                            e.Handled = true;
                            DelayPageTurn(2, true);
                        }
                        return;
                    }

                    //滾動列表
                    if (e.Key == Key.Down && scrList.ExtentHeight > 0)
                    {
                        //避免焦點跑到其它地方
                        e.Handled = true;
                        //向下滾動列表
                        scrList.ScrollToVerticalOffset(scrList.VerticalOffset + scrList.ViewportHeight * 0.5);
                    }
                    else if (e.Key == Key.Up && scrList.ExtentHeight > 0)
                    {
                        e.Handled = true;
                        //向上滾動列表
                        scrList.ScrollToVerticalOffset(scrList.VerticalOffset - scrList.ViewportHeight * 0.5);
                    }
                    else if (e.Key == Key.Home)
                    {
                        e.Handled = true;
                        //滾動列表到頂部
                        scrList.ScrollToTop();
                    }
                    else if (e.Key == Key.End)
                    {
                        e.Handled = true;
                        //滾動列表到底部
                        scrList.ScrollToBottom();
                    }

                    //左右鍵翻頁
                    if (e.Key == Key.Left)
                    {
                        e.Handled = true;
                        //上一頁
                        DelayPageTurn(1);
                    }
                    else if (e.Key == Key.Right)
                    {
                        e.Handled = true;
                        //下一頁
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
                if (MessageBox.Show(this, "Explicit評分的圖片含有限制級內容，請確認您已年滿18週歲", ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
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
                        //啟動回收
                        GC.Collect();
                        //刪除臨時目錄
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
        /// 最大化時拖動還原視窗
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
        /// 關閉程式
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

            //SessionClient.WriteCookiesToFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\SaveCk.mck");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Environment.Exit(0);
        }

        #region 執行緒延遲執行翻頁
        /// <summary>
        /// 執行緒延遲執行翻頁
        /// </summary>
        /// <param name="operating">1上一頁 2下一頁</param>
        /// <param name="force">強制翻頁</param>
        private void DelayPageTurn(int operating, bool force)
        {
            Thread newThread = null;
            if (operating == 1 && realPage > 1)
            {
                newThread = new Thread(new ThreadStart(RDelayP));
                newThread.Name = "RDelayP";
            }
            else if (operating == 2)
            {
                if (HaveNextPage || force)
                {
                    newThread = new Thread(new ThreadStart(RDelayN));
                    newThread.Name = "RDelayN";
                }
            }

            if (newThread != null)
            {
                UpdatePreNextDisable();
                newThread.Start();
            }
        }
        /// <summary>
        /// 執行緒延遲執行翻頁
        /// </summary>
        /// <param name="operating">1上一頁 2下一頁</param>
        private void DelayPageTurn(int operating)
        {
            DelayPageTurn(operating, false);
        }

        private void RDelayP()
        {
            // 如果正在搜尋就先停止
            if (isGetting)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
            Thread.Sleep(233);
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
            // 如果正在搜尋就先停止
            if (isGetting)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    Button_Click(null, null);
                }));
            }
            Thread.Sleep(233);
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
