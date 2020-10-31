using System;
using System.Collections.Generic;
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
    /// SingleTextInputWnd.xaml 的交互逻辑
    /// Last 2020-10-31
    /// </summary>
    public partial class SingleTextInputWnd : Window
    {
        /// <summary>
        /// 输入确认事件委托
        /// </summary>
        public delegate void InputValueHandler(object sender, SingleTextInputEventArgs e);
        public event InputValueHandler InputResultEvent;

        /// <summary>
        /// 窗口句柄
        /// </summary>
        private static IntPtr Hwnd;

        /// <summary>
        /// 是否关闭窗口
        /// </summary>
        private bool isClose;

        //+++++++++++++++  方法开始 +++++++++++++++++++++++++
        /// <summary>
        /// 创建输入窗口 创建后通过 InputResultEvent += new InputValueHandler(EventFunction) 方法绑定输入完成事件
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="windowTitle">窗口标题</param>
        /// <param name="windowIcon">窗口图标、路径为项目主目录下的图片、如: Images/ico.png</param>
        /// <param name="inputInfo">输入提示信息</param>
        /// <param name="inputDefaultValue">输入框默认内容</param>
        /// <param name="isMulitline">是否多行输入</param>
        public SingleTextInputWnd(Window owner = null, string windowTitle = null,
            string windowIcon = null, string inputInfo = null, string inputDefaultValue = null, bool isMulitline = false)
        {
            OnWindowCreate(owner, windowTitle, windowIcon, inputInfo, inputDefaultValue, isMulitline);
        }

        /// <summary>
        /// 创建输入窗口 创建后通过 InputResultEvent += new InputValueHandler(EventFunction) 方法绑定输入完成事件
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="inputInfo">输入提示信息</param>
        /// <param name="inputDefaultValue">输入框默认内容</param>
        /// <param name="isMulitline">是否多行输入</param>
        public SingleTextInputWnd(Window owner = null, string inputInfo = null, string inputDefaultValue = null, bool isMulitline = false)
        {
            OnWindowCreate(owner, null, null, inputInfo, inputDefaultValue, isMulitline);
        }

        /// <summary>
        /// 创建输入窗口
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="windowTitle">标题</param>
        /// <param name="windowIcon">图标</param>
        /// <param name="inputInfo">输入提示信息</param>
        /// <param name="inputDefaultValue">输入框默认内容</param>
        /// <param name="isMulitline">是否多行输入</param>
        private void OnWindowCreate(Window owner = null, string windowTitle = null,
            string windowIcon = null, string inputInfo = null, string inputDefaultValue = null, bool isMulitline = false)
        {
            InitializeComponent();
            Owner = owner;
            Title = WindowTitle.Text = windowTitle ?? Title;
            TextBlockInputInfo.Text = inputInfo ?? TextBlockInputInfo.Text;
            TextBoxInputValue.Text = inputDefaultValue ?? TextBoxInputValue.Text;
            TextBoxInputValue.TextWrapping = isMulitline ? TextWrapping.Wrap : TextWrapping.NoWrap;
            TextBoxInputValue.AcceptsReturn = isMulitline;

            if (string.IsNullOrWhiteSpace(windowIcon)) { return; }
            BitmapImage ico = new BitmapImage(new Uri($"pack://application:,,,/{windowIcon}"));
            WindowIcon.Source = ico;
        }

        /// <summary>
        /// 窗口初始化
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Hwnd = new WindowInteropHelper(this).Handle;
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
            TextBoxInputValue.Focus();
            Height = ContainerB.ActualHeight;
            Window_InitializeStyle();
        }

        #region ################ 界面事件方法 ################
        /// <summary>
        /// 按钮 确定
        /// </summary>
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TextBoxInputValue.Text))
            {
                SingleTextInputEventArgs args = new SingleTextInputEventArgs();
                args.Add(TextBoxInputValue.Text);
                InputResultEvent(this, args);
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
        /// 窗口鼠标左键按下
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            UpdateLayout();
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

    /// <summary>
    /// 输入事件参数类
    /// </summary>
    public class SingleTextInputEventArgs : EventArgs
    {
        private List<string> _arg = new List<string>();
        public SingleTextInputEventArgs() { }

        /// <summary>
        /// 添加参数
        /// </summary>
        public void Add(string str)
        {
            _arg.Add(str);
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        public string GetArg(int index)
        {
            return (index < 0 || index >= _arg.Count) ? string.Empty : _arg[index];
        }
    }
}
