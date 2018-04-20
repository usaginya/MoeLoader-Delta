using System;
using System.Net;

namespace MoeLoaderDelta
{
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
    }
}
