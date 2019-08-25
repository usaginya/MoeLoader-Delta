/*
 * version 1.91
 * by YIU
 * Create               20170106
 * Last Change     20190824
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;

namespace MoeLoaderDelta
{
    /// <summary>
    /// session方式的HttpWeb连接
    /// </summary>
    public class SessionClient
    {
        private static CookieContainer m_Cookie = new CookieContainer();

        private static string[] UAs = new string[]
        {
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.21 (KHTML, like Gecko) Chrome/53.0.1271.64 Safari/537.21",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.117 Safari/537.36",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0_2 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Mobile/15A421",
            };

        /// <summary>
        /// 提供UA
        /// </summary>
        public static string DefUA { get; } = UAs[new Random().Next(0, UAs.Length - 1)];

        /// <summary>
        /// Cookie集合
        /// </summary>
        public CookieContainer CookieContainer
        {
            get => m_Cookie ?? new CookieContainer();
            set => m_Cookie = value;
        }

        public SessionClient()
        {
            ServicePointManager.DefaultConnectionLimit = 768;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }
        //#############################   Header   #################################################
        private HttpWebRequest SetHeader(HttpWebRequest request, string url, IWebProxy proxy, SessionHeadersCollection shc)
        {
            request.Headers = shc;
            request.Proxy = proxy;
            request.Accept = shc.Accept;
            request.Referer = shc.Referer;
            request.Timeout = shc.Timeout;
            request.KeepAlive = shc.KeepAlive;
            request.UserAgent = shc.UserAgent;
            request.CookieContainer = string.IsNullOrWhiteSpace(shc.Get("Cookie")) ? CookieContainer : request.CookieContainer;
            request.AllowAutoRedirect = shc.AllowAutoRedirect;
            request.AutomaticDecompression = shc.AutomaticDecompression;
            request.ContentType = shc.ContentType.Contains("auto") ? MimeMapping.GetMimeMapping(url) : shc.ContentType;
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = int.MaxValue;

            return request;
        }
        //##############################################################################
        //#############################   GET   #################################################
        /// <summary>
        /// Get访问,便捷
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, string pageEncoding)
        {
            return Get(url, proxy, pageEncoding, new SessionHeadersCollection());
        }

        /// <summary>
        /// Get访问,便捷,默认UTF-8编码
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy)
        {
            return Get(url, proxy, "UTF-8", new SessionHeadersCollection());
        }

        /// <summary>
        /// Get访问,便捷,自定义UA
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="UA">user-agent</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, string pageEncoding, string UA)
        {
            SessionHeadersCollection shc = new SessionHeadersCollection();
            shc.UserAgent = UA;
            return Get(url, proxy, pageEncoding, shc);
        }

        /// <summary>
        /// Get访问
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="shc">Headers</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, string pageEncoding, SessionHeadersCollection shc)
        {
            string ret = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse reponse = null;
            try
            {
                SetHeader(request, url, proxy, shc);

                reponse = (HttpWebResponse)request.GetResponse();
                m_Cookie = request.CookieContainer;
                Stream rspStream = reponse.GetResponseStream();
                StreamReader sr = new StreamReader(rspStream, Encoding.GetEncoding(pageEncoding));
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
                if (request != null)
                {
                    request.Abort();
                    request = null;
                }
            }
            return ret;
        }

        /// <summary>
        /// Get访问,默认UTF-8编码
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="shc">Headers</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, SessionHeadersCollection shc)
        {
            return Get(url, proxy, "UTF-8", shc);
        }

        /// <summary>
        /// Get Response 取回响应, Please use Close()
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="rwtimeout">读写流超时ReadWriteTimeout</param>
        /// <param name="shc">Headers</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, int rwtimeout, SessionHeadersCollection shc)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse reponse = null;
            try
            {
                SetHeader(request, url, proxy, shc);

                request.CookieContainer = m_Cookie;
                request.ReadWriteTimeout = rwtimeout;
                reponse = request.GetResponse();
                m_Cookie = request.CookieContainer;
            }
            catch { }
            return reponse;
        }
        /// <summary>
        /// Get Response 取回响应 Timeout 20s, Please use Close()
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="referer">来源</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, string referer)
        {
            SessionHeadersCollection shc = new SessionHeadersCollection();
            shc.Referer = referer;
            shc.Timeout = 20000;
            shc.ContentType = SessionHeadersValue.ContentTypeAuto;
            return GetWebResponse(url, proxy, shc.Timeout, shc);
        }

        /// <summary>
        /// Create HttpWebRequest
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="shc">Headers</param>
        /// <returns>HttpWebRequest</returns>
        public HttpWebRequest CreateWebRequest(string url, IWebProxy proxy, SessionHeadersCollection shc)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                SetHeader(request, url, proxy, shc);

                request.CookieContainer = m_Cookie;
                request.ReadWriteTimeout = 20000;
            }
            catch { }
            return request;
        }

        //########################################################################################

        //#############################   POST   #################################################
        /// <summary>
        /// Post访问,便捷
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        public string Post(string url, string postData, IWebProxy proxy, string pageEncoding)
        {
            WebHeaderCollection whc = null;
            return Post(url, postData, proxy, pageEncoding, new SessionHeadersCollection(), ref whc);
        }

        /// <summary>
        /// Post访问,自定义UA
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="UA">User-Agent</param>
        public string Post(string url, string postData, IWebProxy proxy, string pageEncoding, string UA)
        {
            WebHeaderCollection whc = null;
            SessionHeadersCollection shc = new SessionHeadersCollection();
            shc.UserAgent = UA;
            return Post(url, postData, proxy, pageEncoding, shc, ref whc);
        }

        /// <summary>
        /// Post访问,默认UTF-8编码
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="shc">Headers</param>
        public string Post(string url, string postData, IWebProxy proxy, SessionHeadersCollection shc)
        {
            WebHeaderCollection whc = null;
            return Post(url, postData, proxy, "UTF-8", shc, ref whc);
        }

        /// <summary>
        /// Post访问
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="shc">Headers</param>
        /// <returns></returns>
        public string Post(string url, string postData, IWebProxy proxy, string pageEncoding, SessionHeadersCollection shc, ref WebHeaderCollection responeHeaders)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            HttpWebResponse response = null;

            byte[] bytesToPost = Encoding.GetEncoding(pageEncoding).GetBytes(postData);
            try
            {
                SetHeader(request, url, proxy, shc);

                request.Method = "POST";
                request.CookieContainer = m_Cookie;//设置上次访问页面的Cookie 保持Session
                request.ContentLength = bytesToPost.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytesToPost, 0, bytesToPost.Length);//写入Post数据
                requestStream.Close();

                response = (HttpWebResponse)request.GetResponse();
                m_Cookie = request.CookieContainer;//访问后更新Cookie
                responeHeaders = request.Headers;
                Stream responseStream = response.GetResponseStream();
                string resData = string.Empty;

                using (StreamReader resSR = new StreamReader(responseStream, Encoding.GetEncoding(pageEncoding)))
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
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                    request = null;
                }
            }
        }

        //########################################################################################
        //#############################   HEAD   #################################################
        public bool IsExist(string uri, IWebProxy proxy, SessionHeadersCollection shc)
        {
            HttpWebRequest req = null;
            HttpWebResponse res = null;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(uri);
                req.Method = "HEAD";

                SetHeader(req, uri, proxy, shc);

                res = (HttpWebResponse)req.GetResponse();

                return (res.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (res != null)
                {
                    res.Close();
                    res = null;
                }
                if (req != null)
                {
                    req.Abort();
                    req = null;
                }
            }
        }
        //########################################################################################
        //#############################   Cookies   #################################################
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
        /// 取此CookieContainer中指定站点Cookies
        /// </summary>
        /// <param name="url">域名</param>
        /// <returns></returns>
        public string GetURLCookies(string url)
        {
            return m_Cookie?.GetCookieHeader(new Uri(url));
        }

        /// <summary>
        /// 取CookieContainer中指定站点Cookies 自定CookieContainer
        /// </summary>
        /// <param name="url">域名</param>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        public string GetURLCookies(string url, CookieContainer cc)
        {
            return cc?.GetCookieHeader(new Uri(url));
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
        /// 写出指定CookieContainer到文件
        /// </summary>
        /// <param name="file">文件保存路径</param>
        /// <param name="cc">CookieContainer</param>
        public static void WriteCookiesToFile(string file, CookieContainer cc)
        {
            using (Stream stream = File.Create(file))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cc);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
        }
        /// <summary>
        /// 写出当前CookieContainer到文件
        /// </summary>
        /// <param name="file">文件保存路径</param>
        public static void WriteCookiesToFile(string file)
        {
            WriteCookiesToFile(file, m_Cookie);
        }

        /// <summary>
        /// 从文件读入Cookies
        /// </summary>
        /// <param name="file">Cookies文件</param>
        public static void ReadCookiesFromFile(string file)
        {
            try
            {
                using (Stream stream = File.Open(file, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    m_Cookie = (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
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
                    foreach (CookieCollection colCookies in lstCookieCol.Values)
                        foreach (Cookie c1 in colCookies) lstCookies.Add(c1);
                }

                string ret = "", uri = "";
                switch (mode)
                {
                    default:
                        foreach (Cookie cookie in lstCookies)
                        {
                            if (uri != cookie.Domain)
                            {
                                uri = cookie.Domain;
                                ret += string.IsNullOrWhiteSpace(ret) ? "" : "$";
                                ret += uri + ";";
                            }

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

    }

    //########################################################################################
    //#############################   Class   #################################################
    /// <summary>
    /// Provide some header value
    /// </summary>
    public static class SessionHeadersValue
    {
        /// <summary>
        /// text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
        /// </summary>
        public static string AcceptDefault = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";

        /// <summary>
        /// text/html
        /// </summary>
        public static string AcceptTextHtml = "text/html";

        /// <summary>
        /// text/xml
        /// </summary>
        public static string AcceptTextXml = "text/xml";

        /// <summary>
        /// application/json
        /// </summary>
        public static string AcceptAppJson = "application/json";

        /// <summary>
        /// application/xml
        /// </summary>
        public static string AcceptAppXml = "application/xml";

        /// <summary>
        /// gzip, deflate
        /// </summary>
        public static string AcceptEncodingGzip = "gzip, deflate";

        /// <summary>
        /// Automatic recognition
        /// </summary>
        public static string ContentTypeAuto = "auto";

        /// <summary>
        /// application/x-www-form-urlencoded
        /// </summary>
        public static string ContentTypeFormUrlencoded = "application/x-www-form-urlencoded";

        /// <summary>
        /// multipart/form-data
        /// </summary>
        public static string ContentTypeFormData = "multipart/form-data";
    }

    /// <summary>
    ///  The Ready HeaderCollection Class, 可以直接设置一些常用的Header值
    /// </summary>
    public class SessionHeadersCollection : WebHeaderCollection
    {
        public SessionHeadersCollection()
        {
            Accept = SessionHeadersValue.AcceptDefault;
            AcceptEncoding = null;
            AcceptLanguage = "zh-CN,zh,zh-TW;q=0.7,en,*;q=0.5";
            AllowAutoRedirect = true;
            AutomaticDecompression = DecompressionMethods.None;
            ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;
            KeepAlive = false;
            Referer = null;
            Timeout = 9000;
            UserAgent = SessionClient.DefUA;
        }

        /// <summary>
        /// SessionHeadersValue.AcceptDefault
        /// </summary>
        public string Accept { get; set; }

        /// <summary>
        /// Null
        /// </summary>
        public string AcceptEncoding
        {
            get { return Get("Accept-Encoding"); }
            set { Set(HttpRequestHeader.AcceptEncoding, value); }
        }

        /// <summary>
        /// zh-CN,zh,zh-TW;q=0.7,en,*;q=0.5
        /// </summary>
        public string AcceptLanguage
        {
            get { return Get("Accept-Language"); }
            set { Set(HttpRequestHeader.AcceptLanguage, value); }
        }

        /// <summary>
        /// True 跟随重定向
        /// </summary>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>
        /// None 压缩类型
        /// </summary>
        public DecompressionMethods AutomaticDecompression { get; set; }

        /// <summary>
        /// x-www-form-urlencoded, Use SessionHeadersValue class
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        ///  False
        /// </summary>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// 引用页
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        /// 9000
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// UA
        /// </summary>
        public string UserAgent { get; set; }
    }
}
