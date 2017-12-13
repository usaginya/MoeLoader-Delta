using System;
using System.Net;

namespace MoeLoaderDelta
{
    /*
    //public class Calculagraph
    //{
    //    /// <summary>
    //    /// 時間到事件
    //    /// </summary>
    //    public event TimeoutCaller TimeOver;

    //    /// <summary>
    //    /// 開始時間
    //    /// </summary>
    //    private DateTime _startTime;
    //    private TimeSpan _timeout = new TimeSpan(0, 0, 10);
    //    private bool _hasStarted = false;
    //    object _userdata;

    //    /// <summary>
    //    /// 計時器構造方法
    //    /// </summary>
    //    /// <param name="userdata">計時結束時回調的使用者資料</param>
    //    public Calculagraph(object userdata)
    //    {
    //        TimeOver += new TimeoutCaller(OnTimeOver);
    //        _userdata = userdata;
    //    }

    //    /// <summary>
    //    /// 超時退出
    //    /// </summary>
    //    /// <param name="userdata"></param>
    //    public virtual void OnTimeOver(object userdata)
    //    {
    //        Stop();
    //    }

    //    /// <summary>
    //    /// 過期時間(秒)
    //    /// </summary>
    //    public int Timeout
    //    {
    //        get
    //        {
    //            return _timeout.Seconds;
    //        }
    //        set
    //        {
    //            if (value <= 0)
    //                return;
    //            _timeout = new TimeSpan(0, 0, value);
    //        }
    //    }

    //    /// <summary>
    //    /// 是否已經開始計時
    //    /// </summary>
    //    public bool HasStarted
    //    {
    //        get
    //        {
    //            return _hasStarted;
    //        }
    //    }

    //    /// <summary>
    //    /// 開始計時
    //    /// </summary>
    //    public void Start()
    //    {
    //        Reset();
    //        _hasStarted = true;
    //        Thread th = new Thread(WaitCall);
    //        th.IsBackground = true;
    //        th.Start();
    //    }

    //    /// <summary>
    //    /// 重設
    //    /// </summary>
    //    public void Reset()
    //    {
    //        _startTime = DateTime.Now;
    //    }

    //    /// <summary>
    //    /// 停止計時
    //    /// </summary>
    //    public void Stop()
    //    {
    //        _hasStarted = false;
    //    }

    //    /// <summary>
    //    /// 檢查是否過期
    //    /// </summary>
    //    /// <returns></returns>
    //    private bool checkTimeout()
    //    {
    //        return (DateTime.Now - _startTime).Seconds >= Timeout;
    //    }

    //    private void WaitCall()
    //    {
    //        try
    //        {
    //            //循環檢測是否過期
    //            while (_hasStarted && !checkTimeout())
    //            {
    //                Thread.Sleep(1000);
    //            }
    //            if (TimeOver != null)
    //                TimeOver(_userdata);
    //        }
    //        catch (Exception)
    //        {
    //            Stop();
    //        }
    //    }
    //}

    /// <summary>
    /// 過期時回調委託
    /// </summary>
    /// <param name="userdata"></param>
    //public delegate void TimeoutCaller(object userdata);
    */

    /// <summary>
    /// 帶有超時的WebClient
    /// </summary>
    public class MyWebClient : WebClient
    {
        //private Calculagraph _timer;
        private int _timeOut = 25;

        /// <summary>
        /// 構造WebClient
        /// </summary>
        public MyWebClient()
        {
            Headers["User-Agent"] = SessionClient.DefUA;
            ServicePointManager.DefaultConnectionLimit = 30;
        }

        /// <summary>
        /// 過期時間 in second
        /// </summary>
        public int Timeout
        {
            get
            {
                return _timeOut;
            }
            set
            {
                if (value < 1)
                    _timeOut = 10;
                _timeOut = value;
            }
        }

        /// <summary>
        /// 重寫GetWebRequest，添加WebRequest對象超時時間
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 1000 * Timeout;
            request.ReadWriteTimeout = 1000 * Timeout;
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = int.MaxValue;
            return request;
        }

        ///// <summary>
        ///// 帶過期計時的下載
        ///// </summary>
        //public void DownloadFileAsyncWithTimeout(Uri address, string fileName, object userToken)
        //{
        //    if (_timer == null)
        //    {
        //        _timer = new Calculagraph(this);
        //        _timer.Timeout = Timeout;
        //        _timer.TimeOver += new TimeoutCaller(_timer_TimeOver);
        //        this.DownloadProgressChanged += new DownloadProgressChangedEventHandler(CNNWebClient_DownloadProgressChanged);
        //    }

        //    DownloadFileAsync(address, fileName, userToken);
        //    _timer.Start();
        //}

        ///// <summary>
        ///// WebClient下載過程事件，接收到資料時引發
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void CNNWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        //{
        //    _timer.Reset();//重設計時器
        //}

        ///// <summary>
        ///// 計時器過期
        ///// </summary>
        ///// <param name="userdata"></param>
        //void _timer_TimeOver(object userdata)
        //{
        //    this.CancelAsync();//取消下載
        //}
    }
}
