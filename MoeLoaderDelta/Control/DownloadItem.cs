using System.ComponentModel;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 下載狀態
    /// </summary>
    public enum DLStatus { Success, Failed, Cancel, IsHave, DLing, Wait }

    /// <summary>
    /// 下載任務，用於介面綁定
    /// </summary>
    public class DownloadItem : INotifyPropertyChanged
    {
        private string size;
        private double progress;
        private DLStatus statusE;
        private double speed;

        public string FileName { get; set; }
        public string Host { get; set; }
        public string Author { get; set; }
        public string LocalName { get; set; }
        public string LocalFileName { get; set; }
        public int Id { get; set; }
        public bool NoVerify { get; set; }

        /// <summary>
        /// 大小
        /// </summary>
        public string Size
        {
            get { return size; }
            set
            {
                size = value;
                OnPropertyChanged("Size");
            }
        }

        /// <summary>
        /// 進度
        /// </summary>
        public double Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        /// <summary>
        /// 狀態（圖形表示）
        /// </summary>
        public string Status
        {
            get
            {
                switch (StatusE)
                {
                    case DLStatus.Wait:
                        return "/Images/wait.png";
                    case DLStatus.Success:
                        return "/Images/success.png";
                    case DLStatus.Cancel:
                        return "/Images/stop.png";
                    case DLStatus.IsHave:
                        return "/Images/goto.png";
                    case DLStatus.Failed:
                        return "/Images/failed.png";
                    case DLStatus.DLing:
                        return "/Images/dling.png";
                    default:
                        return "/Images/wait.png";
                }
            }
        }

        /// <summary>
        /// 狀態
        /// </summary>
        public DLStatus StatusE
        {
            get { return statusE; }
            set
            {
                statusE = value;
                OnPropertyChanged("Status");
                if (value != DLStatus.DLing)
                    SetSpeed(0.0);
            }
        }

        public string Url { get; set; }

        public string Speed
        {
            get
            {
                if (statusE == DLStatus.DLing)
                {
                    return (speed > 1024.0
                           ? (speed / 1024.0).ToString("0.00 MB")
                           : speed.ToString("0.00 KB")) + "/s";
                }
                else return "";
            }
        }

        public void SetSpeed(double sp)
        {
            speed = sp;
            OnPropertyChanged("Speed");
        }

        /// <summary>
        /// 下載對象
        /// </summary>
        /// <param name="fileName">下載時檔案名</param>
        /// <param name="url">下載連結</param>
        /// <param name="host">域名</param>
        /// <param name="author">上傳者</param>
        /// <param name="localName">本地路徑檔案名</param>
        /// <param name="localfileName">本地檔案名</param>
        /// <param name="id">作品ID</param>
        public DownloadItem(string fileName, string url, string host, string author, string localName, string localfileName, int id, bool? noVerify)
        {
            FileName = fileName;
            Size = "N/A";
            Progress = 0;
            StatusE = DLStatus.Wait;
            Url = url;
            Host = host != null ? host.Trim() != "" ? host : "unDomain" : "unDomain";
            Author = author != null ? author.Trim() != "" ? author : "unAuthor" : "unAuthor";
            LocalName = localName;
            LocalFileName = localfileName;
            Id = id;
            NoVerify = noVerify != null ? (bool)noVerify : false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
