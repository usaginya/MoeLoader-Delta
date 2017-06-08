using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoeLoaderDelta;
using System.Net;
using System.Web.Script.Serialization;

namespace SitePack
{
    public class SiteSankaku : AbstractImageSite
    {
        private SessionClient Sweb = new SessionClient();
        private Random rand = new Random();
        private string[] user = { "hdy", "moelu1" };
        private string[] pass = { "hdy123456", "moe2017" };
        private string sitePrefix, tempuser, temppass, tempappkey, ua;
        private static string cookie = "";
        private static bool isLogin = false;

        public override string SiteUrl { get { return "https://" + sitePrefix + ".sankakucomplex.com"; } }
        public override string SiteName { get { return sitePrefix + ".sankakucomplex.com"; } }
        public override string ShortName { get { return (sitePrefix.Contains("chan") ? "chan.sku" : "idol.sku"); } }
        public override string ShortType { get { return ""; } }
        public override bool IsSupportScore { get { return false; } }
        public override bool IsSupportCount { get { return true; } }
        public override string Referer { get { return "https://" + sitePrefix + ".sankakucomplex.com/post/show/12345"; } }
        public override string SubReferer { get { return ShortName + ",sankakucomplex"; } }

        /// <summary>
        /// sankakucomplex site
        /// </summary>
        public SiteSankaku(string prefix)
        {
            sitePrefix = prefix;
        }

        /// <summary>
        /// 取页面源码 来自官方APP处理方式
        /// </summary>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <param name="keyWord"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            string url = "", pageString = "";
            if (sitePrefix == "chan")
            {
                ua = "SCChannelApp/2.3 (Android; black)";
                Login(proxy);

                url = "https://capi.sankakucomplex.com/post/index.json?login=" + tempuser + "&password_hash="
                    + temppass + "&appkey=" + tempappkey + "&page=" + page + "&limit=" + count;
            }
            else if (sitePrefix == "idol")
            {
                ua = "SCChannelApp/2.3 (Android; idol)";
                Login(proxy);
                //https://iapi.sankakucomplex.com/post/index.json?login=用户名&password_hash=SHA1(salt密码)&appkey=(APPKey)&tags=(搜索内容)&page=2&limit=2

                url = "https://iapi.sankakucomplex.com/post/index.json?login=" + tempuser
                    + "&password_hash=" + temppass + "&appkey=" + tempappkey + "&page=" + page + "&limit=" + count;
            }

            if (keyWord.Length > 0)
            {
                url += "&tags=" + keyWord;
            }
            pageString = Sweb.Get(url, proxy, Encoding.UTF8, ua);

            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();

            BooruProcessor nowSession = new BooruProcessor(BooruProcessor.SourceType.JSONSku);
            imgs = nowSession.ProcessPage(Referer, pageString);

            return imgs;
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();

            //https://chan.sankakucomplex.com/tag/autosuggest?tag=*****&locale=en
            string url = string.Format("https://" + sitePrefix + ".sankakucomplex.com/tag/autosuggest?tag={0}", word);
            MyWebClient web = new MyWebClient();
            web.Timeout = 8;
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;
            web.Headers[HttpRequestHeader.Cookie] = cookie;

            string json = web.DownloadString(url);
            object[] array = (new JavaScriptSerializer()).DeserializeObject(json) as object[];
            string name = "", count = "";

            if (array.Count() > 1)
            {
                if (array[1].GetType().FullName.Contains("Object[]"))
                {
                    int i = 2;
                    foreach (object names in array[1] as object[])
                    {
                        name = names.ToString();
                        count = array[i].ToString();
                        i++;
                        re.Add(new TagItem() { Name = name, Count = count });
                    }
                }
            }

            return re;
        }

        private string FixUrl(string url)
        {
            if (url.StartsWith("//"))
            {
                url = "https:" + url;
            }
            else if (url.StartsWith("/"))
            {
                url = SiteUrl + url;
            }
            if (url.Contains("?"))
            {
                url = url.Substring(0, url.LastIndexOf('?'));
            }
            return url;
        }

        /// <summary>
        /// 这破站用API需要登录！(╯‵□′)╯︵┻━┻
        /// 两个图站的账号还不通用(╯‵□′)╯︵┻━┻
        /// </summary>
        /// <param name="proxy"></param>
        private void Login(IWebProxy proxy)
        {
            string subdomain = sitePrefix.Substring(0, 1) + "api";

            //防止二次登录造成Cookie错误
            if (isLogin)
            {
                while (!cookie.Contains(subdomain + ".sankaku"))
                {
                    System.Threading.Thread.Sleep(100);
                }
                return;
            }

            if (!cookie.Contains(subdomain + ".sankaku"))
            {
                try
                {
                    isLogin = true;
                    cookie = "";
                    int index = rand.Next(0, user.Length);
                    tempuser = user[index];
                    temppass = GetSankakuPwHash(pass[index]);
                    tempappkey = GetSankakuAppkey(tempuser);
                    string post = "login=" + tempuser + "&password_hash=" + temppass + "&appkey=" + tempappkey;

                    //Post登录取Cookie

                    Sweb.Post(
                        "https://" + subdomain + ".sankakucomplex.com/user/authenticate.json",
                        post, proxy, Encoding.GetEncoding("UTF-8"), ua
                        );
                    cookie = Sweb.GetURLCookies("https://" + subdomain + ".sankakucomplex.com");

                    if (sitePrefix == "idol" && !cookie.Contains("sankakucomplex_session"))
                        throw new Exception("获取登录Cookie失败");
                    else
                        cookie = subdomain + ".sankaku;" + cookie;

                }
                catch (Exception e)
                {
                    throw new Exception("自动登录失败: " + e.Message);
                }
                finally
                {
                    isLogin = false;
                }
            }
        }

        /// <summary>
        /// 计算用于登录等账号操作的AppKey
        /// </summary>
        /// <param name="user">用户名</param>
        /// <returns></returns>
        private static string GetSankakuAppkey(string user)
        {
            return SHA1("sankakuapp_" + user.ToLower() + "_Z5NE9YASej", Encoding.Default).ToLower();
        }

        /// <summary>
        /// 计算密码sha1
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns></returns>
        private static string GetSankakuPwHash(string password)
        {
            return SHA1("choujin-steiner--" + password + "--", Encoding.Default).ToLower();
        }

        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="content">字符串</param>
        /// <param name="encode">编码</param>
        /// <returns></returns>
        private static string SHA1(string content, Encoding encode)
        {
            try
            {
                System.Security.Cryptography.SHA1 sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                byte[] bytes_in = encode.GetBytes(content);
                byte[] bytes_out = sha1.ComputeHash(bytes_in);
                string result = BitConverter.ToString(bytes_out);
                result = result.Replace("-", "");
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("SHA1Error:" + ex.Message);
            }
        }
    }
}
