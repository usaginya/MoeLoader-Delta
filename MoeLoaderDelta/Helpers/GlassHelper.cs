using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 用于Aero效果及Win32窗口相关操作
    /// </summary>
	public class GlassHelper
    {
        //public static IntPtr MakeLParam(int LoWord, int HiWord) 
        //{ 
        //  return (IntPtr)((HiWord << 16) | (LoWord & 0xffff));
        //}

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, uint wParam, IntPtr lParam);

        //[DllImport("User32.dll")]
        //public static extern bool ReleaseCapture();

        //[DllImport("gdi32.dll")]
        //static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("User32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        //public static void FlashWindow(Window wnd)
        //{
        //    FlashWindow(new WindowInteropHelper(wnd).Handle, true);
        //}

        //public const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
        //public const int WINDOWPOSCHANGED = 0x0047;

        #region DWM_BLUR
        enum DWM_BB
        {
            Enable = 1,
            BlurRegion = 2,
            TransitionMaximized = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DWM_BLURBEHIND
        {
            public DWM_BB dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern bool DwmIsCompositionEnabled();

        [DllImport("DwmApi.dll")]
        private static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private static IntPtr _lastRegion = IntPtr.Zero;
        public static bool noBlur = true;

        public static void EnableBlurBehindWindow(Border element, Window window)
        {
            if (noBlur) return;
            try
            {
                if (!DwmIsCompositionEnabled())
                    return;
                if (window.Opacity > 0)
                {
                    // Get the window handle
                    IntPtr hwnd = new WindowInteropHelper(window).Handle;
                    if (hwnd == IntPtr.Zero)
                        throw new InvalidOperationException("Window must be shown to blur");

                    window.Background = Brushes.Transparent;
                    HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;
                    element.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x35, 0x85, 0xe4));

                    if (_lastRegion != IntPtr.Zero)
                        DeleteObject(_lastRegion);

                    //var region = CreateRoundRectRgn((int)element.Margin.Left, (int)element.Margin.Top,
                    //    (int)(element.ActualWidth + element.Margin.Left), (int)(element.ActualHeight + element.Margin.Top),
                    //    (int)element.CornerRadius.TopLeft, (int)element.CornerRadius.TopLeft);
                    const int padding = 6;
                    //var region = CreateRectRgn(padding, padding, (int)element.ActualWidth + padding, (int)element.ActualHeight + padding);
                    var region = CreateRectRgn(padding, padding, (int)element.ActualWidth + padding, (int)element.ActualHeight + padding);

                    _lastRegion = region;

                    // Set Margins
                    DWM_BLURBEHIND blurBehind = new DWM_BLURBEHIND();
                    blurBehind.fEnable = true;
                    blurBehind.fTransitionOnMaximized = false;
                    blurBehind.hRgnBlur = region;
                    //blurBehind.dwFlags = DWM_BB.BlurRegion | DWM_BB.Enable | DWM_BB.TransitionMaximized;
                    blurBehind.dwFlags = DWM_BB.BlurRegion | DWM_BB.Enable;

                    DwmEnableBlurBehindWindow(hwnd, ref blurBehind);
                }
            }
            catch (DllNotFoundException)
            {
            }
        }
        #endregion

        //public static void ExtendFrameIntoClientArea(Window wnd)
        //{
        //    if (System.Environment.OSVersion.Version.Major >= 6)
        //    {
        //        if (GlassHelper.DwmIsCompositionEnabled())
        //        {
        //            MARGINS mg = new MARGINS();
        //            mg.m_buttom = -1;
        //            mg.m_left = -1;
        //            mg.m_right = -1;
        //            mg.m_top = -1;

        //            wnd.Background = Brushes.Transparent;
        //            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(wnd).Handle;
        //            System.Windows.Interop.HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;

        //            DwmExtendFrameIntoClientArea(hwnd, ref mg);
        //        }
        //    }
        //}

        //[DllImport("dwmapi.dll")]
        //private extern static int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margin);
        //private struct MARGINS
        //{
        //    public int m_left;
        //    public int m_right;
        //    public int m_top;
        //    public int m_buttom;
        //};

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hMenu, int nIndex, int dwNewLong);

        //[DllImport("user32.dll")]
        //private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        ////进行禁用后必须进行界面的刷新，否则禁用状态不会立即显示在界面上。
        //public static void DisableMaxmizebox(Window window, bool isDisable)
        //{
        //    int GWL_STYLE = -16;
        //    int WS_MAXIMIZEBOX = 0x00010000;
        //    int SWP_NOSIZE = 0x0001;
        //    int SWP_NOMOVE = 0x0002;
        //    int SWP_FRAMECHANGED = 0x0020;

        //    IntPtr handle = new WindowInteropHelper(window).Handle;

        //    int nStyle = GetWindowLong(handle, GWL_STYLE);
        //    if (isDisable)
        //    {
        //        nStyle &= ~(WS_MAXIMIZEBOX);
        //    }
        //    else
        //    {
        //        nStyle |= WS_MAXIMIZEBOX;
        //    }

        //    SetWindowLong(handle, GWL_STYLE, nStyle);
        //    SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_FRAMECHANGED);
        //}

        #region 处理最大化
        [StructLayout(LayoutKind.Sequential)]
        internal struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        internal struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        #endregion

        #region 关机
        [DllImport("User32.dll")]
        private static extern bool ExitWindowsEx(int uFlags, int dwReserved);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int TOKEN_QUERY = 0x00000008;
        private const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

        //退出类型
        public enum ShutdownType
        {
            LogOff = 0,
            PowerOff = 0x00000008,
            Reboot = 0x00000002
        }

        /// <summary>
        /// 退出系统
        /// </summary>
        /// <param name="type">退出参数</param>
        /// <returns>是否成功</returns>
        public static bool ExitWindows(ShutdownType type)
        {
            bool ok;
            TokPriv1Luid tp;
            IntPtr hproc = GetCurrentProcess();
            IntPtr htok = IntPtr.Zero;
            ok = OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok);
            tp.Count = 1;
            tp.Luid = 0;
            tp.Attr = SE_PRIVILEGE_ENABLED;
            ok = LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tp.Luid);
            ok = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            ok = ExitWindowsEx((int)type, 0);

            return ok;
        }
        #endregion
    }
}