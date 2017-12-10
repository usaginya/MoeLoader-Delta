using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;

namespace MoeLoaderDelta
{
    public enum DLWorkMode { Retry, Stop, Remove, Del, RetryAll, StopAll, RemoveAll }
    public delegate void DownloadHandler(long size, double percent, string url, double speed);

    public struct MiniDownloadItem
    {
        public string url;
        public string fileName;
        public string host;
        public string author;
        public string localName;
        public string localfileName;
        public int id;
        public bool noVerify;
        public MiniDownloadItem(string file, string url, string host, string author, string localName, string localfileName, int id, bool noVerify)
        {
            //原始后缀名
            string ext = "";

            if (file != null && file != "")
            {
                ext = url.Substring(url.LastIndexOf('.'), url.Length - url.LastIndexOf('.'));
                fileName = file.EndsWith(ext) ? file : file + ext;
            }
            else
            {
                fileName = file;
            }

            this.url = url;
            this.host = host;
            this.author = author;
            this.localName = localName.EndsWith(ext) ? localName
                : localName.IsNullOrEmptyOrWhiteSpace() ? fileName : localName + ext;
            this.localfileName = localfileName.EndsWith(ext) ? localfileName
                : localfileName.IsNullOrEmptyOrWhiteSpace() ? fileName : localfileName + ext;
            this.id = id;
            this.noVerify = noVerify;
        }
    }

    /// <summary>
    /// Interaction logic for DownloadControl.xaml
    /// 下载面板用户控件
    /// </summary>
    public partial class DownloadControl : UserControl
    {
        public ScrollViewer Scrollviewer
        {
            get
            {
                return (ScrollViewer)dlList.Template.FindName("dlListSView", dlList);
            }
        }

        public const string DLEXT = ".moe";
        private const string dlerrtxt = "下载失败下载未完成";

        //一个下载任务
        private class DownloadTask
        {
            public string Url { get; set; }
            public string SaveLocation { set; get; }
            public bool IsStop { set; get; }
            public string NeedReferer { get; set; }
            public bool NoVerify { get; set; }

            /// <summary>
            /// 下载任务
            /// </summary>
            /// <param name="url">目标地址</param>
            /// <param name="saveLocation">保存位置</param>
            /// <param name="referer">是否需要伪造Referer</param>
            public DownloadTask(string url, string saveLocation, string referer, bool noVerify)
            {
                SaveLocation = saveLocation;
                Url = url;
                NeedReferer = referer;
                NoVerify = noVerify;
                IsStop = false;
            }
        }

        //下载对象
        private ObservableCollection<DownloadItem> downloadItems = new ObservableCollection<DownloadItem>();
        public ObservableCollection<DownloadItem> DownloadItems
        {
            get { return downloadItems; }
        }

        //downloadItems的副本，用于快速查找
        private Dictionary<string, DownloadItem> downloadItemsDic = new Dictionary<string, DownloadItem>();

        private bool isWorking = false;
        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsWorking
        {
            get { return isWorking; }
            //set { isWorking = value; }
        }

        private int numOnce;
        /// <summary>
        /// 同时下载的任务数量
        /// </summary>
        public int NumOnce
        {
            set
            {
                if (value > 5) value = 5;
                else if (value < 1) value = 1;

                numOnce = value;
                //SetNum(value);
            }
            get { return numOnce; }
        }

        /// <summary>
        /// 分站点存放
        /// </summary>
        public bool IsSepSave { get; set; }
        /// <summary>
        /// 分上传者存放
        /// </summary>
        public bool IsSaSave { get; set; }

        private static string saveLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        /// <summary>
        /// 下载的保存位置
        /// </summary>
        public static string SaveLocation { get { return saveLocation; } set { saveLocation = value; } }

        private int numSaved = 0;
        private int numLeft = 0;

        //正在下载的链接
        private Dictionary<string, DownloadTask> webs = new Dictionary<string, DownloadTask>();

        public DownloadControl()
        {
            InitializeComponent();

            NumOnce = 2;
            IsSepSave = IsSaSave = false;

            downloadStatus.Text = "当前无下载任务";

            dlList.DataContext = this;
        }

        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="urls"></param>
        public void AddDownload(IEnumerable<MiniDownloadItem> items)
        {
            foreach (MiniDownloadItem item in items)
            {
                string fileName = item.fileName;
                if (fileName == null || fileName.Trim().Length == 0)
                    fileName = Uri.UnescapeDataString(item.url.Substring(item.url.LastIndexOf('/') + 1));

                try
                {
                    DownloadItem itm = new DownloadItem(fileName, item.url, item.host, item.author, item.localName, item.localfileName, item.id, item.noVerify);
                    downloadItemsDic.Add(item.url, itm);
                    downloadItems.Add(itm);
                    numLeft = numLeft < 0 ? 0 : numLeft;
                    numLeft++;
                }
                catch (ArgumentException) { }//duplicate entry
            }

            if (!isWorking)
            {
                isWorking = true;
            }

            RefreshList();
        }

        /// <summary>
        /// 取本地保存目录
        /// </summary>
        /// <param name="dlitem">下载项</param>
        /// <returns></returns>
        private string GetLocalPath(DownloadItem dlitem)
        {
            string path = "";

            if (IsSepSave)
            {
                string sPath = saveLocation + "\\" + dlitem.Host + (IsSaSave ? "\\" + dlitem.Author : "");
                if (!Directory.Exists(sPath))
                    Directory.CreateDirectory(sPath);

                path = sPath + "\\";
            }
            else
            {
                path = saveLocation + "\\";
            }
            return path;
        }

        /// <summary>
        /// 刷新下载状态
        /// </summary>
        private void RefreshList()
        {
            TotalProgressChanged();

            //根据numOnce及正在下载的情况生成下载
            int downloadingCount = webs.Count;
            for (int j = 0; j < NumOnce - downloadingCount; j++)
            {
                if (numLeft > 0)
                {
                    DownloadItem dlitem = downloadItems[downloadItems.Count - numLeft];

                    string url = dlitem.Url;
                    string file = dlitem.FileName.Replace("\r\n", "");
                    string path = GetLocalPath(dlitem);

                    //检查目录长度
                    if (path.Length > 248)
                    {
                        downloadItems[downloadItems.Count - numLeft].StatusE = DLStatus.Failed;
                        downloadItems[downloadItems.Count - numLeft].Size = "路径太长";
                        WriteErrText(url + ": 路径太长");
                        j--;
                    }
                    else
                    {
                        dlitem.LocalFileName = ReplaceInvalidPathChars(file, path, 0);
                        file = dlitem.LocalName = path + dlitem.LocalFileName;

                        //检查全路径长度
                        if (file.Length > 258)
                        {
                            downloadItems[downloadItems.Count - numLeft].StatusE = DLStatus.Failed;
                            downloadItems[downloadItems.Count - numLeft].Size = "路径太长";
                            WriteErrText(url + ": 路径太长");
                            j--;
                        }
                    }

                    if (File.Exists(file))
                    {
                        downloadItems[downloadItems.Count - numLeft].StatusE = DLStatus.IsHave;
                        downloadItems[downloadItems.Count - numLeft].Size = "已存在跳过";
                        j--;
                    }
                    else if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    else
                    {
                        downloadItems[downloadItems.Count - numLeft].StatusE = DLStatus.DLing;

                        DownloadTask task = new DownloadTask(url, file, MainWindow.IsNeedReferer(url), dlitem.NoVerify);
                        webs.Add(url, task);

                        //异步下载开始
                        System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(Download));
                        thread.Start(task);
                    }

                    numLeft--;
                }
                else break;
            }
            RefreshStatus();
        }

        /// <summary>
        /// 下载，另一线程
        /// </summary>
        /// <param name="o"></param>
        private void Download(object o)
        {
            DownloadTask task = (DownloadTask)o;
            FileStream fs = null;
            Stream str = null;
            SessionClient sc = new SessionClient();
            System.Net.WebResponse res = null;
            double downed = 0;
            string lpath = GetLocalPath(downloadItemsDic[task.Url]);

            try
            {
                res = sc.GetWebResponse(
                    task.Url,
                    MainWindow.WebProxy,
                    task.NeedReferer
                    );

                /////////开始写入文件
                str = res.GetResponseStream();
                byte[] bytes = new byte[5120];
                fs = new FileStream(task.SaveLocation + DLEXT, FileMode.Create);

                int bytesReceived = 0;
                DateTime last = DateTime.Now;
                int osize = str.Read(bytes, 0, bytes.Length);
                downed = osize;
                while (!task.IsStop && osize > 0)
                {
                    fs.Write(bytes, 0, osize);
                    bytesReceived += osize;
                    DateTime now = DateTime.Now;
                    double speed = -1;
                    if ((now - last).TotalSeconds > 0.6)
                    {
                        speed = downed / (now - last).TotalSeconds / 1024.0;
                        downed = 0;
                        last = now;
                    }
                    Dispatcher.Invoke(new DownloadHandler(web_DownloadProgressChanged),
                        res.ContentLength, bytesReceived / (double)res.ContentLength * 100.0, task.Url, speed);
                    osize = str.Read(bytes, 0, bytes.Length);
                    downed += osize;
                }
            }
            catch (Exception ex)
            {
                //Dispatcher.Invoke(new UIdelegate(delegate(object sender) { StopLoadImg(re.Key, re.Value); }), "");
                task.IsStop = true;
                Dispatcher.Invoke(new VoidDel(delegate ()
                {
                    //下载失败
                    if (downloadItemsDic.ContainsKey(task.Url))
                    {
                        downloadItemsDic[task.Url].StatusE = DLStatus.Failed;
                        downloadItemsDic[task.Url].Size = "下载失败";
                        WriteErrText(task.Url);
                        WriteErrText(task.SaveLocation);
                        WriteErrText(ex.Message + "\r\n");

                        try
                        {
                            if (fs != null)
                                fs.Close();
                            if (str != null)
                                str.Close();
                            if (res != null)
                                res.Close();

                            File.Delete(task.SaveLocation + DLEXT);

                            DirectoryInfo di = new DirectoryInfo(lpath);
                            if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                                Directory.Delete(lpath);
                        }
                        catch { }
                    }
                }));
            }
            finally
            {
                try
                {
                    if (fs != null)
                        fs.Close();
                    if (str != null)
                        str.Close();
                    if (res != null)
                        res.Close();
                }
                catch { }
            }

            if (task.IsStop)
            {
                //任务被取消
                Dispatcher.Invoke(new VoidDel(delegate ()
                {
                    if (downloadItemsDic.ContainsKey(task.Url))
                    {
                        if (!dlerrtxt.Contains(downloadItemsDic[task.Url].Size))
                        {
                            downloadItemsDic[task.Url].StatusE = DLStatus.Cancel;
                        }
                    }
                }));

                try
                {
                    File.Delete(task.SaveLocation + DLEXT);

                    DirectoryInfo di = new DirectoryInfo(lpath);
                    if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                        Directory.Delete(lpath);
                }
                catch { }
            }
            else
            {
                //下载成功完成
                Dispatcher.Invoke(new VoidDel(delegate ()
                {
                    try
                    {
                        //DownloadTask task1 = obj as DownloadTask;

                        //判断完整性
                        if (!downloadItemsDic[task.Url].NoVerify && 100 - downloadItemsDic[task.Url].Progress > 0.001)
                        {
                            task.IsStop = true;
                            downloadItemsDic[task.Url].StatusE = DLStatus.Failed;
                            downloadItemsDic[task.Url].Size = "下载未完成";
                            try
                            {
                                File.Delete(task.SaveLocation + DLEXT);

                                DirectoryInfo di = new DirectoryInfo(lpath);
                                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                                    Directory.Delete(lpath);
                            }
                            catch { }
                        }
                        else
                        {
                            //修改后缀名
                            File.Move(task.SaveLocation + DLEXT, task.SaveLocation);

                            downloadItemsDic[task.Url].Progress = 100.0;
                            downloadItemsDic[task.Url].StatusE = DLStatus.Success;
                            //downloadItemsDic[task.Url].Size = (downed > 1048576
                            //? (downed / 1048576.0).ToString("0.00MB")
                            //: (downed / 1024.0).ToString("0.00KB"));
                            numSaved++;
                        }
                    }
                    catch { }
                }));
            }

            //下载结束
            Dispatcher.Invoke(new VoidDel(delegate ()
            {
                webs.Remove(task.Url);
                RefreshList();
            }));
        }

        private void WriteErrText(string content)
        {
            try
            {
                File.AppendAllText(saveLocation + "\\moedl_error.log", content + "\r\n");
            }
            catch { }
        }


        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void RefreshStatus()
        {
            if (webs.Count > 0)
            {
                downloadStatus.Text = "已保存 " + numSaved + " 剩余 " + numLeft + " 正在下载 " + webs.Count;
            }
            else
            {
                isWorking = false;
                downloadStatus.Text = "已保存 " + numSaved + " 剩余 " + numLeft + " 下载完毕";
            }

            if (downloadItems.Count == 0)
                blkTip.Visibility = Visibility.Visible;
            else blkTip.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 下载进度发生改变
        /// </summary>
        /// <param name="total"></param>
        /// <param name="percent"></param>
        /// <param name="url"></param>
        void web_DownloadProgressChanged(long total, double percent, string url, double speed)
        {
            try
            {
                string size = total > 1048576 ? (total / 1048576.0).ToString("0.00MB") : (total / 1024.0).ToString("0.00KB");
                downloadItemsDic[url].Size = size;
                downloadItemsDic[url].Progress = percent > 100 ? 100 : percent;
                if (speed > 0)
                    downloadItemsDic[url].SetSpeed(speed);
            }
            catch { }
        }

        /// <summary>
        /// 总下载进度，根据下载完成的图片数量计算
        /// </summary>
        private void TotalProgressChanged()
        {
            if (downloadItems.Count > 0)
            {
                double percent = (double)(downloadItems.Count - numLeft - webs.Count) / (double)downloadItems.Count * 100.0;

                Win7TaskBar.ChangeProcessValue(MainWindow.Hwnd, (uint)percent);

                if (Math.Abs(percent - 100.0) < 0.001)
                {
                    Win7TaskBar.StopProcess(MainWindow.Hwnd);
                    if (GlassHelper.GetForegroundWindow() != MainWindow.Hwnd)
                    {
                        //System.Media.SystemSounds.Beep.Play();
                        GlassHelper.FlashWindow(MainWindow.Hwnd, true);
                    }

                    #region 关机
                    if (itmAutoClose.IsChecked)
                    {
                        //关机
                        System.Timers.Timer timer = new System.Timers.Timer()
                        {
                            //20秒后关闭
                            Interval = 20000,
                            Enabled = false,
                            AutoReset = false
                        };
                        timer.Elapsed += delegate { GlassHelper.ExitWindows(GlassHelper.ShutdownType.PowerOff); };
                        timer.Start();

                        if (MessageBox.Show("系统将于20秒后自动关闭，若要取消请点击确定", MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)
                        {
                            timer.Stop();
                        }
                    }
                    #endregion
                }
            }
            else
            {
                Win7TaskBar.StopProcess(MainWindow.Hwnd);
            }
        }

        /// <summary>
        /// 去掉文件名中的无效字符,如 \ / : * ? " < > | 
        /// </summary>
        /// <param name="file">待处理的文件名</param>
        /// <param name="replace">替换字符</param>
        /// <returns>处理后的文件名</returns>
        public static string ReplaceInvalidPathChars(string file, string replace)
        {
            if (file.IndexOf('?', file.LastIndexOf('.')) > 0)
            {
                //adfadsf.jpg?adfsdf   remove trailing ?param
                file = file.Substring(0, file.IndexOf('?'));
            }

            foreach (char rInvalidChar in Path.GetInvalidFileNameChars())
                file = file.Replace(rInvalidChar.ToSafeString(), replace);
            return file;
        }
        /// <summary>
        /// 去掉文件名中的无效字符,如 \ / : * ? " < > | 
        /// </summary>
        public static string ReplaceInvalidPathChars(string file)
        {
            return ReplaceInvalidPathChars(file, "");
        }
        /// <summary>
        /// 去掉文件名中无效字符的同时裁剪过长文件名
        /// </summary>
        /// <param name="file">文件名</param>
        /// <param name="path">所在路径</param>
        /// <param name="any">任何数</param>
        /// <returns></returns>
        public static string ReplaceInvalidPathChars(string file, string path, int any)
        {
            if (path.Length + file.Length > 258 && file.Contains("<!<"))
            {
                string last = file.Substring(file.LastIndexOf("<!<"));
                file = file.Substring(0, 258 - last.Length - path.Length - last.Length) + last;
            }
            file = file.Replace("<!<", "");
            return ReplaceInvalidPathChars(file);
        }

        /// <summary>
        /// 导出lst
        /// </summary>
        private void itmLst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
                {
                    DefaultExt = "lst",
                    FileName = "MoeLoaderList.lst",
                    Filter = "lst文件|*.lst",
                    OverwritePrompt = false
                };
                if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string text = "";
                    int success = 0, repeat = 0;
                    //读存在的lst内容
                    string[] flst = null;
                    bool havelst = File.Exists(saveFileDialog1.FileName);
                    bool isexists = false;

                    if (havelst)
                    {
                        flst = File.ReadAllLines(saveFileDialog1.FileName);
                    }

                    foreach (DownloadItem i in dlList.SelectedItems)
                    {
                        //查找重复项
                        try
                        {
                            isexists = havelst && flst.Any(x => x.Split('|')[2] == i.Host && x.Split('|')[4] == i.Id.ToSafeString());
                        }
                        catch { }

                        if (!isexists)
                        {
                            //url|文件名|域名|上传者|ID(用于判断重复)|免文件校验
                            text += i.Url
                                + "|" + i.LocalFileName
                                + "|" + i.Host
                                + "|" + i.Author
                                + "|" + i.Id
                                + "|" + (i.NoVerify ? 'v' : 'x')
                                + "\r\n";
                            success++;
                        }
                        else
                            repeat++;
                    }
                    File.AppendAllText(saveFileDialog1.FileName, text);
                    MessageBox.Show("成功保存 " + success + " 个地址\r\n" + repeat + " 个地址已在列表中\r\n", MainWindow.ProgramName,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:\r\n" + ex.Message, MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 复制地址
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmCopy_Click(object sender, RoutedEventArgs e)
        {
            DownloadItem i = (DownloadItem)dlList.SelectedItem;
            if (i == null) return;
            string text = i.Url;
            try
            {
                Clipboard.SetText(text);
            }
            catch { }
        }

        //============================== Menu Function ===================================
        private void ExecuteDownloadListTask(DLWorkMode dlworkmode)
        {
            bool delitemfile = false;
            int selectcs = 0;
            List<DownloadItem> selected = new List<DownloadItem>();
            if (dlworkmode == DLWorkMode.RetryAll || dlworkmode == DLWorkMode.StopAll || dlworkmode == DLWorkMode.RemoveAll)
            {
                foreach (object o in dlList.Items)
                {
                    //转存集合，防止selected改变
                    DownloadItem item = (DownloadItem)o;
                    selected.Add(item);
                }
                selectcs = selected.Count;
            }
            else
            {
                foreach (object o in dlList.SelectedItems)
                {
                    DownloadItem item = (DownloadItem)o;
                    selected.Add(item);
                }
            }

            string lpath = "";
            DirectoryInfo di;

            foreach (DownloadItem item in selected)
            {
                switch (dlworkmode)
                {
                    case DLWorkMode.Retry:
                    case DLWorkMode.RetryAll:
                        if (item.StatusE == DLStatus.Failed || item.StatusE == DLStatus.Cancel || item.StatusE == DLStatus.IsHave)
                        {
                            numLeft = numLeft > selectcs ? selectcs : numLeft;
                            downloadItems.Remove(item);
                            downloadItemsDic.Remove(item.Url);
                            AddDownload(new MiniDownloadItem[] {
                                new MiniDownloadItem(item.FileName, item.Url, item.Host, item.Author, item.LocalName, item.LocalFileName,
                                item.Id, item.NoVerify)
                            });
                        }
                        break;

                    case DLWorkMode.Stop:
                    case DLWorkMode.StopAll:
                        if (item.StatusE == DLStatus.DLing || item.StatusE == DLStatus.Wait)
                        {
                            if (webs.ContainsKey(item.Url))
                            {
                                webs[item.Url].IsStop = true;
                                webs.Remove(item.Url);
                            }
                            else
                                numLeft--;

                            if (dlworkmode == DLWorkMode.StopAll)
                            {
                                numLeft = 0;
                            }
                            item.StatusE = DLStatus.Cancel;
                            item.Size = "已取消";

                            try
                            {
                                File.Delete(item.LocalFileName + DLEXT);

                                lpath = GetLocalPath(item);
                                di = new DirectoryInfo(lpath);
                                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                                    Directory.Delete(lpath);
                            }
                            catch { }
                        }
                        break;

                    case DLWorkMode.Del:
                    case DLWorkMode.Remove:
                    case DLWorkMode.RemoveAll:
                        if (dlworkmode == DLWorkMode.Del && !delitemfile)
                        {
                            if (MessageBox.Show("QwQ 真的要把任务和文件一起删除么？",
                                MainWindow.ProgramName,
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) == MessageBoxResult.No)
                            { break; }
                            else
                            { delitemfile = true; }
                        }

                        if (item.StatusE == DLStatus.DLing)
                        {
                            if (webs.ContainsKey(item.Url))
                            {
                                webs[item.Url].IsStop = true;
                                webs.Remove(item.Url);
                            }
                        }
                        else if (item.StatusE == DLStatus.Success || item.StatusE == DLStatus.IsHave)
                            numSaved = numSaved > 0 ? --numSaved : 0;
                        else if (item.StatusE == DLStatus.Wait || item.StatusE == DLStatus.Cancel)
                            numLeft = numLeft > 0 ? --numLeft : 0;

                        downloadItems.Remove(item);
                        downloadItemsDic.Remove(item.Url);

                        //删除文件
                        string fname = item.LocalName;
                        if (dlworkmode == DLWorkMode.Del)
                        {
                            if (File.Exists(fname))
                            {
                                File.Delete(fname);
                            }

                            lpath = GetLocalPath(item);
                            di = new DirectoryInfo(lpath);
                            if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                                Directory.Delete(lpath);
                        }
                        break;
                }
            }
            if (dlworkmode == DLWorkMode.Stop || dlworkmode == DLWorkMode.Remove)
            {
                RefreshList();
            }
            if (dlworkmode == DLWorkMode.Remove)
            {
                RefreshStatus();
            }
        }
        //================================================================================
        /// <summary>
        /// 重试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmRetry_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDownloadListTask(DLWorkMode.Retry);
        }

        /// <summary>
        /// 停止某个任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmStop_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDownloadListTask(DLWorkMode.Stop);
        }

        /// <summary>
        /// 移除某个任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmDelete_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDownloadListTask(DLWorkMode.Remove);
        }

        /// <summary>
        /// 停止所有下载
        /// </summary>
        public void StopAll()
        {
            downloadItems.Clear();
            downloadItemsDic.Clear();
            foreach (DownloadTask item in webs.Values)
            {
                item.IsStop = true;
            }
        }

        /// <summary>
        /// 清空已成功任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void itmClearDled_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            while (true)
            {
                if (i >= downloadItems.Count) break;
                DownloadItem item = downloadItems[i];
                if (item.StatusE == DLStatus.Success)
                {
                    downloadItems.RemoveAt(i);
                    downloadItemsDic.Remove(item.Url);
                }
                else
                {
                    i++;
                }
            }
            numSaved = 0;
            RefreshStatus();
        }

        #region 遗弃的方法
        /// <summary>
        /// 选择保存位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void itmSaveLocation_Click(object sender, RoutedEventArgs e)
        //{
        //    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
        //    {
        //        Description = "当前保存位置: " + saveLocation,
        //        SelectedPath = saveLocation
        //    };

        //    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //    {
        //        SaveLocation = dialog.SelectedPath;
        //    }
        //}

        //#region 设置同时下载数量
        //public void SetNum(int i)
        //{
        //    if (i == 1)
        //    {
        //        itm1.IsChecked = true;
        //    }
        //    else if (i == 2)
        //    {
        //        itm2.IsChecked = true;
        //    }
        //    else if (i == 3)
        //    {
        //        itm3.IsChecked = true;
        //    }
        //    else if (i == 4)
        //    {
        //        itm4.IsChecked = true;
        //    }
        //    else if (i == 5)
        //    {
        //        itm5.IsChecked = true;
        //    }
        //}

        //private void itm1_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (sender == itm1)
        //    {
        //        if (itm1.IsChecked)
        //        {
        //            NumOnce = 1;
        //            itm2.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm4.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm2)
        //    {
        //        if (itm2.IsChecked)
        //        {
        //            NumOnce = 2;
        //            itm1.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm4.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm3)
        //    {
        //        if (itm3.IsChecked)
        //        {
        //            NumOnce = 3;
        //            itm1.IsChecked = false;
        //            itm2.IsChecked = false;
        //            itm4.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm4)
        //    {
        //        if (itm4.IsChecked)
        //        {
        //            NumOnce = 4;
        //            itm1.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm2.IsChecked = false;
        //            itm5.IsChecked = false;
        //        }
        //    }
        //    else if (sender == itm5)
        //    {
        //        if (itm5.IsChecked)
        //        {
        //            NumOnce = 5;
        //            itm1.IsChecked = false;
        //            itm2.IsChecked = false;
        //            itm3.IsChecked = false;
        //            itm4.IsChecked = false;
        //        }
        //    }
        //    SetNum(NumOnce);
        //}
        //#endregion
        #endregion

        /// <summary>
        /// 右键菜单即将打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (dlList.SelectedItems == null || dlList.SelectedItems.Count == 0)
            {
                itmLst.IsEnabled =
                    itmCopy.IsEnabled =
                    itmRetry.IsEnabled =
                    itmStop.IsEnabled =
                    itmDelete.IsEnabled =
                    itmDeleteFile.IsEnabled = false;
            }
            else
            {
                itmCopy.IsEnabled = dlList.SelectedItems.Count == 1;
                itmLst.IsEnabled =
                    itmRetry.IsEnabled =
                    itmStop.IsEnabled =
                    itmDelete.IsEnabled =
                    itmDeleteFile.IsEnabled = true;
            }

            itmRetryAll.IsEnabled =
                itmStopAll.IsEnabled =
                itmDeleteAll.IsEnabled = dlList.Items.Count > 0;
        }

        /// <summary>
        /// 文件拖拽事件
        /// </summary>
        public void UserControl_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                string fileName = ((string[])(e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop)))[0];
                if (fileName != null && Path.GetExtension(fileName).ToLower() == ".lst")
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else e.Effects = DragDropEffects.None;
            }
            catch (Exception) { e.Effects = DragDropEffects.None; }
        }

        /// <summary>
        /// 从lst文件添加下载
        /// </summary>
        /// <param name="fileName"></param>
        public void DownLoadFromFile(string fileName)
        {
            if (fileName != null && Path.GetExtension(fileName).ToLower() == ".lst")
            {
                List<string> lines = new List<string>(File.ReadAllLines(fileName));
                List<MiniDownloadItem> items = new List<MiniDownloadItem>();
                MiniDownloadItem di = new MiniDownloadItem();
                //提取地址
                foreach (string line in lines)
                {
                    //移除空行
                    if (line.Trim().Length == 0) continue;
                    string[] parts = line.Split('|');

                    //url
                    if (parts.Length > 0 && parts[0].Trim().Length < 1)
                        continue;
                    else
                        di.url = parts[0];

                    //文件名
                    if (parts.Length > 1 && parts[1].Trim().Length > 0)
                    {
                        string ext = di.url.Substring(di.url.LastIndexOf('.'), di.url.Length - di.url.LastIndexOf('.'));
                        di.fileName = parts[1].EndsWith(ext) ? parts[1] : parts[1] + ext;
                    }

                    //域名
                    if (parts.Length > 2 && parts[2].Trim().Length > 0)
                        di.host = parts[2];

                    //上传者
                    if (parts.Length > 3 && parts[3].Trim().Length > 0)
                        di.author = parts[3];

                    //ID
                    if (parts.Length > 4 && parts[4].Trim().Length > 0)
                    {
                        try
                        {
                            di.id = int.Parse(parts[4]);
                        }
                        catch { }
                    }

                    //免文件校验
                    if (parts.Length > 5 && parts[5].Trim().Length > 0)
                        di.noVerify = parts[5].Contains('v');


                    items.Add(di);
                }

                //添加至下载列表
                AddDownload(items);
            }
        }

        /// <summary>
        /// 文件被拖入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserControl_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string fileName = ((string[])(e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop)))[0];
                DownLoadFromFile(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("从文件添加下载失败\r\n" + ex.Message, MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 打开保存目录
        /// </summary>
        private void itmOpenSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DownloadItem dlItem = (DownloadItem)dlList.SelectedItem;

                if (File.Exists(dlItem.LocalName))
                {
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("explorer.exe");
                    psi.Arguments = "/e,/select," + dlItem.LocalName;
                    System.Diagnostics.Process.Start(psi);
                }
                else
                {
                    System.Diagnostics.Process.Start(GetLocalPath(dlItem));
                }

            }
            catch { }
        }

        /// <summary>
        /// 双击打开文件
        /// </summary>
        private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    DownloadItem dlItem = (DownloadItem)dlList.SelectedItem;
                    if (dlItem.StatusE == DLStatus.Success)
                    {
                        if (!File.Exists(dlItem.LocalName))
                        {
                            MessageBox.Show("无法打开文件！ 可能已被更名、删除或移动。", MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        System.Diagnostics.Process.Start(dlItem.LocalName);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 清除所有失败任务
        /// </summary>
        private void itmClearDled_Click_1(object sender, RoutedEventArgs e)
        {
            int i = 0;
            while (true)
            {
                if (i >= downloadItems.Count) break;
                DownloadItem item = downloadItems[i];
                if (item.StatusE == DLStatus.Failed)
                {
                    downloadItems.RemoveAt(i);
                    downloadItemsDic.Remove(item.Url);
                }
                else
                {
                    i++;
                }
            }
            RefreshStatus();
        }

        /// <summary>
        /// 仅清除已取消和已存在的任务
        /// </summary>
        private void itmClearDled_Click_2(object sender, RoutedEventArgs e)
        {
            int i = 0;
            while (true)
            {
                if (i >= downloadItems.Count) break;
                DownloadItem item = downloadItems[i];

                if (item.StatusE == DLStatus.Cancel || item.StatusE == DLStatus.IsHave)
                {
                    downloadItems.RemoveAt(i);
                    downloadItemsDic.Remove(item.Url);
                }
                else
                {
                    i++;
                }
            }
            RefreshStatus();
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void itmSelAll_Click(object sender, RoutedEventArgs e)
        {
            dlList.SelectAll();
        }

        /// <summary>
        /// 重试所有任务
        /// </summary>
        private void itmRetryAll_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDownloadListTask(DLWorkMode.RetryAll);
        }

        /// <summary>
        /// 停止所有任务
        /// </summary>
        private void itmStopAll_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDownloadListTask(DLWorkMode.StopAll);
        }

        /// <summary>
        /// 移除所有任务
        /// </summary>
        private void itmDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDownloadListTask(DLWorkMode.RemoveAll);
        }

        /// <summary>
        /// 任务和文件一起删除
        /// </summary>
        private void itmDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDownloadListTask(DLWorkMode.Del);
        }

        /// <summary>
        /// 反选.......我能怎么办?我也很复杂啊...
        /// </summary>
        private void itmSelInvert_Click(object sender, RoutedEventArgs e)
        {
            //表项总数
            int listcount = downloadItems.Count;
            //选中项的url
            List<string> selurl = new List<string>();

            if (listcount < 1)
            {
                dlList.UnselectAll();
                return;
            }

            if (dlList.SelectedItems.Count < 1)
            {
                itmSelAll_Click(null, null);
                return;
            }

            foreach (DownloadItem sitem in dlList.SelectedItems)
            {
                selurl.Add(sitem.Url);
            }

            //设置选中
            dlList.UnselectAll();
            DownloadItem item = null;
            int selcount = selurl.Count;

            for (int i = 0; i < listcount; i++)
            {
                int ii = 0;
                item = downloadItems[i];

                //遍历是否之前选中
                foreach (string surl in selurl)
                {
                    ii++;
                    if (item.Url.Contains(surl))
                    {
                        break;
                    }
                    else if (ii == selcount)
                    {
                        dlList.SelectedItems.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// 双击一个表项执行的操作
        /// </summary>
        private void dlList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dlList.SelectedItems.Count == 1)
            {
                DownloadItem dcitem = (DownloadItem)dlList.SelectedItem;
                switch (dcitem.StatusE)
                {
                    case DLStatus.Success:
                    case DLStatus.IsHave:
                        if (File.Exists(dcitem.LocalName))
                            System.Diagnostics.Process.Start(dcitem.LocalName);
                        break;
                    case DLStatus.Cancel:
                    case DLStatus.Failed:
                        ExecuteDownloadListTask(DLWorkMode.Retry);
                        break;
                    default:
                        ExecuteDownloadListTask(DLWorkMode.Stop);
                        break;
                }
            }
        }

        /// <summary>
        /// 当做下载列表快捷键
        /// </summary>
        private void dlList_KeyDown(object sender, KeyEventArgs e)
        {
            if (MainWindow.IsCtrlDown())
            {
                int dlselect = dlList.SelectedItems.Count;

                if (e.Key == Key.U)
                {   //反选
                    itmSelInvert_Click(null, null);
                }
                else if (dlselect > 0)
                {
                    if (e.Key == Key.L)
                    {//导出下载列表
                        itmLst_Click(null, null);
                    }
                    else if (e.Key == Key.C && dlselect == 1)
                    {   //复制地址
                        itmCopy_Click(null, null);
                    }
                    else if (e.Key == Key.R)
                    {    //重试
                        itmRetry_Click(null, null);
                    }
                    else if (e.Key == Key.S)
                    {    //停止
                        itmStop_Click(null, null);
                    }
                    else if (e.Key == Key.D)
                    {    //移除
                        itmDelete_Click(null, null);
                    }
                    else if (e.Key == Key.X)
                    {    //和文件一起删除
                        itmDeleteFile_Click(null, null);
                    }
                }
                if (e.Key == Key.G)
                {   //停止所有任务
                    itmStopAll_Click(null, null);
                }
                else if (e.Key == Key.V)
                {   //清空所有任务
                    itmDeleteAll_Click(null, null);
                }
                else if (e.Key == Key.T)
                {   //重试所有任务
                    itmRetryAll_Click(null, null);
                }
            }
        }
    }
}
