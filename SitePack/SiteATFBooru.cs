using MoeLoaderDelta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace SitePack
{
    class SiteATFBooru : AbstractImageSite
    {
        private SiteBooru booru;
        private SessionClient Sweb = new SessionClient();
        private readonly SessionHeadersCollection shc = new SessionHeadersCollection();
        public override SessionHeadersCollection SiteHeaders => shc;
        private const string Cookieflag = "atfbooru=t;";
        private static string cookie = string.Empty, nowUser = cookie;
        private static bool IsLoginSite = false, IsRunLogin = IsLoginSite, onceLogin = true;

        public override string SiteUrl => "https://booru.allthefallen.moe";
        public override string SiteName => "atfbooru.ninja";
        public override string ShortName => "atfbooru";
        public override string LoginURL => SiteManager.SiteLoginType.Cookie.ToSafeString();
        public override bool LoginSiteIsLogged => IsLoginSite;
        public override string LoginUser { get => nowUser; set => nowUser = value; }

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
                LoginCall(new LoginSiteArgs() { User = nowUser, Cookie = cookie });
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
            if (IsRunLogin || string.IsNullOrWhiteSpace(loginArgs.Cookie)) { return; }
            nowUser = loginArgs.User;
            cookie = loginArgs.Cookie;
            Login(SiteManager.GetWebProxy());
        }


        private void Login(IWebProxy proxy)
        {
            IsLoginSite = false;
            IsRunLogin = true;

            try
            {
                string pagedata = string.Empty;
                shc.Set(HttpRequestHeader.Cookie, cookie);
                pagedata = Sweb.Get($"{SiteUrl}/profile", proxy, shc);

                Regex regex = new Regex("data-current-user-name=\"(.*?)\"", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                if (!regex.IsMatch(pagedata))
                {
                    cookie = nowUser = string.Empty;
                    SiteManager.EchoErrLog(SiteName, $"{SiteName} 登录失败", !onceLogin);
                }
                else
                {
                    nowUser = regex.Match(pagedata).Groups[1].ToSafeString();
                    IsLoginSite = true;
                    cookie = $"{Cookieflag}{cookie}";
                    SaveUser();
                }
            }
            catch (Exception e)
            {
                cookie = nowUser = string.Empty;
                SiteManager.EchoErrLog(SiteName, e, e.Message.Contains("IP") ? e.Message : "可能无法连接到服务器", !onceLogin);
            }

            IsRunLogin = false;
        }

        /// <summary>
        /// 保存账号
        /// </summary>
        private void SaveUser()
        {
            SiteManager.SiteConfig(ShortName, new SiteConfigArgs() { Section = "Login", Key = "Cookie", Value = cookie }, SiteManager.SiteConfigType.Change);
        }

        /// <summary>
        /// 载入账号
        /// </summary>
        private void LoadUser()
        {
            cookie = SiteManager.SiteConfig(ShortName, new SiteConfigArgs() { Section = "Login", Key = "Cookie", Value = cookie });
        }
    }
}
