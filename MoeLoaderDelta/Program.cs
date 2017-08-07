using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace MoeLoaderDelta
{
    class Program
    {
        /// <summary>
        /// 允许多重启动参数
        /// </summary>
        private const string multipleRunArg = "(=ω=)mRun";

        /// <summary>
        /// 无需更新启动参数
        /// </summary>
        private const string noUpdateArg = "⁄(⁄⁄•⁄ω⁄•⁄⁄)⁄NoUpdate";

        /// <summary>
        /// 更新程序名
        /// </summary>
        private const string UpdateAppName = "MoeToNew";
        private const string UpdateAppEXEName = UpdateAppName + ".exe";

        /// <summary>
        /// 更新文件暂存目录
        /// </summary>
        private const string updateTmpPath = "NewMoeLoader";

        /// <summary>
        /// 更新程序需要的dll
        /// </summary>
        private const string UpdateAppDll = "HtmlTextBlock.dll";


        public static bool is_debug = false;

        static Program()
        {
            try
            {
                is_debug = File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\debug.txt");
            }
            catch { }
        }

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThread()]
        [DebuggerNonUserCode()]
        public static void Main(string[] args)
        {
            try
            {
                bool isRuned;
                Mutex mutex = new Mutex(true, "MoeLoaderΔ", out isRuned);

                if (isRuned || args.Length > 0 && args[0] == multipleRunArg)
                {
                    if (args.Length > 0 && args[0] == noUpdateArg || !File.Exists(UpdateAppEXEName) || args.Length > 0 && args[0] == multipleRunArg)
                    {
                        ReplaceUpdateApp();
                        DelRedundantFile();
                        System.Net.ServicePointManager.DefaultConnectionLimit = 100;
                        System.Net.ServicePointManager.Expect100Continue = false;
                        System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                        SplashScreen splashScreen = new SplashScreen("images/slash.png");
                        splashScreen.Show(true);
                        RecoveryConfig();
                        App app = new App();
                        app.InitializeComponent();
                        app.Run();
                        mutex.ReleaseMutex();
                    }
                    else
                    {
                        //从更新程序启动
                        Process.Start(UpdateAppEXEName);
                        Process.GetCurrentProcess().Kill();
                    }
                }
                else
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText("moe_fatal.txt", ex.ToString());
                    System.Media.SystemSounds.Asterisk.Play();
                    (new ErrForm(ex.ToString())).ShowDialog();
                    Process.GetCurrentProcess().Kill();
                }
                catch { }
            }
        }

        public static void Log(Exception e, string desc)
        {
            try
            {
                if (is_debug)
                {
                    File.AppendAllText("moe_log.txt", DateTime.Now + " " + desc + ": " + e.ToString() + "\r\n");
                }
            }
            catch { }
        }

        /// <summary>
        /// 恢复配置文件
        /// </summary>
        private static void RecoveryConfig()
        {
            string appCfg = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                + "\\" + Assembly.GetEntryAssembly().GetName().Name + ".exe.config";

            if (!File.Exists(appCfg))
            {
                File.WriteAllText(appCfg, Properties.Resources.exeConfig);
                Process.Start(Assembly.GetExecutingAssembly().Location, multipleRunArg);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// 替换新的更新程序
        /// </summary>
        private static void ReplaceUpdateApp()
        {
            SystemHelpers.KillProcess(UpdateAppName);

            Thread.Sleep(233);

            string NewUpdate = updateTmpPath + "\\" + UpdateAppEXEName;
            if (File.Exists(NewUpdate))
            {
                File.Delete(UpdateAppEXEName);
                File.Move(NewUpdate, UpdateAppEXEName);
            }

            //删除空目录
            try
            {
                DirectoryInfo di = new DirectoryInfo(updateTmpPath);
                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                    Directory.Delete(updateTmpPath);
            }
            catch { }
        }

        /// <summary>
        /// 删除更新程序用的多余文件 减少硬盘容量占用
        /// </summary>
        private static void DelRedundantFile()
        {
            try
            {
                File.Delete(UpdateAppDll);
            }
            catch { }
        }
    }
}
