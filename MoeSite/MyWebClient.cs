using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Text;

namespace MoeLoaderDelta
{
    /*
    //public class Calculagraph
    //{
    //    /// <summary>
    //    /// 时间到事件
    //    /// </summary>
    //    public event TimeoutCaller TimeOver;

    //    /// <summary>
    //    /// 开始时间
    //    /// </summary>
    //    private DateTime _startTime;
    //    private TimeSpan _timeout = new TimeSpan(0, 0, 10);
    //    private bool _hasStarted = false;
    //    object _userdata;

    //    /// <summary>
    //    /// 计时器构造方法
    //    /// </summary>
    //    /// <param name="userdata">计时结束时回调的用户数据</param>
    //    public Calculagraph(object userdata)
    //    {
    //        TimeOver += new TimeoutCaller(OnTimeOver);
    //        _userdata = userdata;
    //    }

    //    /// <summary>
    //    /// 超时退出
    //    /// </summary>
    //    /// <param name="userdata"></param>
    //    public virtual void OnTimeOver(object userdata)
    //    {
    //        Stop();
    //    }

    //    /// <summary>
    //    /// 过期时间(秒)
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
    //    /// 是否已经开始计时
    //    /// </summary>
    //    public bool HasStarted
    //    {
    //        get
    //        {
    //            return _hasStarted;
    //        }
    //    }

    //    /// <summary>
    //    /// 开始计时
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
    //    /// 重置
    //    /// </summary>
    //    public void Reset()
    //    {
    //        _startTime = DateTime.Now;
    //    }

    //    /// <summary>
    //    /// 停止计时
    //    /// </summary>
    //    public void Stop()
    //    {
    //        _hasStarted = false;
    //    }

    //    /// <summary>
    //    /// 检查是否过期
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
    //            //循环检测是否过期
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
    /// 过期时回调委托
    /// </summary>
    /// <param name="userdata"></param>
    //public delegate void TimeoutCaller(object userdata);
    */

    /// <summary>
    /// 带有超时的WebClient
    /// </summary>
    public class MyWebClient : WebClient
    {
        //private Calculagraph _timer;
        private int _timeOut = 30;

        /// <summary>
        /// 构造WebClient
        /// </summary>
        public MyWebClient()
        {
            //最大连接数512
            ServicePointManager.DefaultConnectionLimit = 512;
            Headers["User-Agent"] = "Googlebot-Image/1.0";
        }

        /// <summary>
        /// 过期时间 in second
        /// </summary>
        public int Timeout
        {
            get
            {
                return _timeOut;
            }
            set
            {
                if (value <= 0)
                    _timeOut = 10;
                _timeOut = value;
            }
        }

        /// <summary>
        /// 重写GetWebRequest，添加WebRequest对象超时时间
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 1000 * Timeout;
            request.ReadWriteTimeout = 1000 * Timeout;
            return request;
        }

        ///// <summary>
        ///// 带过期计时的下载
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
        ///// WebClient下载过程事件，接收到数据时引发
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void CNNWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        //{
        //    _timer.Reset();//重置计时器
        //}

        ///// <summary>
        ///// 计时器过期
        ///// </summary>
        ///// <param name="userdata"></param>
        //void _timer_TimeOver(object userdata)
        //{
        //    this.CancelAsync();//取消下载
        //}
    }
}
