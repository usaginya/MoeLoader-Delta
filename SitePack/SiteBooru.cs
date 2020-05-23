using MoeLoaderDelta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;

namespace SitePack
{
    /// <summary>
    /// Booru系站点
    /// Last 200524
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

        private Dictionary<string, string> siteLoginUser = new Dictionary<string, string>();

        private string siteINI;
        private string SiteINI
        {
            get => siteINI; set => siteINI = string.Format($"{SiteManager.SitePacksPath}{{0}}.ini", value);
        }
        /// <summary>
        /// 读写站点设置
        /// </summary>
        /// <param name="section">项名</param>
        /// <param name="key">键名</param>
        /// <param name="save">写配置</param>
        /// <param name="value">写入值</param>
        /// <returns></returns>
        private string SiteConfig(string section, string key, bool save = false, string value = null)
        {
            return save
                ? SiteManager.WritePrivateProfileString(section, key, value, SiteINI).ToSafeString()
                : SiteManager.GetPrivateProfileString(section, key, SiteINI);
        }

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
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            SiteINI = this.shortName = shortName;
            this.referer = referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            this.loginUrl = loginUrl;
            SetHeaders(srcType);
            if (string.IsNullOrWhiteSpace(SiteConfig("Login", "Cookie")))
            {
                SiteConfig("Login", "Cookie", true, string.Empty);
            }
            siteLoginUser.Add(shortName, null);
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
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            SiteINI = this.shortName = shortName;
            referer = shc.Referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            this.shc = shc;
            this.loginUrl = loginUrl;
            siteLoginUser.Add(shortName, null);
        }

        public override string SiteUrl => siteUrl;
        public override string SiteName => siteName;
        public override string ShortName => shortName;
        public override string ShortType => shortType;
        public override string Referer => referer;
        public override string SubReferer => ShortName;
        public override string LoginURL => SiteManager.SiteLoginType.Custom.ToSafeString();
        public override bool LoginSite { get => IsLoginSite; set => IsLoginSite = value; }
        public override string LoginUser => siteLoginUser[shortName] ?? base.LoginUser;

        private static bool IsLoginSite, IsRunLogin;
        private static string cookie = string.Empty;

        private void SetHeaders(BooruProcessor.SourceType srcType)
        {
            shc.Referer = referer;
            shc.Timeout = 27000;
            shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
            shc.AutomaticDecompression = DecompressionMethods.GZip;

            SetHeaderType(srcType);
        }

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

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            #region Auto login
            IsRunLogin = true;
            LoginCall(proxy);
            if (!string.IsNullOrWhiteSpace(cookie)) { shc.Set("Cookie", cookie); }
            IsRunLogin = false;
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

            if (count > 0)
                url = string.Format(Url, pagestr, count, keyWord);
            else
                url = string.Format(Url, pagestr, keyWord);

            url = keyWord.Length < 1 ? url.Substring(0, url.Length - 6) : url;

            return Sweb.Get(url, proxy, shc);
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            BooruProcessor nowSession = new BooruProcessor(srcType);
            return nowSession.ProcessPage(siteUrl, shortName, Url, pageString);
        }

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

                object[] jsonobj = (new JavaScriptSerializer()).DeserializeObject(url) as object[];

                foreach (Dictionary<string, object> o in jsonobj)
                {
                    string name = "", count = "";
                    if (o.ContainsKey("name"))
                        name = o["name"].ToString();
                    if (o.ContainsKey("post_count"))
                        count = o["post_count"].ToString();
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

        public override bool LoginCall(IWebProxy proxy)
        {
            if (!string.IsNullOrWhiteSpace(loginUrl))
            {
                if (IsRunLogin)
                {
                    if (!IsLoginSite) { CookieLogin(proxy, true); }
                }
                else
                {
                    CookieLogin(proxy, false, true);
                }
            }
            else if (!IsRunLogin)
            {
                MessageBox.Show("暂不支持登录", SiteName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return IsLoginSite;
        }

        /// <summary>
        /// 用本地Cookie登录
        /// </summary>
        /// <param name="callBack">判断是否回调</param>
        /// <param name="call">以调用方式登录</param>
        private bool CookieLogin(IWebProxy proxy, bool callBack = false, bool call = false)
        {
            IsLoginSite = false;

            bool result = IsLoginSite;
            string tmp_cookie = SiteConfig("Login", "Cookie"), loggedFlags;

            switch (shortName)
            {
                case "donmai":
                    loggedFlags = "/profile"; break;
                default:
                    loggedFlags = string.Empty; break;
            }

            if (string.IsNullOrWhiteSpace(tmp_cookie) || call)
            {
                if (!callBack || call)
                {
                    DialogResult mdr = MessageBox.Show("需要查看登录教程吗？", ShortName,
                          MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (mdr == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start("https://docs.qq.com/doc/DWWhUcHlzbE9aeXZE?pub=1");
                        return IsLoginSite;
                    }
                    else if (mdr == DialogResult.Cancel) { return IsLoginSite; }

                    System.Diagnostics.Process.Start(siteINI);
                    System.Diagnostics.Process.Start(loginUrl);

                    if (MessageBox.Show("请把登录后的Cookie复制\r\n"
                        + "粘贴到打开的记事本中Cookie=后面\r\n"
                        + "然后保存并关闭记事本，再点击确定按钮",
                        ShortName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                    {
                        //登录后回调一次
                        CookieLogin(proxy, true);
                    }
                }
                return IsLoginSite;
            }

            try
            {
                string pageString = string.Empty,
                    oldAccept = shc.Accept,
                    oldContentType = shc.ContentType;

                if (!string.IsNullOrWhiteSpace(loggedFlags))
                {
                    shc.Timeout = shc.Timeout * 2;
                    shc.Set("Cookie", tmp_cookie);
                    shc.Accept = SessionHeadersValue.AcceptDefault;
                    shc.ContentType = SessionHeadersValue.ContentTypeAuto;
                    pageString = Sweb.Get(SiteUrl, proxy, shc);
                    shc.Accept = oldAccept;
                    shc.ContentType = oldContentType;
                    result = !string.IsNullOrWhiteSpace(pageString);

                    if (result)
                    {
                        string[] LFlagsArray = loggedFlags.Split('|');
                        foreach (string Flag in LFlagsArray)
                        {
                            result &= pageString.Contains(Flag);
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(tmp_cookie)) { result = true; }

                if (result)
                {
                    IsLoginSite = result;
                    cookie = tmp_cookie;
                    string loginUser = string.Empty;

                    switch (shortName)
                    {
                        case "donmai":
                            Regex rx = new Regex("data-current-user-name=\"(.*?)\"", RegexOptions.IgnoreCase);
                            GroupCollection group = rx.Match(pageString).Groups;

                            if (group.Count > 1) { loginUser = group[1].Value; }
                            siteLoginUser[shortName] = $"{loginUser} 已登录";
                            break;
                        default:
                            siteLoginUser[shortName] = "已登录"; break;
                    }

                }
                else
                {
                    siteLoginUser[shortName] = null;
                    result = IsLoginSite = false;
                }
            }
            catch
            {
                SiteManager.EchoErrLog(SiteName, "用户登录失败 ");
            }

            return result;
        }

    }
}
