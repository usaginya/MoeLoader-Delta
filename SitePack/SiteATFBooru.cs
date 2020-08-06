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
        private const string Cookieflag = "atfbooru=t;";
        private static string cookie = string.Empty, nowUser = cookie, nowPwd = cookie;
        private static bool IsLoginSite = false, IsRunLogin = IsLoginSite, onceLogin = true;
        private const string loginUrl = "https://booru.allthefallen.moe/session/new";

        public override string SiteUrl => "https://booru.allthefallen.moe";
        public override string SiteName => "atfbooru.ninja";
        public override string ShortName => "atfbooru";
        public override string LoginURL => SiteManager.SiteLoginType.FillIn.ToSafeString();
        public override bool LoginSiteIsLogged => IsLoginSite;
        public override string LoginUser { get => nowUser; set => nowUser = value; }
        public override string LoginPwd { get => nowPwd; set => nowPwd = value; }

        public SiteATFBooru()
        {
            booru = new SiteBooru(
                SiteUrl, $"{SiteUrl}/posts.json?page={{0}}&limit={{1}}&tags={{2}}",
                $"{SiteUrl}/tags/autocomplete.json?search%5Bname_matches%5D={{0}}"
                , SiteName, ShortName, SiteUrl, false, BooruProcessor.SourceType.JSON);
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            if (onceLogin && !IsLoginSite)
            {
                onceLogin = false;
                LoadUser();
                LoginCall(new LoginSiteArgs() { User = nowUser, Pwd = nowPwd });
            }
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
        /// 调用登录
        /// </summary>
        public override void LoginCall(LoginSiteArgs loginArgs)
        {
            if (IsRunLogin || string.IsNullOrWhiteSpace(loginArgs.User) || string.IsNullOrWhiteSpace(loginArgs.Pwd)) { return; }
            nowUser = loginArgs.User;
            nowPwd = loginArgs.Pwd;
            Login(SiteManager.Mainproxy);
        }

        /// <summary>
        /// 还原Cookie
        /// </summary>
        private void CookieRestore()
        {
            if (!string.IsNullOrWhiteSpace(cookie) || cookie.Contains(Cookieflag)) return;
            string ck = Sweb.GetURLCookies(SiteUrl);
            cookie = string.IsNullOrWhiteSpace(ck) ? string.Empty : $"{Cookieflag}{ck}";
        }

        private void Login(IWebProxy proxy)
        {
            IsLoginSite = false;
            IsRunLogin = true;
            CookieRestore();
            if (!cookie.Contains(Cookieflag))
            {
                try
                {
                    cookie = string.Empty;
                    string pagedata = string.Empty, token = string.Empty;

                    shc.Referer = $"{SiteUrl}/login";
                    HtmlDocument hdoc = new HtmlDocument();

                    //1 Get csrf-token
                    pagedata = Sweb.Get(loginUrl, proxy, shc);
                    hdoc.LoadHtml(pagedata);
                    token = hdoc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']").Attributes["content"].Value;
                    if (token.Length < 9)
                    {
                        nowUser = nowPwd = null;
                        SiteManager.EchoErrLog(SiteName, "登录失败1 ", !onceLogin);
                        return;
                    }

                    //2 Post login
                    pagedata = $"authenticity_token={UrlEncode(token)}&session%5Burl%5D=&session%5Bname%5D={nowUser}&session%5Bpassword%5D=={nowPwd}&commit=Login";
                    pagedata = Sweb.Post(loginUrl.Replace("/new", string.Empty), pagedata, proxy, shc);
                    cookie = Sweb.GetURLCookies(SiteUrl);

                    //if (!pagedata.Contains("setUserId"))
                    //{
                    //    nowUser = nowPwd = null;
                    //    SiteManager.EchoErrLog(SiteName, $"{SiteName} 登录失败", !onceLogin);
                    //}
                  //  else
                   // {
                        IsLoginSite = true;
                        cookie = $"{Cookieflag}{cookie}";
                        SaveUser();
                 //   }
                }
                catch (Exception e)
                {
                    nowUser = nowPwd = null;
                    SiteManager.EchoErrLog(SiteName, e, e.Message.Contains("IP") ? e.Message : "可能无法连接到服务器", !onceLogin);
                }
            }
            IsRunLogin = false;
        }

        /// <summary>
        /// 保存账号
        /// </summary>
        private void SaveUser()
        {
            SiteManager.SiteConfig(ShortName, "Login", "User", nowUser, true);
            SiteManager.SiteConfig(ShortName, "Login", "Pwd", nowPwd, true);
        }

        /// <summary>
        /// 载入账号
        /// </summary>
        private void LoadUser()
        {
            nowUser = SiteManager.SiteConfig(ShortName, "Login", "User");
            nowPwd = SiteManager.SiteConfig(ShortName, "Login", "Pwd");
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
