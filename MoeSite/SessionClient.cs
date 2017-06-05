using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 带session的HttpWeb连接
    /// 2017-1-6 by YIU
    /// </summary>
    public class SessionClient
    {
        private static CookieContainer m_Cookie = new CookieContainer();
        private static string defUA = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";

        /// <summary>
        /// Cookie集合
        /// </summary>
        public CookieContainer CookieContainer
        {
            get { return m_Cookie; }
            set { m_Cookie = value; }
        }

        /*
        /// <summary>
        /// 清空已连接的Session 同时也是清空当前访问网站的Cookies
        /// </summary>
        public void ClearSession()
        {
            this.CookieContainer = m_Cookie = new CookieContainer();
        }
        */

        //#############################   GET   #################################################
        /// <summary>
        /// Get访问
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, Encoding pageEncoding)
        {
            return Get(url, proxy, pageEncoding, defUA);
        }

        /// <summary>
        /// Get访问，自定义UA
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="UA">User-Agent</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, Encoding pageEncoding, string UA)
        {
            string ret = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse reponse = null;
            try
            {
                request.UserAgent = UA;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Proxy = proxy;
                request.CookieContainer = m_Cookie;
                reponse = (HttpWebResponse)request.GetResponse();
                m_Cookie = request.CookieContainer;
                Stream rspStream = reponse.GetResponseStream();
                StreamReader sr = new StreamReader(rspStream, pageEncoding);
                ret = sr.ReadToEnd();
                sr.Close();
                rspStream.Close();
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            finally
            {
                if (reponse != null)
                {
                    reponse.Close();
                }
            }
            return ret;
        }

        /// <summary>
        /// Get Response 取回响应, Please use Close()
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="rwtimeout">读写流超时ReadWriteTimeout</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="referer">来源</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, int rwtimeout, int timeout, string referer)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse reponse = null;
            try
            {
                request.UserAgent = defUA;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Proxy = proxy;
                request.CookieContainer = m_Cookie;
                request.ReadWriteTimeout = rwtimeout;
                request.Timeout = timeout;
                request.Referer = referer;
                reponse = request.GetResponse();
                m_Cookie = request.CookieContainer;
            }
            catch { }
            return reponse;
        }
        /// <summary>
        /// Get Response 取回响应 Timeout 60000ms, Please use Close()
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="referer">来源</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, string referer)
        {
            return GetWebResponse(url, proxy, 60000, 60000, referer);
        }

        //########################################################################################

        //#############################   POST   #################################################
        /// <summary>
        /// Post访问
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <returns>网页内容</returns>
        public string Post(string url, string postData, IWebProxy proxy, Encoding pageEncoding)
        {
            return Post(url, postData, proxy, pageEncoding, defUA);
        }

        /// <summary>
        /// Post访问，自定义UA
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="UA">User-Agent</param>
        /// <returns></returns>
        public string Post(string url, string postData, IWebProxy proxy, Encoding pageEncoding, string UA)
        {
            HttpWebRequest request;
            HttpWebResponse response;

            byte[] bytesToPost = pageEncoding.GetBytes(postData);
            try
            {
                request = WebRequest.Create(url) as HttpWebRequest;
                request.UserAgent = UA;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Proxy = proxy;
                request.Method = "POST";
                request.KeepAlive = true;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = m_Cookie;//设置上次访问页面的Cookie 保持Session  
                request.ContentLength = bytesToPost.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytesToPost, 0, bytesToPost.Length);//写入Post数据  
                requestStream.Close();

                response = (HttpWebResponse)request.GetResponse();
                m_Cookie = request.CookieContainer;//访问后更新Cookie  
                Stream responseStream = response.GetResponseStream();
                string resData = "";

                using (StreamReader resSR = new StreamReader(responseStream, pageEncoding))
                {
                    resData = resSR.ReadToEnd();
                    resSR.Close();
                    responseStream.Close();
                }
                return resData;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        //########################################################################################

        /// <summary>
        /// 取CookieContainer中所有站点Cookies
        /// </summary>
        /// <returns>全部Cookie值</returns>
        public string GetAllCookies()
        {
            return _GetCookieValue(m_Cookie);
        }

        /// <summary>
        /// 取CookieContainer中所有站点Cookies 自定CookieContainer
        /// </summary>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        public string GetAllCookies(CookieContainer cc)
        {
            return _GetCookieValue(cc);
        }

        /// <summary>
        /// 取CookieContainer中指定站点Cookies
        /// </summary>
        /// <param name="url">域名</param>
        /// <returns></returns>
        public string GetURLCookies(string url)
        {
            return _GetUrlCookies(url, m_Cookie);
        }

        /// <summary>
        /// 取CookieContainer中指定站点Cookies 自定CookieContainer
        /// </summary>
        /// <param name="url">域名</param>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        public string GetURLCookies(string url, CookieContainer cc)
        {
            return _GetUrlCookies(url, cc);
        }

        /// <summary>
        /// 取Cookie中键的值 当前访问的网站
        /// </summary>
        /// <param name="CookieKey">Cookie键</param>
        /// <returns>Cookie键对应值</returns>
        public string GetCookieValue(string CookieKey)
        {
            return _GetCookieValue(CookieKey, m_Cookie, 1);
        }

        /// <summary>
        /// 取Cookie中键的值 自定CookieContainer
        /// </summary>
        /// <param name="CookieKey">Cookie键</param>
        /// <param name="cc">Cookie集合对象</param>
        /// <returns>Cookie键对应值</returns>
        public string GetCookieValue(string CookieKey, CookieContainer cc)
        {
            return _GetCookieValue(CookieKey, cc, 1);
        }

        /// <summary>
        /// 私有处理Cookie集合的方法 默认取全部Cookie值
        /// </summary>
        /// <param name="cc">Cookie集合对象</param>
        /// <returns></returns>
        private static string _GetCookieValue(CookieContainer cc)
        {
            return _GetCookieValue("", cc, 0);
        }

        /// <summary>
        /// 私有处理Cookie集合的方法
        /// </summary>
        /// <param name="CookieKey">Cookie键</param>
        /// <param name="cc">Cookie集合对象</param>
        /// <param name="mode">处理方式 0取所有站点全部值 1取指定键的值</param>
        /// <returns>Cookie对应值</returns>
        private static string _GetCookieValue(string CookieKey, CookieContainer cc, int mode)
        {
            try
            {
                List<Cookie> lstCookies = new List<Cookie>();
                System.Collections.Hashtable table = (System.Collections.Hashtable)cc.GetType().InvokeMember("m_domainTable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                    System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

                foreach (object pathList in table.Values)
                {
                    System.Collections.SortedList lstCookieCol = (System.Collections.SortedList)pathList.GetType().InvokeMember("m_list",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                        | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                    foreach (System.Net.CookieCollection colCookies in lstCookieCol.Values)
                        foreach (System.Net.Cookie c1 in colCookies) lstCookies.Add(c1);
                }

                string ret = "";
                switch (mode)
                {
                    default:
                        foreach (Cookie cookie in lstCookies)
                        {
                            ret += cookie.Name + "=" + cookie.Value + ";";
                        }
                        break;
                    case 1:
                        var model = lstCookies.Find(p => p.Name == CookieKey);
                        if (model != null)
                        {
                            ret = model.Value;
                        }
                        ret = string.Empty;
                        break;
                }
                return ret;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        /// <summary>
        /// 私有取指定URL的Cookies
        /// </summary>
        /// <param name="url">域名</param>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        private static string _GetUrlCookies(string url, CookieContainer cc)
        {
            CookieCollection cs = cc.GetCookies(new Uri(url));
            string ret = "";

            foreach (Cookie c in cs)
            {
                if (ret == "")
                {
                    ret += c.Name + "=" + c.Value;
                }
                else
                {
                    ret += ";" + c.Name + "=" + c.Value;
                }
            }

            return ret;
        }
    }
}
