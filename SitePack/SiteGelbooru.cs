using HtmlAgilityPack;
using MoeLoaderDelta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace SitePack
{
    /// <summary>
    /// Gelbooru.com
    /// Fixed 200819
    /// </summary>
    class SiteGelbooru : AbstractImageSite
    {
        private SiteBooru booru;
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private APImode apiMode = APImode.NULL;
        public override string SiteUrl { get { return "https://gelbooru.com"; } }
        public override string SiteName { get { return "gelbooru.com"; } }
        public override string ShortName { get { return "gelbooru"; } }
        //public override bool IsSupportCount { get { return false; } }

        public SiteGelbooru()
        {
            booru = new SiteBooru(
                SiteUrl, "", SiteUrl + "/index.php?page=dapi&s=tag&q=index&order=name&limit={0}&name={1}"
                , SiteName, ShortName, Referer, true, BooruProcessor.SourceType.JSONGelbooru);
        }
        private enum APImode
        {
            // XML 格式
            XML,
            // JSON 格式
            JSON,
            // 其他
            OTHER,
            //初始空
            NULL
        }
        private APImode GetAPImode(string pageString)
        {
            if (pageString.Contains("<post"))
                return APImode.XML;
            else if (pageString.Contains("[{"))
                return APImode.JSON;
            else
                return APImode.OTHER;
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            string pageString = string.Empty;
            // API
            // JSON
            if (apiMode == APImode.NULL || apiMode == APImode.JSON)
            {
                booru.Url = $"{SiteUrl}/index.php?page=dapi&s=post&q=index&pid={{0}}&limit={{1}}&json=1&tags={{2}}";
                pageString = booru.GetPageString(page, count, keyWord, proxy);
                if (pageString.Length < 24) { return pageString; }
                if (GetAPImode(pageString) == APImode.JSON)
                { apiMode = APImode.JSON; return pageString; }
            }
            // XML
            if (apiMode == APImode.NULL || apiMode == APImode.XML)
            {
                booru = new SiteBooru(
                SiteUrl, string.Empty, $"{SiteUrl}/index.php?page=dapi&s=tag&q=index&order=name&limit={{0}}&name={{1}}"
                , SiteName, ShortName, Referer, true, BooruProcessor.SourceType.XML)
                {
                    Url = $"{SiteUrl}/index.php?page=dapi&s=post&q=index&pid={{0}}&limit={{1}}&tags={{2}}"
                };
                pageString = booru.GetPageString(page, count, keyWord, proxy);
                if (pageString.Length < 24) { return pageString; }
                if (GetAPImode(pageString) == APImode.XML)
                { apiMode = APImode.XML; return pageString; }
            }
            // Html
            if (apiMode == APImode.NULL || apiMode == APImode.OTHER)
            {
                booru.Url = $"{SiteUrl}/index.php?page=post&s=list&pid={{0}}&tags={{1}}";
                pageString = booru.GetPageString((page - 1) * 42, 0, keyWord, proxy);
                if (pageString.Length < 24) { return pageString; }
                apiMode = APImode.OTHER;
            }
            return pageString;
        }

        //tags https://gelbooru.com/index.php?page=autocomplete&term=don
        /// <summary>
        /// JSON and API
        /// </summary>
        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();
            try
            {
                string url = $"{SiteUrl}/index.php?page=autocomplete&term={word}";
                shc.Accept = SessionHeadersValue.AcceptAppJson;
                url = Sweb.Get(url, proxy, shc);

                object[] jsonobj = (new JavaScriptSerializer()).DeserializeObject(url) as object[];

                foreach (object o in jsonobj)
                {
                    re.Add(new TagItem() { Name = o.ToString() });
                }
            }
            catch { }

            return re.Count > 0 ? re : booru.GetTags(word, proxy);
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> list = new List<Img>();
            //API
            if (GetAPImode(pageString) == APImode.JSON || GetAPImode(pageString) == APImode.XML)
            {
                list = booru.GetImages(pageString, proxy);
                return list;
            }

            //Html
            if (pageString.Length < 20) { throw new Exception(pageString); }
            if (!pageString.Contains("<body")) return list;
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(pageString);
            HtmlNodeCollection previewNodes = document.DocumentNode.SelectNodes("//div[@class=\"thumbnail-container\"]/div");
            if (previewNodes == null)
            { return list; }
            foreach (HtmlNode node in previewNodes)
            {
                HtmlNode node1 = node.SelectSingleNode("./span/a");
                HtmlNode node2 = node1.SelectSingleNode("./img");
                string detailUrl = FormattedImgUrl(node1.Attributes["href"].Value);
                string desc = Regex.Match(node1.InnerHtml, "(?<=title=\" ).*?(?=  score)").Value;
                Img item = new Img()
                {
                    Desc = desc,
                    Tags = desc,
                    Id = Convert.ToInt32(Regex.Match(node1.Attributes["id"].Value, @"\d+").Value),
                    DetailUrl = detailUrl,
                    SampleUrl = node2.Attributes["src"].Value
                    //PreviewUrl = node1.InnerHtml.Substring(node1.InnerHtml.IndexOf("original=\"") + 10,
                    //        node1.InnerHtml.IndexOf("\" src") - node1.InnerHtml.IndexOf("original=\"") - 10)
                };
                item.DownloadDetail = (i, p) =>
                {
                    string html = Sweb.Get(i.DetailUrl, p, shc);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    HtmlNodeCollection liNodes = doc.DocumentNode.SelectNodes("//*[@id=\"tag-list\"]/div/li");
                    HtmlNode imgData = doc.DocumentNode.SelectSingleNode("//*[@id=\"image\"]");
                    if (imgData != null)
                    {
                        i.Width = Convert.ToInt32(imgData.Attributes["data-original-width"].Value);
                        i.Height = Convert.ToInt32(imgData.Attributes["data-original-height"].Value);
                        i.PreviewUrl = imgData.Attributes["src"].Value;
                    }
                    foreach (HtmlNode n in liNodes)
                    {
                        if (n.InnerText.Contains("Posted:"))
                        { i.Date = n.InnerText.Substring(n.InnerText.IndexOf("ed: ") + 3, n.InnerText.IndexOf(" by") - n.InnerText.IndexOf("d: ") - 3); }
                        if (n.InnerHtml.Contains("by"))
                        { i.Author = n.InnerText.Substring(n.InnerText.LastIndexOf(' ') + 1, n.InnerText.Length - n.InnerText.LastIndexOf(' ') - 1); }
                        if (i.Width < 1 && n.InnerText.Contains("Size:"))
                        { i.Dimension = n.InnerText.Substring(n.InnerText.LastIndexOf("Size: ") + 6); }
                        if (n.InnerText.Contains("Source:"))
                        {
                            i.Source = n.InnerHtml.Contains("<a")
                                ? n.SelectSingleNode("./a").Attributes["href"].Value
                                : n.InnerText.Substring(n.InnerText.LastIndexOf("Source: ") + 8);
                        }
                        else { i.IsExplicit = !(n.InnerText.Contains("Rating:") && n.InnerText.Contains("Safe")); }
                        if (n.InnerText.Contains("Score:"))
                        { i.Score = Convert.ToInt32(n.SelectSingleNode("./span").InnerText); }
                        else if (n.InnerHtml.Contains("Original i"))
                        { i.OriginalUrl = i.JpegUrl = n.SelectSingleNode("./a").Attributes["href"].Value; }
                    }
                };
                list.Add(item);
            }
            return list;
        }
        private static string FormattedImgUrl(string prUrl)
        {
            try
            {
                if (prUrl.StartsWith("//"))
                    prUrl = "https:" + prUrl;
                if (prUrl.Contains("&amp;"))
                    prUrl = prUrl.Replace("&amp;", "&");
                return prUrl;
            }
            catch
            {
                return prUrl;
            }
        }


    }
}
