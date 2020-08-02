using Microsoft.Win32;
using MoeLoaderDelta.Control;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MoeLoaderDelta.Windows
{
    /// <summary>
    /// FavoriteWnd.xaml 的交互逻辑
    /// Last:2020-7-30
    /// </summary>
    public partial class FavoriteWnd : Window
    {
        /// <summary>
        /// 提供外部操作用
        /// </summary>
        public static FavoriteWnd ThisWnd { get; set; }

        /// <summary>
        /// 窗口句柄
        /// </summary>
        private static IntPtr Hwnd;

        /// <summary>
        /// 窗口标题
        /// </summary>
        private const string windowTitle = "标签收藏夹";

        /// <summary>
        /// 是否关闭窗口
        /// </summary>
        private bool isClose;

        /// <summary>
        /// 是否多选状态
        /// </summary>
        public static bool IsMultiSelect { get; set; }

        //+++++++++++++++  方法开始 +++++++++++++++++++++++++
        /// <summary>
        /// 构造窗口
        /// </summary>
        /// <param name="owner">父窗口</param>
        public FavoriteWnd(Window owner = null)
        {
            InitializeComponent();
            Owner = owner;
            ThisWnd = this;
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
            Title = windowTitle;
            WindowTitle.Text = windowTitle;
            SetDefaultPosition();
            IsMultiSelect = false;
            TextBoxFind.Focus();
            TreeViewFavorite.ItemsSource = MainWindow.MainW.FavoriteTreeView.TreeNodes;
        }

        /// <summary>
        /// 设置默认窗口位置
        /// </summary>
        private void SetDefaultPosition()
        {
            Left = (Owner.WindowState == WindowState.Maximized ? 0 : Owner.Left) + Owner.ActualWidth - ActualWidth - 26;
            Top = Owner.Top + 80;
            Height = Owner.Height - 110;
        }
        #region ################ 界面事件方法 ################

        /// <summary>
        ///  [查找] 按钮点击
        /// </summary>
        private void ButtonFind_Click(object sender, RoutedEventArgs e)
        {
            string siteName = MainWindow.MainW.SelectedSiteName();
            TreeViewModel.FindNodes(siteName, TextBoxFind.Text);
            siteName = string.IsNullOrWhiteSpace(TextBoxFind.Text) ? "已显示全部标签收藏" : $"查找 {siteName} 站点收藏完成";
            MainWindow.MainW.Control_Toast.Show(siteName, Toast.MsgType.Info);
        }

        /// <summary>
        /// [多选] 显示勾选框
        /// </summary>
        private void ButtonShowCheck_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (IsMultiSelect)
            {
                IsMultiSelect = false;
                button.Content = (string)button.Tag;
                TreeViewModel.ClearChecked();
            }
            else
            {
                IsMultiSelect = true;
                button.Content = "单选";
            }
            TreeViewModel.RefreshCheckBoxVisibility();
        }

        /// <summary>
        /// [删除] 删除选中节点或勾选节点
        /// </summary>
        private void ButtonDelCheck_Click(object sender, RoutedEventArgs e)
        {
            bool isDeleted = false;
            const string msgCaption = "删除收藏标签";

            if (IsMultiSelect && TreeViewModel.GetHaveChecked()
                    && MessageBox.Show(this, $"确定要删除全部打勾的标签吗？", msgCaption,
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                TreeViewModel.RemoveCheckedNode();
                isDeleted = true;
            }
            else if (!IsMultiSelect)
            {
                TreeNode node = TreeViewModel.SelectedNode;
                if (node == null || node.Parent == null) { return; }

                string msg = $"确定要删除{(node.Type == TreeNode.NodeType.Dir ? $"【{node.Name}】" : $" {node.Name} ")}吗？";
                msg += node.Type == TreeNode.NodeType.Dir ? $"{Environment.NewLine}注意删除目录会将目录下所有标签删除！" : string.Empty;
                if (MessageBox.Show(this, msg, msgCaption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    TreeViewModel.RemoveSelectedNode();
                    isDeleted = true;
                }

                if (!isDeleted) { return; }
                //更新查找结果
                if (!string.IsNullOrWhiteSpace(TextBoxFind.Text)) { TreeViewModel.FindNodes(MainWindow.MainW.SelectedSiteName(), TextBoxFind.Text); }
                //保存收藏
                MainWindow.MainW.SaveFavorite();
            }
        }

        /// <summary>
        /// [导入] 导入标签收藏文件
        /// </summary>
        private void ButtonImportCheck_Click(object sender, RoutedEventArgs e)
        {
            //无站点无法导入
            if (string.IsNullOrWhiteSpace(MainWindow.MainW.SelectedSiteName()))
            {
                MainWindow.MainW.Control_Toast.Show("当前没有站点，不能导入收藏", Toast.MsgType.Warning, 2000);
                return;
            }

            //打开选择文件窗口
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "导入MLD标签收藏",
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "收藏备份(*.mldf)|*.mldf|收藏列表(*.json)|*.json"
            };
            if ((bool)openFileDialog.ShowDialog())
            {
                string exInfo = TreeViewModel.LoadFavoriteFile(openFileDialog.FileName, openFileDialog.FilterIndex == 1, out int imports, out int repetitions, out int failures);
                if (!string.IsNullOrWhiteSpace(exInfo))
                {
                    MainWindow.MainW.Control_Toast.Show($"导入收藏出错{Environment.NewLine}{exInfo}", Toast.MsgType.Error, 4500);
                    return;
                }

                string completeMsg = $"导入收藏完成{Environment.NewLine}已导入{imports}个";
                completeMsg += repetitions > 0 ? $"，已存在{repetitions}个" : string.Empty;
                completeMsg += failures > 0 ? $"，失败{failures}个" : string.Empty;
                MainWindow.MainW.Control_Toast.Show(completeMsg, imports < 1 ? Toast.MsgType.Warning : Toast.MsgType.Success, 4000);

                if (imports < 1) { return; }
                //更新查找结果
                if (!string.IsNullOrWhiteSpace(TextBoxFind.Text)) { TreeViewModel.FindNodes(MainWindow.MainW.SelectedSiteName(), TextBoxFind.Text); }
                //保存收藏
                MainWindow.MainW.SaveFavorite();
            }
        }

        /// <summary>
        /// [导出] 导出标签收藏文件
        /// </summary>
        private void ButtonExportCheck_Click(object sender, RoutedEventArgs e)
        {
            //打开保存文件窗口
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = "导出MLD标签收藏",
                AddExtension = true,
                CheckPathExists = true,
                OverwritePrompt = true,
                DefaultExt = "mldf",
                Filter = "收藏备份(*.mldf)|*.mldf|收藏列表(*.json)|*.json",
                FileName = "MLD标签收藏.mldf",
            };
            if ((bool)saveFileDialog.ShowDialog())
            {
                TreeViewModel.SaveFavoriteFile(saveFileDialog.FileName, saveFileDialog.FilterIndex == 1);
                MainWindow.MainW.Control_Toast.Show($"已导出标签收藏夹文件{Environment.NewLine}{saveFileDialog.FileName}", Toast.MsgType.Success, 2500);
            }
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
            Opacity = .9;
            ContainerB.BorderBrush = new SolidColorBrush(Color.FromRgb(0x35, 0x85, 0xe4));
            #region - 防止超出可视区 -
            Rect workArea = SystemParameters.WorkArea;
            if (Left + ActualWidth > workArea.Width) { Left = workArea.Width - ActualWidth; }
            if (Top + ActualHeight > workArea.Height) { Top = workArea.Height - ActualHeight; }
            if (Left < 1) { Left = 0; }
            if (Top < 1) { Top = 0; }
            #endregion
        }

        /// <summary>
        /// 窗口失去焦点
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            Opacity = .7;
            byte gray = 0xbc;
            ContainerB.BorderBrush = new SolidColorBrush(Color.FromRgb(gray, gray, gray));
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
        /// 窗口鼠标按下
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {

                ResizeMode windowMode = ResizeMode;
                if (ResizeMode != ResizeMode.NoResize)
                {
                    ResizeMode = ResizeMode.NoResize;
                }
                UpdateLayout();

                DragMove();
                if (ResizeMode != windowMode)
                {
                    ResizeMode = windowMode;
                }
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
            else { MainWindow.MainW.SaveFavorite(); }
        }

        /// <summary>
        /// 窗口被关闭
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();
            Owner.Focus();
            Close();
        }

    }
}
