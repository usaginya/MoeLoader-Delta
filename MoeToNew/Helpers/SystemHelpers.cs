using System.Diagnostics;

namespace MoeLoaderDelta.Helpers
{
    class SystemHelpers
    {

        /// <summary>
        /// 結束指定進程名的所有進程
        /// </summary>
        /// <param name="processName">要結束的進程名,不需要.exe後綴</param>
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
