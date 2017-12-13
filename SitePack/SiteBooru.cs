using MoeLoaderDelta;
using System.Collections.Generic;
using System.Xml;

namespace SitePack
{
    /// <summary>
    /// Booru系站点
    /// </summary>
    public class SiteBooru : AbstractImageSite
    {
        //缩略图大小
        //private const int SWIDTH = 150;
        //private const int SHEIGHT = 150;
        //private const int LWIDTH = 300;
        //private const int LHEIGHT = 300;

        /// <summary>
        /// eg. http://yande.re/post/index.xml?page={0}&limit={1}&tags={2}
        /// </summary>
        public string siteUrl;
        /// <summary>
        /// eg. http://yande.re/tag/index.xml?limit={0}&order=count&name={1}
        /// </summary>
        public string tagUrl;
        protected string siteName;
        protected string shortName;
        protected string shortType;
        protected string referer;
        protected bool needMinus;
        protected BooruProcessor.SourceType srcType;
        protected SessionClient Sweb = new SessionClient();
        protected SessionHeadersCollection shc = new SessionHeadersCollection();

        /// <summary>
        /// Booru Site
        /// </summary>
        /// <param name="siteUrl">站点解析地址</param>
        /// <param name="tagUrl">tag自动提示地址</param>
        /// <param name="siteName">站点名</param>
        /// <param name="shortName">站点短名</param>
        /// <param name="referer">引用地址</param>
        /// <param name="needMinus">页码是否从0开始</param>
        /// <param name="srcType">解析类型</param>
        public SiteBooru(string siteUrl, string tagUrl, string siteName, string shortName, string referer,
            bool needMinus, BooruProcessor.SourceType srcType)
        {
            this.siteName = siteName;
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            this.shortName = shortName;
            this.referer = referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            //ShowExplicit = false;
            SetHeaders(srcType);
        }

        /// <summary>
        /// Use after successful login
        /// </summary>
        /// <param name="siteUrl">站点解析地址</param>
        /// <param name="tagUrl">tag自动提示地址</param>
        /// <param name="siteName">站点名</param>
        /// <param name="shortName">站点短名</param>
        /// <param name="needMinus">页码是否从0开始</param>
        /// <param name="srcType">解析类型</param>
        /// <param name="shc">Headers</param>
        public SiteBooru(string siteUrl, string tagUrl, string siteName, string shortName, bool needMinus,
            BooruProcessor.SourceType srcType, SessionHeadersCollection shc)
        {
            this.siteName = siteName;
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            this.shortName = shortName;
            referer = shc.Referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            this.shc = shc;
        }

        public override string SiteUrl { get { return siteUrl.Substring(0, siteUrl.IndexOf('/', 8)); } }
        public override string SiteName { get { return siteName; } }
        public override string ShortName { get { return shortName; } }
        public override string ShortType { get { return shortType; } }
        public override string Referer { get { return referer; } }
        public override string SubReferer { get { return ShortName; } }

        private void SetHeaders(BooruProcessor.SourceType srcType)
        {
            shc.Referer = referer;
            shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
            shc.AutomaticDecompression = System.Net.DecompressionMethods.GZip;

            SetHeaderType(srcType);
        }

        private void SetHeaderType(BooruProcessor.SourceType srcType)
        {
            switch (srcType)
            {
                case BooruProcessor.SourceType.JSON:
                case BooruProcessor.SourceType.JSONNV:
                case BooruProcessor.SourceType.JSONSku:
                    shc.Accept = shc.ContentType = SessionHeadersValue.AcceptAppJson; break;
                case BooruProcessor.SourceType.XML:
                case BooruProcessor.SourceType.XMLNV:
                    shc.Accept = shc.ContentType = SessionHeadersValue.AcceptTextXml; break;
                default:
                    shc.ContentType = SessionHeadersValue.AcceptTextHtml; break;
            }
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            string url;
            if (count > 0)
                url = string.Format(siteUrl, needMinus ? page - 1 : page, count, keyWord);
            else
                url = string.Format(siteUrl, needMinus ? page - 1 : page, keyWord);

            url = keyWord.Length < 1 ? url.Substring(0, url.Length - 6) : url;

            SetHeaderType(srcType);
            return Sweb.Get(url, proxy, "UTF-8", shc);
        }

        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            BooruProcessor nowSession = new BooruProcessor(srcType);
            return nowSession.ProcessPage(siteUrl, pageString);
        }

        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();
            if (string.IsNullOrWhiteSpace(tagUrl)) return re;

            string url = string.Format(tagUrl, 8, word);

            shc.Accept = SessionHeadersValue.AcceptTextXml;
            shc.ContentType = SessionHeadersValue.AcceptAppXml;
            string xml = Sweb.Get(url, proxy, "UTF-8", shc);


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

            return re;
        }
    }
}
