/*
 * version 1.97
 * by YIU
 * Create               20170106
 * Last Change     20210203
 */

using Brotli;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.75 Safari/537.36",
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
            set => m_Cookie = value ?? new CookieContainer();
        }

        public SessionClient()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 768;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
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
            request.PreAuthenticate = !string.IsNullOrWhiteSpace(shc.Get("Authorization"));
            request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
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
        //###################### General ################################################
        /// <summary>
        /// 强制IPV4请求
        /// </summary>
        /// <param name="request">HttpWebRequest</param>
        private void ForceIpv4Request(HttpWebRequest request)
        {
            request.ServicePoint.BindIPEndPointDelegate = (servicePoint, remoteEndPoint, retryCount) =>
            {
                return remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? new IPEndPoint(IPAddress.Any, 0) : null;
            };
        }

        /// <summary>
        /// 提供错误的输出
        /// </summary>
        /// <param name="ex">错误信息</param>
        /// <param name="extra_info">附加错误信息</param>
        public static void EchoErrLog(WebException webExcp = null, string extra_info = null, string url = "")
        {
            int maxlog = 8192;
            bool exisnull = webExcp == null;
            string logPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\moesc_error.log";
            string wstr = string.Empty;
            if (!exisnull)
            {
                wstr = "[异常时间]: " + DateTime.Now.ToString() + "\r\n";
                wstr += "[异常信息]: " + extra_info + "\r\n";
                wstr += (string.IsNullOrWhiteSpace(extra_info) ? string.Empty : " | ") + webExcp.Message + "\r\n";
                wstr += "[请求域名]: " + new Uri(url).Host + "\r\n";
                wstr += "[调用堆栈]: " + webExcp.StackTrace.Trim() + "\r\n";
                wstr += "[触发方法]: " + webExcp.TargetSite + "\r\n";

                HttpWebResponse rsp = (HttpWebResponse)webExcp.Response;
                if (rsp != null)
                {
                    wstr += "[请求返回]: " + rsp.Server + " - " + rsp.StatusCode + " - " + rsp.StatusCode + "\r\n" + rsp.Headers;
                    rsp.Close();
                }

                wstr += $"[异常详细]:{Environment.NewLine}{webExcp.ToString()}{Environment.NewLine}";
            }
            File.AppendAllText(logPath, wstr + "\r\n");
            //压缩记录
            long sourceLength = new FileInfo(logPath).Length;
            if (sourceLength > maxlog)
            {
                byte[] buffer = new byte[maxlog];
                using (FileStream fs = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    int newleng = (int)sourceLength - maxlog;
                    newleng = newleng > maxlog ? maxlog : newleng;
                    fs.Seek(newleng, SeekOrigin.Begin);
                    fs.Read(buffer, 0, maxlog);
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.SetLength(0);
                    fs.Write(buffer, 0, maxlog);
                }
            }
        }
        //##############################################################################
        //######################   Encode Decode  ############################################
        /// <summary>
        /// 读取编码的Stream，返回字符串结果
        /// </summary>
        /// <param name="webResponse">HttpWebResponse</param>
        /// <param name="encoding">指定编码</param>
        /// <returns>字符串结果</returns>
        private string EncodeStreamReader(HttpWebResponse webResponse, Encoding encoding)
        {
            string strReader = string.Empty;
            string contentEncoding = webResponse.ContentEncoding.ToLower();

            using (Stream stream = webResponse.GetResponseStream())
            {
                if (contentEncoding.Contains("gzip"))
                {
                    using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new StreamReader(zipStream, encoding))
                        {
                            strReader = reader.ReadToEnd();
                        }
                    }
                }
                else if (contentEncoding.Contains("br"))
                {
                    using (BrotliStream bs = new BrotliStream(stream, CompressionMode.Decompress))
                    {
                        using (MemoryStream msOutput = new MemoryStream())
                        {
                            bs.CopyTo(msOutput);
                            msOutput.Seek(0, SeekOrigin.Begin);
                            using (StreamReader reader = new StreamReader(msOutput))
                            {
                                strReader = reader.ReadToEnd();
                            }
                        }
                    }
                }
                else
                {
                    using (StreamReader reader = new StreamReader(stream, encoding))
                    {
                        strReader = reader.ReadToEnd();
                    }
                }
            }
            return strReader;
        }

        /// <summary>
        /// 读取编码的Stream，返回字符串结果，Encoding.Default
        /// </summary>
        /// <param name="webResponse">HttpWebResponse</param>
        /// <returns>字符串结果</returns>
        private string EncodeStreamReader(HttpWebResponse webResponse, string contentEncoding)
        {
            return EncodeStreamReader(webResponse, Encoding.Default);
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
            SessionHeadersCollection shc = new SessionHeadersCollection
            {
                UserAgent = UA
            };
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
            try
            {
                GC.Collect(1, GCCollectionMode.Optimized);
                string ret = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = null;

                SetHeader(request, url, proxy, shc);
                ForceIpv4Request(request);

                response = (HttpWebResponse)request.GetResponse();
                //更新Cookie
                m_Cookie = request.CookieContainer ?? m_Cookie;
                string sc = response.Headers["Set-Cookie"];
                if (!string.IsNullOrWhiteSpace(sc))
                {
                    m_Cookie.Add(new Uri(url), CookiesHelper.GetCookiesByHeader(sc));
                }

                ret = EncodeStreamReader(response, Encoding.GetEncoding(pageEncoding));

                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                    request = null;
                }

                return ret;
            }
            catch (WebException webExcp)
            {
                EchoErrLog(webExcp, "SessionClient Get Error", url);
                return webExcp.Message;
            }
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
            GC.Collect(1, GCCollectionMode.Forced);
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
            SessionHeadersCollection shc = new SessionHeadersCollection
            {
                Referer = referer,
                Timeout = 20000,
                ContentType = SessionHeadersValue.ContentTypeAuto
            };
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
            SessionHeadersCollection shc = new SessionHeadersCollection
            {
                UserAgent = UA
            };
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
        public string Post(string url, string postData, IWebProxy proxy, string pageEncoding,
            SessionHeadersCollection shc, ref WebHeaderCollection responeHeaders)
        {
            GC.Collect(1, GCCollectionMode.Forced);
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            HttpWebResponse response = null;

            byte[] bytesToPost = Encoding.GetEncoding(pageEncoding).GetBytes(postData);
            try
            {
                SetHeader(request, url, proxy, shc);
                ForceIpv4Request(request);

                request.Method = "POST";
                request.CookieContainer = m_Cookie;//设置上次访问页面的Cookie 保持Session
                request.ContentLength = bytesToPost.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytesToPost, 0, bytesToPost.Length);//写入Post数据
                requestStream.Close();

                response = (HttpWebResponse)request.GetResponse();
                //更新Cookie
                m_Cookie = request.CookieContainer ?? m_Cookie;
                string sc = response.Headers["Set-Cookie"];
                if (!string.IsNullOrWhiteSpace(sc))
                {
                    m_Cookie.Add(new Uri(url), CookiesHelper.GetCookiesByHeader(sc));
                }
                responeHeaders = request.Headers;
                string resData = EncodeStreamReader(response, Encoding.GetEncoding(pageEncoding));
                return resData;
            }
            catch (WebException webExcp)
            {
                EchoErrLog(webExcp, "SessionClient Post Error", url);
                return webExcp.Message;
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
                ForceIpv4Request(req);

                res = (HttpWebResponse)req.GetResponse();

                return res.StatusCode == HttpStatusCode.OK;
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

                string ret = string.Empty, uri = string.Empty;
                switch (mode)
                {
                    default:
                        foreach (Cookie cookie in lstCookies)
                        {
                            if (uri != cookie.Domain)
                            {
                                uri = cookie.Domain;
                                ret += string.IsNullOrWhiteSpace(ret) ? string.Empty : "$";
                                ret += $"{uri};";
                            }

                            ret += $"{cookie.Name}={cookie.Value};";

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
        /// text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9
        /// </summary>
        public static string AcceptDefault = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";

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
        /// gzip, deflate, br
        /// </summary>
        public static string AcceptEncodingGzip = "gzip, deflate, br";

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
