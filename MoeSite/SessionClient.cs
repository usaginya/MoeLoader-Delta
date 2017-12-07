using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Web;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 帶session的HttpWeb連接
    /// 2017-1-6 by YIU
    /// </summary>
    public class SessionClient
    {
        private static CookieContainer m_Cookie = new CookieContainer();
        private static string defUA = "Googlebot-Image/1.0";

        /// <summary>
        /// Cookie集合
        /// </summary>
        public CookieContainer CookieContainer
        {
            get { return m_Cookie; }
            set { m_Cookie = value; }
        }

 
        //#############################   GET   #################################################
        /// <summary>
        /// Get訪問
        /// </summary>
        /// <param name="url">網址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">編碼</param>
        /// <returns>網頁內容</returns>
        public string Get(string url, IWebProxy proxy, Encoding pageEncoding)
        {
            return Get(url, proxy, pageEncoding, defUA);
        }

        /// <summary>
        /// Get訪問，自訂UA
        /// </summary>
        /// <param name="url">網址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">編碼</param>
        /// <param name="UA">User-Agent</param>
        /// <returns>網頁內容</returns>
        public string Get(string url, IWebProxy proxy, Encoding pageEncoding, string UA)
        {
            string ret = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse reponse = null;
            try
            {
                request.UserAgent = UA;
                request.ContentType = MimeMapping.GetMimeMapping(url);
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
        /// Get Response 取迴響應, Please use Close()
        /// </summary>
        /// <param name="url">網址</param>
        /// <param name="proxy">代理</param>
        /// <param name="rwtimeout">讀寫流超時ReadWriteTimeout</param>
        /// <param name="timeout">超時時間</param>
        /// <param name="referer">來源</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, int rwtimeout, int timeout, string referer)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse reponse = null;
            try
            {
                request.UserAgent = defUA;
                request.ContentType = MimeMapping.GetMimeMapping(url);
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
        /// Get Response 取迴響應 Timeout 20s, Please use Close()
        /// </summary>
        /// <param name="url">網址</param>
        /// <param name="proxy">代理</param>
        /// <param name="referer">來源</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, string referer)
        {
            return GetWebResponse(url, proxy, 20000, 20000, referer);
        }

        //########################################################################################

        //#############################   POST   #################################################
        /// <summary>
        /// Post訪問
        /// </summary>
        /// <param name="url">網址</param>
        /// <param name="postData">Post資料</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">編碼</param>
        /// <returns>網頁內容</returns>
        public string Post(string url, string postData, IWebProxy proxy, Encoding pageEncoding)
        {
            return Post(url, postData, proxy, pageEncoding, defUA);
        }

        /// <summary>
        /// Post訪問，自訂UA
        /// </summary>
        /// <param name="url">網址</param>
        /// <param name="postData">Post資料</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">編碼</param>
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
                request.ContentType = MimeMapping.GetMimeMapping(url);
                request.Proxy = proxy;
                request.Method = "POST";
                request.KeepAlive = true;
                request.CookieContainer = m_Cookie;//設定上次訪問頁面的Cookie 保持Session
                request.ContentLength = bytesToPost.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytesToPost, 0, bytesToPost.Length);//寫入Post資料
                requestStream.Close();

                response = (HttpWebResponse)request.GetResponse();
                m_Cookie = request.CookieContainer;//訪問後更新Cookie
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
        //#############################   Cookies   #################################################
        /// <summary>
        /// 取CookieContainer中所有站點Cookies
        /// </summary>
        /// <returns>全部Cookie值</returns>
        public string GetAllCookies()
        {
            return _GetCookieValue(m_Cookie);
        }

        /// <summary>
        /// 取CookieContainer中所有站點Cookies 自定CookieContainer
        /// </summary>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        public string GetAllCookies(CookieContainer cc)
        {
            return _GetCookieValue(cc);
        }

        /// <summary>
        /// 取CookieContainer中指定站點Cookies
        /// </summary>
        /// <param name="url">域名</param>
        /// <returns></returns>
        public string GetURLCookies(string url)
        {
            return _GetUrlCookies(url, m_Cookie);
        }

        /// <summary>
        /// 取CookieContainer中指定站點Cookies 自定CookieContainer
        /// </summary>
        /// <param name="url">域名</param>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        public string GetURLCookies(string url, CookieContainer cc)
        {
            return _GetUrlCookies(url, cc);
        }

        /// <summary>
        /// 取Cookie中鍵的值 當前訪問的網站
        /// </summary>
        /// <param name="CookieKey">Cookie鍵</param>
        /// <returns>Cookie鍵對應值</returns>
        public string GetCookieValue(string CookieKey)
        {
            return _GetCookieValue(CookieKey, m_Cookie, 1);
        }

        /// <summary>
        /// 取Cookie中鍵的值 自定CookieContainer
        /// </summary>
        /// <param name="CookieKey">Cookie鍵</param>
        /// <param name="cc">Cookie集合對象</param>
        /// <returns>Cookie鍵對應值</returns>
        public string GetCookieValue(string CookieKey, CookieContainer cc)
        {
            return _GetCookieValue(CookieKey, cc, 1);
        }

        /// <summary>
        /// 私有處理Cookie集合的方法 預設取全部Cookie值
        /// </summary>
        /// <param name="cc">Cookie集合對象</param>
        /// <returns></returns>
        private static string _GetCookieValue(CookieContainer cc)
        {
            return _GetCookieValue("", cc, 0);
        }

        /// <summary>
        /// 私有處理Cookie集合的方法
        /// </summary>
        /// <param name="CookieKey">Cookie鍵</param>
        /// <param name="cc">Cookie集合對象</param>
        /// <param name="mode">處理方式 0取所有站點全部值 1取指定鍵的值</param>
        /// <returns>Cookie對應值</returns>
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
                    foreach (CookieCollection colCookies in lstCookieCol.Values)
                        foreach (Cookie c1 in colCookies) lstCookies.Add(c1);
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
