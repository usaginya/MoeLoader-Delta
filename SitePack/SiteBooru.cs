using MoeLoaderDelta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xml;

namespace SitePack
{
    /// <summary>
    /// Booru系站点
    /// Last 210120
    /// </summary>
    public class SiteBooru : AbstractImageSite
    {
        /// <summary>
        /// eg. http://yande.re/post/index.xml?page={0}&limit={1}&tags={2}
        /// eg. http://yande.re/tag/index.xml?limit={0}&order=count&name={1}
        /// </summary>
        public string Url, siteUrl, tagUrl;

        protected string siteName, shortName, shortType, referer, loginUrl;
        protected bool needMinus;
        protected BooruProcessor.SourceType srcType;
        protected SessionClient Sweb = new SessionClient();
        protected SessionHeadersCollection shc = new SessionHeadersCollection();
        public override SessionHeadersCollection SiteHeaders => shc;

        private Dictionary<string, string> siteLoginUser = new Dictionary<string, string>();
        private Dictionary<string, string> siteLoginCookie = new Dictionary<string, string>();

        /// <summary>
        /// Booru Site
        /// </summary>
        /// <param name="siteUrl">站点解析地址</param>
        /// <param name="url">图库服务器地址</param>
        /// <param name="tagUrl">tag自动提示地址</param>
        /// <param name="siteName">站点名</param>
        /// <param name="shortName">站点短名</param>
        /// <param name="referer">引用地址</param>
        /// <param name="needMinus">页码是否从0开始</param>
        /// <param name="srcType">解析类型</param>
        /// <param name="loginUrl">登录地址</param>
        public SiteBooru(string siteUrl, string url, string tagUrl, string siteName, string shortName, string referer,
            bool needMinus, BooruProcessor.SourceType srcType, string loginUrl = null)
        {
            Url = url;
            this.siteName = siteName;
            this.shortName = shortName;
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            this.referer = referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            this.loginUrl = loginUrl;
            SetHeaders(srcType);
            siteLoginUser.Add(shortName, null);
            siteLoginCookie.Add(shortName, null);
        }

        /// <summary>
        /// Use after successful login
        /// </summary>
        /// <param name="siteUrl">站点解析地址</param>
        /// <param name="url">图库服务器地址</param>
        /// <param name="tagUrl">tag自动提示地址</param>
        /// <param name="siteName">站点名</param>
        /// <param name="shortName">站点短名</param>
        /// <param name="needMinus">页码是否从0开始</param>
        /// <param name="srcType">解析类型</param>
        /// <param name="shc">Headers</param>
        /// <param name="loginUrl">登录地址</param>
        public SiteBooru(string siteUrl, string url, string tagUrl, string siteName, string shortName, bool needMinus,
            BooruProcessor.SourceType srcType, SessionHeadersCollection shc, string loginUrl = null)
        {
            Url = url;
            this.siteName = siteName;
            this.shortName = shortName;
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            referer = shc.Referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            this.shc = shc;
            this.loginUrl = loginUrl;
            siteLoginUser.Add(shortName, null);
            if (string.IsNullOrEmpty(shc.Get("Cookie")))
                siteLoginCookie.Add(shortName, null);
            else
                siteLoginCookie.Add(shortName, shc.Get("Cookie"));
        }

        public override string SiteUrl => siteUrl;
        public override string SiteName => siteName;
        public override string ShortName => shortName;
        public override string ShortType => shortType;
        public override string Referer => referer;
        public override string SubReferer => ShortName;
        public override string LoginURL => SiteManager.SiteLoginType.Cookie.ToSafeString();
        public override bool LoginSiteIsLogged => IsLoginSite;
        public override string LoginUser => siteLoginUser[shortName] ?? base.LoginUser;
        public override string LoginPwd { get => loginPwd; set => loginPwd = value; }
        public override string LoginHelpUrl => "https://docs.qq.com/doc/DWWhUcHlzbE9aeXZE?pub=1";

        private static bool IsLoginSite, IsRunLogin;
        private static string loginPwd = string.Empty;

        /// <summary>
        /// 设置访问Headers
        /// </summary>
        private void SetHeaders(BooruProcessor.SourceType srcType)
        {
            shc.Referer = referer;
            shc.Timeout = 27000;
            shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
            shc.AutomaticDecompression = DecompressionMethods.GZip;

            SetHeaderType(srcType);
        }

        /// <summary>
        /// 设置访问Accept
        /// </summary>
        private void SetHeaderType(BooruProcessor.SourceType srcType)
        {
            switch (srcType)
            {
                case BooruProcessor.SourceType.JSON:
                case BooruProcessor.SourceType.JSONNV:
                case BooruProcessor.SourceType.JSONiSku:
                    shc.Accept = shc.ContentType = SessionHeadersValue.AcceptAppJson; break;
                case BooruProcessor.SourceType.XML:
                case BooruProcessor.SourceType.XMLNV:
                    shc.Accept = shc.ContentType = SessionHeadersValue.AcceptTextXml; break;
                default:
                    shc.ContentType = SessionHeadersValue.AcceptTextHtml; break;
            }
        }

        /// <summary>
        /// 获取源码
        /// </summary>
        /// <param name="page">页数</param>
        /// <param name="count">获取数</param>
        /// <param name="keyWord">标签</param>
        /// <param name="proxy">代理</param>
        /// <returns></returns>
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            #region Auto login
            if (!IsLoginSite)
            {
                LoginCall(new LoginSiteArgs() { Cookie = siteLoginCookie[shortName] });
            }
            #endregion

            string url, pagestr;
            int tmpID;

            SetHeaderType(srcType);
            page = needMinus ? page - 1 : page;
            pagestr = Convert.ToString(page);

            //Danbooru 1000+ page
            switch (shortName)
            {
                case "donmai":
                case "atfbooru":
                    if (page > 1000)
                    {
                        //取得1000页最后ID
                        List<Img> tmpimgs = GetImages(
                                Sweb.Get(
                                    string.Format(Url, 1000, count, keyWord)
                                , proxy, shc)
                            , proxy);

                        tmpID = tmpimgs[tmpimgs.Count - 1].Id;

                        tmpID -= (page - 1001) * count;
                        pagestr = "b" + tmpID;
                    }
                    break;
            }

            url = (count > 0) ? string.Format(Url, pagestr, count, keyWord) : string.Format(Url, pagestr, keyWord);

            url = keyWord.Length < 1 ? url.Substring(0, url.Length - 6) : url;

            return Sweb.Get(url, proxy, shc);
        }

        /// <summary>
        /// 获取图片列表
        /// </summary>
        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            BooruProcessor nowSession = new BooruProcessor(srcType);
            return nowSession.ProcessPage(siteUrl, shortName, Url, pageString);
        }

        /// <summary>
        /// 获取相关标签
        /// </summary>
        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();
            if (string.IsNullOrWhiteSpace(tagUrl)) return re;

            if (tagUrl.Contains("autocomplete.json"))
            {
                string url = string.Format(tagUrl, word);
                shc.Accept = SessionHeadersValue.AcceptAppJson;
                url = Sweb.Get(url, proxy, shc);

                // [{"id":null,"name":"idolmaster_cinderella_girls","post_count":54050,"category":3,"antecedent_name":"cinderella_girls"},
                // {"id":null,"name":"cirno","post_count":24486,"category":4,"antecedent_name":null}]

                object[] jsonobj = new JavaScriptSerializer().DeserializeObject(url) as object[];

                foreach (Dictionary<string, object> o in jsonobj)
                {
                    string name = string.Empty, count = string.Empty;
                    if (o.ContainsKey("name"))
                    {
                        name = o["name"].ToString();
                    }
                    else if (o.ContainsKey("value"))
                    {
                        name = o["value"].ToString();
                    }

                    if (o.ContainsKey("post_count"))
                    { count = o["post_count"].ToString(); }

                    re.Add(new TagItem()
                    {
                        Name = name,
                        Count = count
                    });
                }
            }
            else
            {
                string url = string.Format(tagUrl, 8, word);

                shc.Accept = SessionHeadersValue.AcceptTextXml;
                shc.ContentType = SessionHeadersValue.AcceptAppXml;
                string xml = Sweb.Get(url, proxy, shc);

                //<?xml version="1.0" encoding="UTF-8"?>
                //<tags type="array">
                //  <tag type="3" ambiguous="false" count="955" name="neon_genesis_evangelion" id="270"/>
                //  <tag type="3" ambiguous="false" count="335" name="angel_beats!" id="26272"/>
                //  <tag type="3" ambiguous="false" count="214" name="galaxy_angel" id="243"/>
                //  <tag type="3" ambiguous="false" count="58" name="wrestle_angels_survivor_2" id="34664"/>
                //</tags>

                if (!xml.Contains("<tag")) return re;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml.ToString());

                XmlElement root = (XmlElement)(xmlDoc.SelectSingleNode("tags")); //root

                foreach (XmlNode node in root.ChildNodes)
                {
                    XmlElement tag = (XmlElement)node;

                    string name = tag.GetAttribute("name");
                    string count = tag.GetAttribute("count");

                    re.Add(new TagItem() { Name = name, Count = count });
                }
            }

            return re;
        }

        /// <summary>
        /// 调用登录
        /// </summary>
        public override void LoginCall(LoginSiteArgs loginArgs)
        {
            if (string.IsNullOrWhiteSpace(loginUrl) || IsRunLogin)
            { return; }

            siteLoginCookie[shortName] = loginArgs.Cookie;
            CookieLogin();
        }

        /// <summary>
        /// 用本地Cookie登录
        /// </summary>
        private void CookieLogin()
        {
            IsRunLogin = true;
            IsLoginSite = false;

            if (string.IsNullOrWhiteSpace(siteLoginCookie[shortName]))
            {
                siteLoginCookie[shortName] = SiteManager.SiteConfig(shortName, new SiteConfigArgs()
                {
                    Section = "Login",
                    Key = "Cookie"
                });
            }

            string tmp_cookie = siteLoginCookie[shortName], loggedFlags;
            if (string.IsNullOrWhiteSpace(tmp_cookie)) { IsRunLogin = false; return; }

            switch (shortName)
            {
                case "donmai":
                    loggedFlags = "/profile"; break;
                default:
                    loggedFlags = string.Empty; break;
            }

            try
            {
                bool result = false;
                string pageString = string.Empty,
                    oldAccept = shc.Accept,
                    oldContentType = shc.ContentType;

                shc.Timeout = shc.Timeout * 2;
                shc.Set("Cookie", tmp_cookie);
                shc.Accept = SessionHeadersValue.AcceptDefault;
                shc.ContentType = SessionHeadersValue.ContentTypeAuto;
                pageString = Sweb.Get(SiteUrl, SiteManager.MainProxy, shc);
                shc.Accept = oldAccept;
                shc.ContentType = oldContentType;
                result = !string.IsNullOrWhiteSpace(pageString);
                if (!result) { SiteManager.EchoErrLog(siteName, "登录失败 站点没有响应", true); }

                if (!string.IsNullOrWhiteSpace(loggedFlags))
                {
                    string[] LFlagsArray = loggedFlags.Split('|');
                    foreach (string Flag in LFlagsArray)
                    {
                        result &= pageString.Contains(Flag);
                    }
                }

                if (result)
                {
                    IsLoginSite = result;
                    siteLoginUser[shortName] = LoginUser;
                    siteLoginCookie[shortName] = tmp_cookie;
                    SiteManager.SiteConfig(shortName, new SiteConfigArgs()
                    {
                        Section = "Login",
                        Key = "Cookie",
                        Value = tmp_cookie
                    }, SiteManager.SiteConfigType.Change);

                    switch (shortName)
                    {
                        case "donmai":
                            Regex rx = new Regex("data-current-user-name=\"(.*?)\"", RegexOptions.IgnoreCase);
                            GroupCollection group = rx.Match(pageString).Groups;
                            if (group.Count > 1) { siteLoginUser[shortName] = group[1].Value; }
                            break;
                    }
                }
                else
                {
                    siteLoginUser[shortName] = siteLoginCookie[shortName] = null;
                    result = IsLoginSite = false;
                }

            }
            catch (Exception e)
            {
                IsLoginSite = false;
                siteLoginUser[shortName] = siteLoginCookie[shortName] = null;
                string msg = $"登录失败{Environment.NewLine}{e.Message}";
                SiteManager.EchoErrLog(SiteName, msg, true);
                SiteManager.ShowToastMsg(msg, SiteManager.MsgType.Error);
            }
            finally { IsRunLogin = false; }
        }

    }
}
