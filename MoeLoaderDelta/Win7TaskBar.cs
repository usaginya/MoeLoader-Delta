using System;
using System.Runtime.InteropServices;
//using Standard;

namespace MoeLoaderDelta
{
    /// <summary>
    /// Win7任務欄進度條
    /// </summary>
    class Win7TaskBar
    {
        private Win7TaskBar() { }
        private static ITaskbarList taskbarList = null;
        private static ITaskbarList TaskbarList
        {
            get
            {
                if (taskbarList == null)
                {
                    lock (typeof(Win7TaskBar))
                    {
                        if (taskbarList == null)
                        {
                            // Create a new instance of ITaskbarList3
                            taskbarList = (ITaskbarList)new TaskbarList();
                            taskbarList.HrInit();
                        }
                    }
                }
                return taskbarList;
            }
        }

        public static void StopProcess(IntPtr hwnd)
        {
            try
            {
                if ((Environment.OSVersion.Version.Major > 6) || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1))
                {
                    //Windows 7
                    TaskbarList.SetProgressState(hwnd, TbpFlag.NoProgress);
                }
            }
            catch { }
        }

        /// <summary>
        /// 在windows7系統中設定TaskBar中的進度條的值
        /// </summary>
        /// <param name="hwnd">目標視窗句柄</param>
        /// <param name="completed">進度條值，0-100間的數字</param>
        public static void ChangeProcessValue(IntPtr hwnd, uint completed)
        {
            try
            {
                if ((Environment.OSVersion.Version.Major > 6) || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1))
                {
                    //Windows 7
                    TaskbarList.SetProgressState(hwnd, TbpFlag.Normal);
                    TaskbarList.SetProgressValue(hwnd, completed, 100);
                }
            }
            catch { }
        }
    }

    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComImport()]
    internal class TaskbarList { }

    [ComImport(), Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITaskbarList
    {
        #region ITaskbarList
        void HrInit();
        void AddTab(IntPtr hWnd);
        void DeleteTab(IntPtr hWnd);
        void ActivateTab(IntPtr hWnd);
        void SetActiveAlt(IntPtr hWnd);
        #endregion
        #region ITaskbarList2
        void MarkFullscreenWindow(IntPtr hWnd, bool fFullscreen);
        #endregion
        #region ITaskbarList3
        /// <summary>
        /// 設定任務欄顯示進度
        /// </summary>
        /// <param name="hWnd">任務欄對應的視窗句柄</param>
        /// <param name="Completed">進度的當前值</param>
        /// <param name="Total">總的進度值</param>
        void SetProgressValue(IntPtr hWnd, ulong Completed, ulong Total);
        /// <summary>
        /// 設定任務欄狀態
        /// </summary>
        /// <param name="hWnd">任務欄對應的視窗句柄</param>
        /// <param name="Flags">狀態指示，具體見TbpFlag定義</param>
        void SetProgressState(IntPtr hWnd, TbpFlag Flags);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWndTab"></param>
        /// <param name="hWndMDI"></param>
        void RegisterTab(IntPtr hWndTab, IntPtr hWndMDI);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWndTab"></param>
        void UnregisterTab(IntPtr hWndTab);
        void SetTabOrder(IntPtr hWndTab, IntPtr hwndInsertBefore);
        void SetTabActive(IntPtr hWndTab, IntPtr hWndMDI, uint dwReserved);
        void ThumbBarAddButtons(IntPtr hWnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] ThumbButton[] pButtons);
        void ThumbBarUpdateButtons(IntPtr hWnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)]ThumbButton[] pButtons);
        void ThumbBarSetImageList(IntPtr hWnd, IntPtr himl);
        void SetOverlayIcon(IntPtr hWnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)]string pszDescription);
        void SetThumbnailTooltip(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)]string pszTip);
        void SetThumbnailClip(IntPtr hWnd, ref tagRECT prcClip);

        #endregion
    }

    [Flags]
    public enum TbpFlag : uint
    {
        NoProgress = 0x00, //不顯示進度
        Indeterminate = 0x01, //進度循環顯示
        Normal = 0x02, //進度條顯示綠色
        Error = 0x04, //進度條紅色顯示
        Paused = 0x08 //進度條黃色顯示
    }

    /// <summary>
    /// 指名在ThumbButton結構中哪個成員包含有有效訊息
    /// </summary>
    [Flags]
    public enum ThumbButtonMask : uint
    {
        Bitmap = 0x01, //ThumbButton.iBitmap包含有效訊息
        Icon = 0x02, //ThumbButton.hIcon包含有效訊息
        ToolTip = 0x04, //ThumbButton.szTip包含有效訊息
        Flags = 0x08 //ThumbButton.dwFlags包含有效訊息
    }
    [Flags]
    public enum ThumbButtonFlags : uint
    {
        Enabled = 0x00, //按鈕是可用的
        Disabled = 0x01, //按鈕是不可用的
        DisMissonClick = 0x02, //當按鈕被點擊，任務欄按鈕的彈出立刻關閉
        NoBackground = 0x04, //不標示按鈕邊框，只顯示按鈕圖像
        Hidden = 0x08, //隱藏按鈕
        NonInterActive = 0x10 //該按鈕啟用，但沒有互動，沒有按下按鈕的狀態繪製。此值用於按鈕所在的通知是在使用實例。
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct tagRECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
    public struct ThumbButton
    {
        public ThumbButtonMask dwMask;
        public uint iID;
        public uint iBitmap;
        //IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szTip;
        public ThumbButtonFlags dwFlags;
    }
}
