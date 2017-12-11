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
        public static bool is_debug = false;

        /// <summary>
        /// 允許多重啟動參數
        /// </summary>
        private const string multipleRunArg = "(=ω=)mRun";

        /// <summary>
        /// 無需更新啟動參數
        /// </summary>
        private const string noUpdateArg = "⁄(⁄⁄•⁄ω⁄•⁄⁄)⁄NoUpdate";

        /// <summary>
        /// 更新程式名
        /// </summary>
        private const string UpdateAppName = "MoeToNew";

        private const string UpdateAppEXEName = UpdateAppName + ".exe";

        /// <summary>
        /// 更新檔案暫存目錄
        /// </summary>
        private const string updateTmpPath = "NewMoeLoader";

        /// <summary>
        /// 更新程式需要的dll
        /// </summary>
        private const string UpdateAppDll = "HtmlTextBlock.dll";

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
                        //從更新程式啟動
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
        /// 恢復設定檔案
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
        /// 取代新的更新程式
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

            //刪除空目錄
            try
            {
                DirectoryInfo di = new DirectoryInfo(updateTmpPath);
                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                    Directory.Delete(updateTmpPath);
            }
            catch { }
        }

        /// <summary>
        /// 刪除更新程式用的多餘檔案 減少硬碟容量占用
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