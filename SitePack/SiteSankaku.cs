using MoeLoaderDelta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;

namespace SitePack
{
    public class SiteSankaku : AbstractImageSite
    {
        private SiteBooru booru;
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private string sitePrefix, temppass, tempappkey, ua, pageurl;
        private static string cookie = string.Empty, authorization = cookie, nowUser = cookie, nowPwd = cookie, prevSitePrefix = cookie;
        private static int IsLoginSite = 0;
        private static readonly string LocalAccountINI = $"{SiteManager.SitePacksPath}sankaku.ini";

        public override string SiteUrl => $"https://{sitePrefix}.sankakucomplex.com";
        public override string SiteName => $"{sitePrefix}.sankakucomplex.com";
        public override string ShortName => sitePrefix.Contains("chan") ? "chan.sku" : "idol.sku";
        public override bool IsSupportScore => false;
        public override bool IsSupportCount => true;
        //public override string Referer => sitePrefix.Contains("chan") ? "https://beta.sankakucomplex.com/" : null;
        public override string SubReferer => "*";
        public override string LoginURL => SiteManager.SiteLoginType.FillIn.ToSafeString();
        public override int LoginSiteInt { get => IsLoginSite; set => IsLoginSite = value; }
        public override string LoginUser { get => nowUser; set => nowUser = value; }
        public override string LoginPwd { set => nowPwd = value; }

        /// <summary>
        /// 读INI配置文件
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">项</param>
        /// <param name="def">缺省值</param>
        /// <param name="retval">lpReturnedString取得的内容</param>
        /// <param name="size">lpReturnedString缓冲区的最大字符数</param>
        /// <param name="filePath">配置文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);

        /// <summary>
        /// 写INI配置文件
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">项</param>
        /// <param name="val">值</param>
        /// <param name="filepath">配置文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        public static extern long WritePrivateProfileString(string section, string key, string val, string filepath);

        /// <summary>
        /// sankakucomplex site
        /// </summary>
        public SiteSankaku(string prefix)
        {
            shc.Timeout = 18000;
            sitePrefix = prefix;
            CookieRestore();
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
            if (prevSitePrefix != sitePrefix)
            {
                IsLoginSite = 0;
                prevSitePrefix = sitePrefix;
            }

            if (sitePrefix == "idol")
            {
                ua = "SCChannelApp/3.2 (Android; idol)";
            }

            Login(proxy);
            return booru?.GetPageString(page, count, keyWord, proxy);
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            return booru?.GetImages(pageString, proxy);
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();

            //https://chan.sankakucomplex.com/tag/autosuggest?tag=*****&locale=en
            string url = string.Format(SiteUrl + "/tag/autosuggest?tag={0}", word);
            shc.ContentType = SessionHeadersValue.AcceptAppJson;
            string json = Sweb.Get(url, proxy, shc);
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

        /// <summary>
        /// 还原Cookie
        /// </summary>
        private void CookieRestore()
        {
            if (!string.IsNullOrWhiteSpace(cookie) || sitePrefix.Contains("chan"))
            {
                return;
            }

            string ck = Sweb.GetURLCookies(SiteUrl);
            cookie = string.IsNullOrWhiteSpace(ck) ? string.Empty : $"iapi.sankaku;{ck}";
        }

        /// <summary>
        /// 设置登录账号
        /// </summary>
        private int SetLogin()
        {
            try
            {
                if (IsLoginSite > 0)
                {
                    //已从界面填入了账号 保存起来
                    SetLocalAccount(1, nowUser);
                    SetLocalAccount(2, nowPwd);
                }
                else
                {
                    //还没有账号就从本地读取
                    if (string.IsNullOrWhiteSpace(nowPwd) || prevSitePrefix != sitePrefix)
                    {
                        nowUser = GetLocalAccount(1);
                        nowPwd = GetLocalAccount(2);
                        if (nowPwd.Length < 1)
                        {
                            throw new Exception();
                        }
                    }
                }
                return IsLoginSite > 1 ? 2 : 1;
            }
            catch
            {
                SiteManager.echoErrLog(ShortName, "搜索之前必须先登录站点", false, true);
                return 0;
            }
        }

        /// <summary>
        /// 两个子站登录方式不同
        /// chan 使用 Authorization
        /// idol 使用 Cookie
        /// </summary>
        /// <param name="proxy"></param>
        private void Login(IWebProxy proxy)
        {

            if (IsLoginSite != 1)
            {
                string subdomain = sitePrefix.Substring(0, 1),
                    loginhost = "https://";

                if (subdomain.Contains("c"))
                {
                    //chan
                    subdomain += "api-v2";
                    loginhost += $"{subdomain}.sankakucomplex.com";

                    if (string.IsNullOrWhiteSpace(authorization) || IsLoginSite != 1)
                    {
                        try
                        {
                            IsLoginSite = SetLogin();
                            if (IsLoginSite < 1)
                            {
                                return;
                            }

                            JObject user = new JObject
                            {
                                ["login"] = nowUser,
                                ["password"] = nowPwd
                            };
                            string post = JsonConvert.SerializeObject(user);

                            //Post登录取Authorization
                            shc.Accept = "application/vnd.sankaku.api+json;v=2";
                            shc.ContentType = SessionHeadersValue.AcceptAppJson;
                            Sweb.CookieContainer = null;

                            post = Sweb.Post(loginhost + "/auth/token", post, proxy, shc);
                            if (string.IsNullOrWhiteSpace(post) || !post.Contains("{"))
                            {
                                IsLoginSite = 0;
                                nowUser = nowPwd = null;
                                SiteManager.echoErrLog(ShortName, $"登录失败 - {post}");
                                return;
                            }

                            JObject jobj = JObject.Parse(post);
                            if (jobj.Property("token_type") != null)
                            {
                                authorization = $"{jobj["token_type"]} {jobj["access_token"]} ";
                            }

                            if (string.IsNullOrWhiteSpace(authorization))
                            {
                                IsLoginSite = 0;
                                nowUser = nowPwd = null;
                                SiteManager.echoErrLog(ShortName, "登录失败 - 验证账号错误");
                                return;
                            }

                            pageurl = $"{loginhost}/posts?page={{0}}&limit={{1}}&tags=hide_posts_in_books:never+{{2}}";

                            //登录成功 初始化Booru类型站点
                            booru = new SiteBooru(SiteUrl, pageurl, null, SiteName, ShortName, false, BooruProcessor.SourceType.JSONcSku, shc);

                            //保存账号
                            SetLogin();
                        }
                        catch (Exception e)
                        {
                            IsLoginSite = 0;
                            nowUser = nowPwd = null;
                            SiteManager.echoErrLog(ShortName, e, "登录失败 - 内部错误");
                        }
                    }

                }
                else
                {
                    //idol
                    subdomain += "api";
                    loginhost += $"{subdomain}.sankakucomplex.com";

                    if (string.IsNullOrWhiteSpace(cookie) || !cookie.Contains($"{subdomain}.sankaku") || IsLoginSite != 1)
                    {
                        try
                        {
                            IsLoginSite = SetLogin();
                            if (IsLoginSite < 1)
                            {
                                nowUser = nowPwd = null;
                                return;
                            }

                            cookie = string.Empty;

                            temppass = GetSankakuPwHash(nowPwd);
                            tempappkey = GetSankakuAppkey(nowUser);

                            string post = $"login={nowUser}&password_hash={temppass}&appkey={tempappkey}";

                            //Post登录取Cookie
                            shc.UserAgent = ua;
                            shc.Accept = SessionHeadersValue.AcceptAppJson;
                            shc.ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;
                            post = Sweb.Post($"{loginhost}/user/authenticate.json", post, proxy, shc);
                            cookie = Sweb.GetURLCookies(loginhost);

                            if (!cookie.Contains("sankakucomplex_session") || string.IsNullOrWhiteSpace(cookie))
                            {
                                IsLoginSite = 0;
                                nowUser = nowPwd = null;
                                SiteManager.echoErrLog(ShortName, $"登录失败 - {post}");
                                return;
                            }
                            else
                            {
                                cookie = $"{subdomain }.sankaku;{cookie}";
                            }

                            pageurl = $"{loginhost}/post/index.json?login={nowUser}&password_hash={temppass}" +
                                $"&appkey={tempappkey}&page={{0}}&limit={{1}}&tags={{2}}";

                            //登录成功 初始化Booru类型站点
                            booru = new SiteBooru(SiteUrl, pageurl, null, SiteName, ShortName, false, BooruProcessor.SourceType.JSONiSku, shc);

                            //保存账号
                            SetLogin();
                        }
                        catch (Exception e)
                        {
                            IsLoginSite = 0;
                            nowUser = nowPwd = null;
                            SiteManager.echoErrLog(ShortName, e, "登录失败 - 内部错误");
                        }
                    }

                }
            }

        }

        /// <summary>
        /// 读取本地账号
        /// </summary>
        /// <param name="type">1用户名 2密码</param>
        /// <returns></returns>
        private string GetLocalAccount(int type)
        {
            StringBuilder sb = new StringBuilder();
            string key = string.Empty;

            switch (type)
            {
                case 1:
                    key = "User"; break;
                case 2:
                    key = "Pwd"; break;
            }

            GetPrivateProfileString(sitePrefix, key, string.Empty, sb, 255, LocalAccountINI);
            return sb.ToSafeString();
        }

        /// <summary>
        /// 写本地账号
        /// </summary>
        /// <param name="type">1用户名 2密码</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        private long SetLocalAccount(int type, string value)
        {
            string key = string.Empty;

            switch (type)
            {
                case 1:
                    key = "User"; break;
                case 2:
                    key = "Pwd"; break;
            }

            return WritePrivateProfileString(sitePrefix, key, value, LocalAccountINI);
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
                result = result.Replace("-", string.Empty);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("SHA1Error:" + ex.Message);
            }
        }
    }
}
