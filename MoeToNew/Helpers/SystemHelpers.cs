using System.Diagnostics;

namespace MoeLoaderDelta.Helpers
{
    class SystemHelpers
    {

        /// <summary>
        /// 结束指定进程名的所有进程
        /// </summary>
        /// <param name="processName">要结束的进程名,不需要.exe后缀</param>
        public static void KillProcess(string processName)
        {
            try
            {
                Process[] thisproc = Process.GetProcessesByName(processName);
                int procCount = thisproc.Length;

                for (int i = 0; i < procCount; i++)
                {
                    if (!thisproc[i].CloseMainWindow())
                    {
                        thisproc[i].Kill();
                    }
                }
            }
            catch { }
        }

    }
}
