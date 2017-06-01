using System;
using System.Net;

namespace MoeLoader
{
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
            Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
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
    }
}
