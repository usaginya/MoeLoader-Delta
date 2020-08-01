using MoeLoaderDelta.Control;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MoeLoaderDelta.Windows
{
    /// <summary>
    /// FavoriteAddWnd.xaml 的交互逻辑
    /// Last: 2020-7-29
    /// </summary>
    public partial class FavoriteAddWnd : Window
    {
        /// <summary>
        /// 添加或编辑模式
        /// </summary>
        public enum AddMode { Add, EditDir, EditKeyword }

        /// <summary>
        /// 提供外部操作用
        /// </summary>
        public static FavoriteAddWnd ThisWnd { get; set; }

        /// <summary>
        /// 窗口句柄
        /// </summary>
        private static IntPtr Hwnd;

        /// <summary>
        /// 是否关闭窗口
        /// </summary>
        private bool isClose;

        /// <summary>
        /// 输入窗口定义
        /// </summary>
        private SingleTextInputWnd inputWnd;

        /// <summary>
        /// 添加模式 内部
        /// </summary>
        private AddMode addMode;

        /// <summary>
        /// 原名,备注,所在目录,所在站点
        /// </summary>
        private string editName, editMark, editDir, editSite;

        /// <summary>
        /// ComboBox目录源
        /// </summary>
        private ObservableCollection<string> DirItems = new ObservableCollection<string>();

        //+++++++++++++++  方法开始 +++++++++++++++++++++++++
        /// <summary>
        /// 构造窗口
        /// </summary>
        /// <param name="name">原始名称</param>
        /// <param name="mark">备注名称</param>
        /// <param name="dir">所在目录</param>
        /// <param name="owner">父窗口</param>
        public FavoriteAddWnd(string name, AddMode mode = AddMode.Add, string mark = null, string dir = null, string siteName = null, Window owner = null)
        {
            InitializeComponent();
            Owner = owner;
            ThisWnd = this;
            addMode = mode;
            editName = name;
            editMark = mark;
            editDir = dir;
            editSite = siteName;
        }

        /// <summary>
        /// 窗口初始化
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Hwnd = new WindowInteropHelper(this).Handle;
            Window_InitializeStyle();
        }

        /// <summary>
        /// 窗口初始化样式
        /// </summary>
        private void Window_InitializeStyle()
        {
            LinearGradientBrush brush = new LinearGradientBrush
            {
                StartPoint = new Point(1, 1),
                EndPoint = new Point(0, 0)
            };
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 0, 0, 0), 1));
            OpacityMask = brush;
        }

        /// <summary>
        /// 窗口载入完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string wTitle = "收藏新标签";
            switch (addMode)
            {
                case AddMode.EditDir:
                    wTitle = "编辑收藏目录名";
                    WindowIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Images/adddir.png"));
                    TextBlockMarkLabel.Text = (string)TextBlockMarkLabel.Tag;
                    GridSaveFavDir.Visibility = Visibility.Collapsed;
                    break;

                case AddMode.EditKeyword:
                    wTitle = "编辑收藏标签";
                    ComboxItemsInit(editSite);
                    break;

                default:
                    ComboxItemsInit(editSite);
                    break;
            }
            if (!editName.IsNullOrEmptyOrWhiteSpace()) { TextBoxOriginal.Text = editName; }
            if (!editMark.IsNullOrEmptyOrWhiteSpace()) { TextBoxMark.Text = editName; TextBoxOriginal.Text = editMark; }
            if (!editDir.IsNullOrEmptyOrWhiteSpace())
            {
                ComboBoxFavDir.SelectedItem = editDir;
                //记录原始目录、用于移动
                ComboBoxFavDir.Tag = editDir;
            }
            WindowTitle.Text = Title = wTitle;
            TextBoxMark.Focus();
        }

        /// <summary>
        /// 选择框列表初始化
        /// </summary>
        /// <param name="siteName">指定站点列表范围</param>
        private void ComboxItemsInit(string siteName)
        {
            if (siteName.IsNullOrEmptyOrWhiteSpace()) { return; }
            ComboBoxFavDir.ItemsSource = DirItems;

            DirItems.Add(siteName);
            List<string> dirNameList = TreeViewModel.GetSiteDirNameList(siteName);
            DirItems.AddRange(dirNameList);

            if (ComboBoxFavDir.Items.Count > 0) { ComboBoxFavDir.SelectedIndex = 0; }
        }

        #region ################ 界面事件方法 ################
        /// <summary>
        /// 按钮 添加收藏目录
        /// </summary>
        private void BtnAddFavDir_Click(object sender, RoutedEventArgs e)
        {
            if (editSite == null)
            {
                MainWindow.MainW.Control_Toast.Show("当前没有站点可以新建收藏目录", Toast.MsgType.Warning);
                return;
            }
            if (inputWnd != null && inputWnd.IsLoaded)
            {
                inputWnd.Activate();
            }
            else
            {
                //创建并绑定输入窗口
                inputWnd = new SingleTextInputWnd(this, "新建收藏目录", "Images/adddir.png", "请输入新建的收藏目录名称");
                inputWnd.InputResultEvent += new SingleTextInputWnd.InputValueHandler(AddFavDirToCombBox);
                inputWnd.ShowDialog();
            }
        }

        /// <summary>
        /// 添加收藏目录、暂时添加到Combox中、添加关键词时才会真正创建到收藏列表
        /// </summary>
        private void AddFavDirToCombBox(object sender, SingleTextInputEventArgs args)
        {
            string favDirName = args.ToStringArray()[0];
            if (string.IsNullOrWhiteSpace(favDirName)) { return; }

            if (!DirItems.Any(n => n == favDirName))
            {
                DirItems.Add(favDirName);
                MainWindow.MainW.Control_Toast.Show($"新建 {favDirName} 收藏目录完成", Toast.MsgType.Success);
            }
            else
            {
                MainWindow.MainW.Control_Toast.Show($"已存在同名收藏目录 {favDirName}", Toast.MsgType.Warning);
            }
            ComboBoxFavDir.SelectedItem = favDirName;
        }

        /// <summary>
        /// 按钮 确定
        /// </summary>
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (editSite == null)
            {
                MainWindow.MainW.Control_Toast.Show("当前没有站点，不能收藏新的标签", Toast.MsgType.Warning, 2000);
                return;
            }

            int result = 0;
            switch (addMode)
            {
                case AddMode.Add:
                    result = TreeViewModel.AddOrEdit(editSite, null, ComboBoxFavDir.SelectedItem.ToSafeString(), TextBoxOriginal.Text, TextBoxMark.Text, false, true);
                    switch (result)
                    {
                        case 1: MainWindow.MainW.Control_Toast.Show("新标签已收藏", Toast.MsgType.Success); break;
                        case 3: MainWindow.MainW.Control_Toast.Show("已有重复的标签在收藏目录中", Toast.MsgType.Warning, 1800); return;
                        default: MainWindow.MainW.Control_Toast.Show("新标签收藏失败", Toast.MsgType.Error); return;
                    }
                    break;

                case AddMode.EditDir:
                    if (string.IsNullOrWhiteSpace(TextBoxMark.Text))
                    {
                        MainWindow.MainW.Control_Toast.Show("新的名称不能是空的", Toast.MsgType.Warning);
                        return;
                    }
                    result = TreeViewModel.AddOrEdit(editSite, ComboBoxFavDir.Tag.ToSafeString(), ComboBoxFavDir.Tag.ToSafeString(),
                        TextBoxOriginal.Text, TextBoxMark.Text, false, false, true);
                    switch (result)
                    {
                        case 1: MainWindow.MainW.Control_Toast.Show("收藏目录改名完成", Toast.MsgType.Success); break;
                        default: MainWindow.MainW.Control_Toast.Show("收藏目录改名失败", Toast.MsgType.Error); return;
                    }
                    break;

                case AddMode.EditKeyword:
                    result = TreeViewModel.AddOrEdit(editSite, ComboBoxFavDir.Tag.ToSafeString(), ComboBoxFavDir.SelectedItem.ToSafeString(),
                        TextBoxOriginal.Text, TextBoxMark.Text, false, true);
                    switch (result)
                    {
                        case 1: MainWindow.MainW.Control_Toast.Show("收藏标签已更新", Toast.MsgType.Success); break;
                        case 3: MainWindow.MainW.Control_Toast.Show("已有重复的标签在收藏目录中", Toast.MsgType.Warning, 1800); return;
                        default: MainWindow.MainW.Control_Toast.Show("收藏标签更新失败", Toast.MsgType.Error); return;
                    }
                    break;
            }

            if (result == 1)
            {
                //更新查找结果
                if (FavoriteWnd.ThisWnd != null && !string.IsNullOrWhiteSpace(FavoriteWnd.ThisWnd.TextBoxFind.Text))
                {
                    TreeViewModel.FindNodes(editSite, FavoriteWnd.ThisWnd.TextBoxFind.Text);
                }
                //保存收藏
                MainWindow.MainW.SaveFavorite();
            }
            Close_Click(sender, e);
        }

        /// <summary>
        /// 按钮 取消
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close_Click(sender, e);
        }
        #endregion #######################################

        /// <summary>
        /// 改变窗口大小
        /// </summary>
        private void ContentWnd_SizeChanged(object sender, SizeChangedEventArgs e)
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

            GlassHelper.EnableBlurBehindWindow(ContainerB, this);
            GC.Collect(2, GCCollectionMode.Optimized);
        }

        /// <summary>
        /// 窗口状态改变
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    return;
                }

                int nStyle = GlassHelper.GetWindowLong(Hwnd, -16);
                nStyle &= ~0x00C00000;
                GlassHelper.SetWindowLong(Hwnd, -16, nStyle);
            }
            GlassHelper.EnableBlurBehindWindow(ContainerB, this);
        }

        /// <summary>
        /// 控制按钮-最小化
        /// </summary>
        private void Min_Click(object sender, RoutedEventArgs e)
        {
            GlassHelper.SendMessage(Hwnd, 0x0112, 0xF020, IntPtr.Zero);
        }

        /// <summary>
        /// 控制按钮-关闭
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 窗口激活
        /// </summary>
        private void Window_Activated(object sender, EventArgs e)
        {
            ContainerB.BorderBrush = new SolidColorBrush(Color.FromRgb(0x35, 0x85, 0xe4));
        }

        /// <summary>
        /// 窗口失去焦点
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            byte gray = 0xbc;
            ContainerB.BorderBrush = new SolidColorBrush(Color.FromRgb(gray, gray, gray));
        }

        /// <summary>
        /// 窗口鼠标按下
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
                UpdateLayout();
            }
        }

        /// <summary>
        /// 窗口关闭中
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //动画结束后 isClose = true 则正式关闭窗口
            if (!isClose)
            {
                #region 关闭动画
                OpacityMask = Resources["ClosedBrush"] as LinearGradientBrush;
                Storyboard std = Resources["ClosedStoryboard"] as Storyboard;
                std.Completed += delegate
                {
                    isClose = true;
                    Window_Closed(sender, e);
                };
                std.Begin();
                #endregion
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 窗口被关闭
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();
            Close();
        }

    }
}
