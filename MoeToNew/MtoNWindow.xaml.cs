using MoeLoaderDelta.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Documents;

namespace MoeLoaderDelta
{

    /// <summary>
    /// MainWindow.xaml 的交互邏輯
    /// 2017-5-10       by YIU
    /// </summary>
    public partial class MtoNWindow : Window
    {
        /// <summary>
        /// 此更新程式檔案名
        /// </summary>
        private string UpdateAppName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// 是否有更新
        /// </summary>
        private bool haveUpdate = false;

        /// <summary>
        /// 更新狀態 0檢查更新 1正在更新 2更新完成
        /// </summary>
        private int UpdateState = 0;

        /// <summary>
        /// 更新列表訊息
        /// </summary>
        private MoeUpdateItem UpdateInfo;

        /// <summary>
        /// 需要更新的檔案列表
        /// </summary>
        private List<MoeUpdateFile> UpdateFiles = new List<MoeUpdateFile>();

        /// <summary>
        /// 需要更新的檔案表訊息
        /// </summary>
        private string UpdateFilesInfo = "";

        /// <summary>
        /// 更新檔案暫存目錄
        /// </summary>
        private const string updateTmpPath = "NewMoeLoader";

        /// <summary>
        /// 無更新執行MoeLoader(根據 MoeLoaderDelta 中 Program.cs 啟動參數決定)
        /// </summary>
        private const string noUpdateRunMLD = "⁄(⁄⁄•⁄ω⁄•⁄⁄)⁄NoUpdate";

        /// <summary>
        /// 更新訊息地址
        /// </summary>
        private const string updateInfoUrl = "https://raw.githubusercontent.com/usaginya/mkAppUpInfo/master/MoeLoader-Delta/update.json";


        public MtoNWindow()
        {
            InitializeComponent();

            ReplaceNewFile();

            BindUIEvent();

            try
            {
                haveUpdate = CreateUpdate();
            }
            catch
            { }

            if (haveUpdate)
            {
                ShowUpdate();
            }
            else
            {
                UpdateEnd();
            }
        }

        /// <summary>
        /// 綁定事件
        /// </summary>
        private void BindUIEvent()
        {
            btnY.Click += new RoutedEventHandler(UpdateMoeLoader);
            btnN.Click += new RoutedEventHandler(NotUpdate);
        }

        #region HtmlText
        #region HtmlText example
        //DataContext = this;
        //htmltb.Html = "The [i][u]quick brown fox[/i][/u][br] jumps over the [b]lazy dog[/b] "
        //    +"[br] [binding  path='Title' /][br][a href=https://moekai.moe]萌界[/a]";
        #endregion

        /// <summary>
        /// html文字框載入後綁定事件
        /// </summary>
        private void HtmlTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            AddHandler(Hyperlink.ClickEvent, (RoutedEventHandler)Hyperlink_Click);
        }

        /// <summary>
        /// html文字框點擊連結時事件
        /// </summary>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Hyperlink)
            {
                try
                {
                    Process.Start((e.OriginalSource as Hyperlink).NavigateUri.AbsoluteUri);
                }
                catch { }
                finally
                {
                    e.Handled = true;
                }
            }
        }
        #endregion

        /// <summary>
        /// 檢查更新
        /// </summary>
        /// <returns>是否有更新</returns>
        private bool CreateUpdate()
        {

            #region 取更新訊息
            MyWebClient web = new MyWebClient();
            web.Proxy = WebRequest.DefaultWebProxy;
            string updatejson = web.DownloadString(updateInfoUrl);
            #endregion

            #region 匹配檔案並添加到更新列表
            string localFile = "";

            if (string.IsNullOrWhiteSpace(updatejson))
                return false;

            UpdateInfo = (new MoeUpdateInfo()).GetMoeUpdateInfo(updatejson);
            if (UpdateInfo == null)
                return false;

            foreach (MoeUpdateFile upfile in UpdateInfo.files)
            {
                localFile = RepairPath(upfile.Path) + upfile.Name;
                if (upfile.State == "up" || string.IsNullOrWhiteSpace(upfile.State))
                {
                    if (string.IsNullOrWhiteSpace(localFile) || string.IsNullOrWhiteSpace(upfile.Url))
                        continue;
                    if (DataHelpers.GetMD5Hash(localFile) == upfile.MD5 || DataHelpers.GetMD5Hash(updateTmpPath + "\\" + localFile) == upfile.MD5)
                        continue;
                    UpdateFilesInfo += "+ " + localFile + " - Ver " + upfile.Ver + "  (" + upfile.MD5 + ")[br]";
                    UpdateFiles.Add(upfile);
                }
                else if (upfile.State == "del" && File.Exists(localFile))
                {
                    UpdateFilesInfo += "- " + localFile + "[br]";
                    UpdateFiles.Add(upfile);
                }
            }

            return UpdateFiles.Count > 0;
            #endregion

        }

        /// <summary>
        /// 顯示更新視窗
        /// </summary>
        private void ShowUpdate()
        {
            string line = new string('-', 40);
            htmltb.Html = UpdateInfo.Info + "[br][br][b]" + line + " 以下是更新的內容 " + line + "[/b][br][br]" + UpdateFilesInfo;
            Visibility = Visibility.Visible;
        }

        /// <summary>
        /// btnY 更新
        /// </summary>
        private void UpdateMoeLoader(object sender, RoutedEventArgs e)
        {
            #region 更新
            if (string.IsNullOrWhiteSpace(btnY.Tag as string) && UpdateState < 1)
            {
                StartUpdate();
            }
            #endregion
            #region 更新完成
            else if (UpdateState == 2)
            {
                UpdateEnd();
            }
            #endregion
        }

        /// <summary>
        /// btnN 不更新
        /// </summary>
        private void NotUpdate(object sender, RoutedEventArgs e)
        {
            if (UpdateState != 2)
            {
                try
                {
                    Directory.Delete(updateTmpPath, true);
                }
                catch { }
            }
            else if (UpdateState == 2)
                Environment.Exit(0);

            haveUpdate = false;
            UpdateEnd();
        }

        /// <summary>
        /// 執行MoeLoaderDelta
        /// </summary>
        private void RunMoeLoader(string arg)
        {
            try
            {
                Process.Start("MoeLoaderDelta.exe", arg);
            }
            catch { }
        }
        private void RunMoeLoader()
        {
            RunMoeLoader("");
        }

        /// <summary>
        /// 結束MoeLoaderDelta
        /// </summary>
        private void KillMoeLoader()
        {
            SystemHelpers.KillProcess("MoeLoaderDelta");
        }

        /// <summary>
        /// 開始更新
        /// </summary>
        private void StartUpdate()
        {
            #region 更新初始化
            UpdateState = 1;
            btnY.IsEnabled = false;
            btnY.Content = "正在更新";
            btnN.Content = "取消更新";

            KillMoeLoader();

            int filecount = UpdateFiles.Count;
            if (filecount > 0)
            {
                pbTotal.Value = 0;
                pbTotal.Maximum = filecount;
                DownloadStateUI.Visibility = Visibility.Visible;
            }
            #endregion

            #region 更新處理(包括更新結束)
            DownloadFile();
            #endregion

        }

        /// <summary>
        /// 從更新快取中取代新檔案
        /// </summary>
        private void ReplaceNewFile()
        {
            List<string> exculde = new List<string>();
            exculde.Add(UpdateAppName);
            DataHelpers.MoveFolder(updateTmpPath, ".", exculde);
        }


        /// <summary>
        /// 按順序下載更新列表中的檔案(回調)
        /// </summary>
        /// <param name="FileListCount">更新列表檔案總數</param>
        /// <param name="FileListIndex">當前下載更新列表中的檔案索引</param>
        private void DownloadFile(int FileListCount, int FileListIndex)
        {
            if (FileListIndex < FileListCount && UpdateState == 1)
            {

                MoeUpdateFile nowDLfile = UpdateFiles[FileListIndex];

                try
                {
                    switch (nowDLfile.State)
                    {
                        case "del":
                            #region 刪除訊息顯示
                            Dispatcher.Invoke(new Action(delegate
                            {
                                pbSingleTxt.Text = "移除 " + nowDLfile.Name;
                                pbSingleSpeed.Visibility = pbSingleVal.Visibility = Visibility.Hidden;
                            }));
                            #endregion

                            #region 刪除指定檔案
                            string nowPath = RepairPath(nowDLfile.Path);
                            File.Delete(nowPath + nowDLfile.Name);
                            try
                            {
                                //刪除空目錄
                                DirectoryInfo di = new DirectoryInfo(nowPath);
                                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                                    Directory.Delete(nowPath);
                            }
                            catch { }
                            #endregion

                            Dispatcher.Invoke(new Action(delegate { pbTotal.Value++; }));
                            Thread.Sleep(666);

                            //處理下一個檔案
                            DownloadFile(FileListCount, ++FileListIndex);
                            break;

                        default:
                            #region 下載訊息顯示
                            Dispatcher.Invoke(new Action(delegate
                            {
                                pbSingleTxt.Text = "獲取 " + nowDLfile.Name;
                                pbSingle.Value = 0;
                                pbSingleSpeed.Text = "(0.00 KB/s";
                                pbSingleSpeed.Visibility = pbSingleVal.Visibility = Visibility.Visible;
                            }));
                            #endregion

                            #region 創建下載
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(nowDLfile.Url);
                            req.Proxy = WebRequest.DefaultWebProxy;
                            req.UserAgent = SessionClient.DefUA;
                            req.AllowAutoRedirect = true;

                            HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                            //響應長度
                            double reslength = res.ContentLength;

                            string tmpDLPath = updateTmpPath + "\\" + RepairPath(nowDLfile.Path);
                            if (!Directory.Exists(tmpDLPath))
                            {
                                Directory.CreateDirectory(tmpDLPath);
                            }

                            Stream str = res.GetResponseStream();
                            Stream fileStr = new FileStream(tmpDLPath + nowDLfile.Name, FileMode.Create);
                            #endregion

                            #region 開始執行緒下載檔案
                            //限制執行緒最大數
                            ThreadPool.SetMaxThreads(2, 2);

                            ThreadPool.QueueUserWorkItem((o) =>
                            {
                                byte[] buffer = new byte[1024];
                                double progressBarValue = 0; //進度預置

                                DateTime last = DateTime.Now;
                                int realReadLen = str.Read(buffer, 0, buffer.Length);
                                double downed = realReadLen;
                                double speed = -1;

                                while (realReadLen > 0 && UpdateState == 1)
                                {
                                    fileStr.Write(buffer, 0, realReadLen);
                                    progressBarValue += realReadLen;

                                    try
                                    {
                                        DateTime now = DateTime.Now;
                                        if ((now - last).TotalSeconds > 0.2)
                                        {
                                            speed = downed / (now - last).TotalSeconds;
                                            downed = 0;
                                            last = now;
                                        }

                                        pbSingle.Dispatcher.BeginInvoke(new ProgressBarDelegate(RefreshDownload), progressBarValue / reslength, speed);

                                        realReadLen = str.Read(buffer, 0, buffer.Length);
                                        downed += realReadLen;
                                    }
                                    catch { }
                                }
                                str.Close();
                                fileStr.Close();

                                Dispatcher.Invoke(new Action(delegate { pbTotal.Value++; }));
                                Thread.Sleep(666);

                                //下載完成一個檔案後回調下載下一個
                                DownloadFile(FileListCount, ++FileListIndex);
                            }, null);
                            #endregion
                            break;

                    }
                }
                catch (Exception ex)
                {
                    FileListIndex++;
                    Dispatcher.Invoke(new Action(delegate
                    {
                        pbSingleTxt.Text = nowDLfile.Name + ":" + ex.Message;
                        pbTotal.Value++;
                    }));
                    Thread.Sleep(666);
                }
            }

            #region 更新結束
            if (FileListIndex >= FileListCount && UpdateState == 1)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    UpdateState = 2;
                    DownloadStateUI.Visibility = Visibility.Hidden;
                    btnY.Tag = "y";
                    btnY.IsEnabled = true;
                    btnY.Content = "重啟完成更新";
                    btnN.Content = "退出更新";
                }));
            }
            #endregion
        }
        /// <summary>
        /// 按順序下載更新列表中的檔案
        /// </summary>
        private void DownloadFile()
        {
            //從列表中第1個開始索引為0
            DownloadFile(UpdateFiles.Count, 0);
        }


        /// <summary>
        /// 刷新下載狀態
        /// </summary>
        /// <param name="downloaded">已下載進度</param>
        /// <param name="speed">下載速度</param>
        private delegate void ProgressBarDelegate(double downloaded, double speed);
        private void RefreshDownload(double downloaded, double speed)
        {
            pbSingle.Value = downloaded;

            string speedstr = (speed > 1048575 ? (speed / 1048576).ToString("0.00") + " MB" : (speed / 1024).ToString("0.00") + " KB");
            if (speed > -1) { }
            pbSingleSpeed.Dispatcher.Invoke(new Action(delegate
            {
                pbSingleSpeed.Text = "(" + speedstr + "/s";
            }));
        }

        /// <summary>
        /// 更新程式結束
        /// </summary>
        private void UpdateEnd()
        {
            UpdateState = -1;
            KillMoeLoader();
            RunMoeLoader(haveUpdate ? "" : noUpdateRunMLD);
            Environment.Exit(0);
        }

        /// <summary>
        /// 修復格式不正確的檔案路徑
        /// </summary>
        /// <param name="path">路徑</param>
        private string RepairPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;
            path = path.Replace("/", @"\");
            path = (path.Substring(path.Length - 1, 1) == @"\" ? path : path + @"\");
            return path.Replace(@"\\", @"\");
        }
    }
}
