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
    /// MainWindow.xaml 的交互逻辑
    /// 2017-05-10       by YIU
    /// Last 20180717
    /// </summary>
    public partial class MtoNWindow : Window
    {
        /// <summary>
        /// 此更新程序文件名
        /// </summary>
        private string UpdateAppName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// 是否有更新
        /// </summary>
        private bool haveUpdate = false;

        /// <summary>
        /// 更新状态 0检查更新 1正在更新 2更新完成
        /// </summary>
        private int UpdateState = 0;

        /// <summary>
        /// 更新列表信息
        /// </summary>
        private MoeUpdateItem UpdateInfo;

        /// <summary>
        /// 需要更新的文件列表
        /// </summary>
        private List<MoeUpdateFile> UpdateFiles = new List<MoeUpdateFile>();

        /// <summary>
        /// 需要更新的文件表信息
        /// </summary>
        private string UpdateFilesInfo = "";

        /// <summary>
        /// 更新文件暂存目录
        /// </summary>
        private const string updateTmpPath = "NewMoeLoader";

        /// <summary>
        /// 无更新运行MoeLoader(根据 MoeLoaderDelta 中 Program.cs 启动参数决定)
        /// </summary>
        private const string noUpdateRunMLD = "⁄(⁄⁄•⁄ω⁄•⁄⁄)⁄NoUpdate";

        /// <summary>
        /// 更新信息地址
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
        /// 绑定事件
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
        /// html文本框载入后绑定事件
        /// </summary>
        private void HtmlTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            AddHandler(Hyperlink.ClickEvent, (RoutedEventHandler)Hyperlink_Click);
        }

        /// <summary>
        /// html文本框点击链接时事件
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
        /// 检查更新
        /// </summary>
        /// <returns>是否有更新</returns>
        private bool CreateUpdate()
        {

            #region 取更新信息
            MyWebClient web = new MyWebClient();
            web.Proxy = WebRequest.DefaultWebProxy;
            string updatejson = web.DownloadString(updateInfoUrl);
            #endregion

            #region 匹配文件并添加到更新列表
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
        /// 显示更新窗口
        /// </summary>
        private void ShowUpdate()
        {
            string line = new string('-', 40);
            htmltb.Html = UpdateInfo.Info + "[br][br][b]" + line + " 以下是更新的内容 " + line + "[/b][br][br]" + UpdateFilesInfo;
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
        /// 运行MoeLoaderDelta
        /// </summary>
        private void RunMoeLoader(string arg)
        {
            try
            {
                string nowDy = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = nowDy + "MoeLoaderDelta.exe";
                psi.UseShellExecute = false;
                psi.WorkingDirectory = nowDy;
                psi.CreateNoWindow = true;
                psi.Arguments = arg;
                Process.Start(psi);
            }
            catch { }
        }
        private void RunMoeLoader()
        {
            RunMoeLoader("");
        }

        /// <summary>
        /// 结束MoeLoaderDelta
        /// </summary>
        private void KillMoeLoader()
        {
            SystemHelpers.KillProcess("MoeLoaderDelta");
        }

        /// <summary>
        /// 开始更新
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

            #region 更新处理(包括更新结束)
            DownloadFile();
            #endregion

        }

        /// <summary>
        /// 从更新缓存中替换新文件
        /// </summary>
        private void ReplaceNewFile()
        {
            List<string> exculde = new List<string>();
            exculde.Add(UpdateAppName);
            DataHelpers.MoveFolder(updateTmpPath, ".", exculde);
        }


        /// <summary>
        /// 按顺序下载更新列表中的文件(回调)
        /// </summary>
        /// <param name="FileListCount">更新列表文件总数</param>
        /// <param name="FileListIndex">当前下载更新列表中的文件索引</param>
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
                            #region 删除信息显示
                            Dispatcher.Invoke(new Action(delegate
                            {
                                pbSingleTxt.Text = "移除 " + nowDLfile.Name;
                                pbSingleSpeed.Visibility = pbSingleVal.Visibility = Visibility.Hidden;
                            }));
                            #endregion

                            #region 删除指定文件
                            string nowPath = RepairPath(nowDLfile.Path);
                            File.Delete(nowPath + nowDLfile.Name);
                            try
                            {
                                //删除空目录
                                DirectoryInfo di = new DirectoryInfo(nowPath);
                                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                                    Directory.Delete(nowPath);
                            }
                            catch { }
                            #endregion

                            Dispatcher.Invoke(new Action(delegate { pbTotal.Value++; }));
                            Thread.Sleep(666);

                            //处理下一个文件
                            DownloadFile(FileListCount, ++FileListIndex);
                            break;

                        default:
                            #region 下载信息显示
                            Dispatcher.Invoke(new Action(delegate
                            {
                                pbSingleTxt.Text = "获取 " + nowDLfile.Name;
                                pbSingle.Value = 0;
                                pbSingleSpeed.Text = "(0.00 KB/s";
                                pbSingleSpeed.Visibility = pbSingleVal.Visibility = Visibility.Visible;
                            }));
                            #endregion

                            #region 创建下载
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(nowDLfile.Url);
                            req.Proxy = WebRequest.DefaultWebProxy;
                            req.UserAgent = SessionClient.DefUA;
                            req.AllowAutoRedirect = true;

                            HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                            //响应长度
                            double reslength = res.ContentLength;

                            string tmpDLPath = updateTmpPath + "\\" + RepairPath(nowDLfile.Path);
                            if (!Directory.Exists(tmpDLPath))
                            {
                                Directory.CreateDirectory(tmpDLPath);
                                new DirectoryInfo(tmpDLPath).Attributes = FileAttributes.Hidden;
                            }

                            Stream str = res.GetResponseStream();
                            Stream fileStr = new FileStream(tmpDLPath + nowDLfile.Name, FileMode.Create);
                            #endregion

                            #region 开始线程下载文件
                            //限制线程最大数
                            ThreadPool.SetMaxThreads(2, 2);

                            ThreadPool.QueueUserWorkItem((o) =>
                            {
                                byte[] buffer = new byte[1024];
                                double progressBarValue = 0; //进度预置

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

                                //下载完成一个文件后回调下载下一个
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

            #region 更新结束
            if (FileListIndex >= FileListCount && UpdateState == 1)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    UpdateState = 2;
                    DownloadStateUI.Visibility = Visibility.Hidden;
                    btnY.Tag = "y";
                    btnY.IsEnabled = true;
                    btnY.Content = "重启完成更新";
                    btnN.Content = "退出更新";
                }));
            }
            #endregion
        }
        /// <summary>
        /// 按顺序下载更新列表中的文件
        /// </summary>
        private void DownloadFile()
        {
            //从列表中第1个开始索引为0
            DownloadFile(UpdateFiles.Count, 0);
        }


        /// <summary>
        /// 刷新下载状态
        /// </summary>
        /// <param name="downloaded">已下载进度</param>
        /// <param name="speed">下载速度</param>
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
        /// 更新程序结束
        /// </summary>
        private void UpdateEnd()
        {
            UpdateState = -1;
            KillMoeLoader();
            RunMoeLoader(haveUpdate ? "" : noUpdateRunMLD);
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// 修复格式不正确的文件路径
        /// </summary>
        /// <param name="path">路径</param>
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
