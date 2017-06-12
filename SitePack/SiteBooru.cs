using MoeLoaderDelta;
using System.Collections.Generic;
using System.Text;
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
        protected string siteUrl;
        /// <summary>
        /// eg. http://yande.re/tag/index.xml?limit={0}&order=count&name={1}
        /// </summary>
        protected string tagUrl;
        protected string siteName;
        protected string shortName;
        protected string shortType;
        protected string referer;
        protected bool needMinus;
        protected BooruProcessor.SourceType srcType;

        public SiteBooru(string siteUrl, string tagUrl, string siteName, string shortName, string referer, bool needMinus, BooruProcessor.SourceType srcType)
        {
            this.siteName = siteName;
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            this.shortName = shortName;
            this.referer = referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            //ShowExplicit = false;
        }

        public override string SiteUrl { get { return siteUrl.Substring(0, siteUrl.IndexOf('/', 8)); } }
        public override string SiteName { get { return siteName; } }
        public override string ShortName { get { return shortName; } }
        public override string ShortType { get { return shortType; } }
        public override string Referer { get { return referer; } }
        public override string SubReferer { get { return ShortName; } }

        //public bool ShowExplicit { get; set; }

        //public virtual bool IsSupportCount { get { return true; } }
        //public virtual bool IsSupportScore { get { return true; } }
        //public virtual bool IsSupportRes { get { return true; } }
        //public virtual bool IsSupportPreview { get { return true; } }
        //public virtual bool IsSupportJpeg { get { return true; } }
        //public virtual bool IsSupportTag { get { return true; } }

        //public virtual System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(SWIDTH, SWIDTH); } }
        //public virtual System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(SWIDTH, SHEIGHT); } }

        /// <summary>
        /// get images sync
        /// </summary>
        //public virtual List<Img> GetImages(int page, int count, string keyWord, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, System.Net.IWebProxy proxy, bool showExplicit)
        //{
        //    return GetImages(GetPageString(page, count, keyWord, proxy), maskScore, maskRes, lastViewed, maskViewed, proxy, showExplicit);
        //}

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            string url = string.Format(siteUrl, needMinus ? page - 1 : page, count, keyWord);
            if (keyWord.Length == 0)
            {
                url = url.Substring(0, url.Length - 6);
            }

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;

            web.Encoding = Encoding.UTF8;
            string pageString = web.DownloadString(url);
            web.Dispose();

            return pageString;
        }

        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            BooruProcessor nowSession = new BooruProcessor(srcType);
            return nowSession.ProcessPage(siteUrl, pageString);
        }

        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();

            string url = string.Format(tagUrl, 8, word);
            MyWebClient web = new MyWebClient();
            web.Timeout = 8;
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;

            string xml = web.DownloadString(url);


            //<?xml version="1.0" encoding="UTF-8"?>
            //<tags type="array">
            //  <tag type="3" ambiguous="false" count="955" name="neon_genesis_evangelion" id="270"/>
            //  <tag type="3" ambiguous="false" count="335" name="angel_beats!" id="26272"/>
            //  <tag type="3" ambiguous="false" count="214" name="galaxy_angel" id="243"/>
            //  <tag type="3" ambiguous="false" count="58" name="wrestle_angels_survivor_2" id="34664"/>
            //</tags>

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
