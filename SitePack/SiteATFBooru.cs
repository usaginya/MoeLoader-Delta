using HtmlAgilityPack;
using MoeLoaderDelta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

namespace SitePack
{
    class SiteATFBooru : AbstractImageSite
    {
        private SiteBooru booru;
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private Random rand = new Random();
        private const string Cookieflag = "atfbooru=t;";
        private readonly string[] user = { "mload1901" };
        private readonly string[] pass = { "mload1901pw" };
        private static string cookie = string.Empty, nowUser = null;
        private static bool startLogin, IsLoginSite;

        public override string SiteUrl => "https://atfbooru.ninja";
        public override string SiteName => "atfbooru.ninja";
        public override string ShortName => "atfbooru";
        public override string LoginURL => "https://atfbooru.ninja/session/new";
        public override bool LoginSite { get => IsLoginSite; set => IsLoginSite = value; }
        public override string LoginUser => nowUser ?? base.LoginUser;

        public SiteATFBooru()
        {
            booru = new SiteBooru(
                SiteUrl, $"{SiteUrl}/posts.json?page={{0}}&limit={{1}}&tags={{2}}",
                $"{SiteUrl}/tags/autocomplete.json?search%5Bname_matches%5D={{0}}"
                , SiteName, ShortName, SiteUrl, false, BooruProcessor.SourceType.JSON);
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            Login(proxy);
            return booru.GetPageString(page, count, keyWord, proxy);
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            return booru.GetTags(word, proxy);
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            return booru.GetImages(pageString, proxy);
        }

        /// <summary>
        /// 还原Cookie
        /// </summary>
        private void CookieRestore()
        {
            if (!string.IsNullOrWhiteSpace(cookie) || cookie.Contains(Cookieflag)) return;

            if (!IELogin())
            {
                string ck = Sweb.GetURLCookies(SiteUrl);
                cookie = string.IsNullOrWhiteSpace(ck) ? string.Empty : $"{Cookieflag}{ck}";
            }
            
        }

        private void Login(IWebProxy proxy)
        {
            CookieRestore();
            if (!cookie.Contains(Cookieflag) || IsLoginSite)
            {
                try
                {
                    nowUser = null;
                    cookie = string.Empty;
                    string pagedata = string.Empty, token = string.Empty;

                    if (IsLoginSite)
                    {
                        startLogin = false;
                        if (!IELogin())
                        {
                            Login(proxy); //重新自动登录
                        }
                        startLogin = true;
                    }
                    else
                    {
                        int index = rand.Next(0, user.Length);

                        shc.Referer = LoginURL;
                        shc.Remove("Cookie");
                        HtmlDocument hdoc = new HtmlDocument();

                        //1 Get csrf-token
                        pagedata = Sweb.Get(LoginURL, proxy, shc);
                        hdoc.LoadHtml(pagedata);
                        token = hdoc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']").Attributes["content"].Value;
                        if (token.Length < 9)
                        {
                            SiteManager.echoErrLog(SiteName, "自动登录失败[1] ");
                            return;
                        }

                        //2 Post login
                        pagedata = $"utf8=%E2%9C%93&authenticity_token={UrlEncode(token)}&url=&name={user[index]}&password={pass[index]}&commit=Submit";
                        pagedata = Sweb.Post(LoginURL.Replace("/new", string.Empty), pagedata, proxy, shc);
                        cookie = Sweb.GetURLCookies(SiteUrl);

                        if (!pagedata.Contains("setUserId"))
                        {
                            SiteManager.echoErrLog(SiteName, $"{SiteName} 自动登录失败");
                        }
                        else
                        {
                            cookie = $"{Cookieflag}{cookie}";
                            nowUser = "内置账号";
                        }
                    }

                }
                catch (Exception e)
                {
                    SiteManager.echoErrLog(SiteName, e, e.Message.Contains("IP") ? e.Message : "可能无法连接到服务器");
                }
            }
        }

        /// <summary>
        /// 从IE登录
        /// </summary>
        private bool IELogin()
        {
            IsLoginSite = false;
            cookie = string.Empty;

            string pageString = string.Empty;
            bool result = SiteManager.LoginSite(this, ref cookie, "setUserId", ref Sweb, ref shc, ref pageString);
            HtmlDocument hdoc = new HtmlDocument();

            if (result)
            {
                nowUser = "你的账号";
                cookie = $"{Cookieflag}{cookie}";
                try
                {
                    hdoc.LoadHtml(pageString);
                    pageString = hdoc.DocumentNode.SelectSingleNode("//meta[@name='current-user-name']").Attributes["content"].Value;
                    if (!pageString.IsNullOrEmptyOrWhiteSpace())
                        nowUser += $": {pageString}";
                }
                catch { }
            }
            else if (startLogin)
            {
                SiteManager.echoErrLog(SiteName, "用户未登录或登录失败 ");
            }

            return result;
        }

        /// <summary>
        /// UrlEncode输出大写字母
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string UrlEncode(string str)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in str)
            {
                if (HttpUtility.UrlEncode(c.ToString()).Length > 1)
                {
                    builder.Append(HttpUtility.UrlEncode(c.ToString()).ToUpper());
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

    }
}
