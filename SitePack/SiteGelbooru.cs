using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using MoeLoaderDelta;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace SitePack
{
    /// <summary>
    /// Gelbooru.com
    /// Fixed 180326
    /// </summary>
    class SiteGelbooru : AbstractImageSite
    {
        private SiteBooru booru;
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        public override string SiteUrl { get { return "https://gelbooru.com"; } }
        public override string SiteName { get { return "gelbooru.com"; } }
        public override string ShortName { get { return "gelbooru"; } }
        //public override bool IsSupportCount { get { return false; } }

        public SiteGelbooru()
        {
            booru = new SiteBooru(
                SiteUrl, "", SiteUrl + "/index.php?page=dapi&s=tag&q=index&order=name&limit={0}&name={1}"
                , SiteName, ShortName, Referer, true, BooruProcessor.SourceType.XML);
        }

        private bool GetAPImode(string pageString)
        {
            return pageString.Contains("<post");
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            // API
            booru.siteUrl = SiteUrl + "/index.php?page=dapi&s=post&q=index&pid={0}&limit={1}&tags={2}";
            string pageString = booru.GetPageString(page, count, keyWord, proxy);
            if (GetAPImode(pageString)) return pageString;

            // Html
            booru.siteUrl = string.Format(SiteUrl + "/index.php?page=post&s=list&pid={0}&tags={1}", (page - 1) * 42, keyWord);
            booru.siteUrl = keyWord.Length < 1 ? booru.siteUrl.Substring(0, booru.siteUrl.Length - 6) : booru.siteUrl;
            pageString = booru.GetPageString(page, 0, keyWord, proxy);
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
                string url = string.Format(SiteUrl + "/index.php?page=autocomplete&term={0}", word);
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
            if (GetAPImode(pageString))
            {
                list = booru.GetImages(pageString, proxy);
                return list;
            }

            //Html
            if (pageString.Length < 20) { throw new Exception(pageString); }
            if (!pageString.Contains("<body")) return list;
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(pageString);
            HtmlNodeCollection previewNodes = document.DocumentNode.SelectNodes("//div[@class=\"thumbnail-preview\"]");
            if (previewNodes == null)
                return list;
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
                    PreviewUrl = node2.Attributes["data-original"].Value
                    //PreviewUrl = node1.InnerHtml.Substring(node1.InnerHtml.IndexOf("original=\"") + 10,
                    //        node1.InnerHtml.IndexOf("\" src") - node1.InnerHtml.IndexOf("original=\"") - 10)
                };
                item.DownloadDetail = (i, p) =>
                {
                    string html = new MyWebClient { Proxy = p, Encoding = Encoding.UTF8 }.DownloadString(i.DetailUrl);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    HtmlNodeCollection liNodes = doc.DocumentNode.SelectNodes("//li");
                    HtmlNode imgData = doc.DocumentNode.SelectSingleNode("//*[@id=\"image\"]");
                    if (imgData != null)
                    {
                        i.Width = Convert.ToInt32(imgData.Attributes["data-original-width"].Value);
                        i.Height = Convert.ToInt32(imgData.Attributes["data-original-height"].Value);
                        i.SampleUrl = imgData.Attributes["src"].Value;
                    }
                    foreach (HtmlNode n in liNodes)
                    {
                        if (n.InnerText.Contains("Posted"))
                            i.Date = n.InnerText.Substring(n.InnerText.IndexOf("ed: ") + 3, n.InnerText.IndexOf(" by") - n.InnerText.IndexOf("d: ") - 3);
                        if (n.InnerHtml.Contains("by"))
                            i.Author = n.InnerText.Substring(n.InnerText.LastIndexOf(' ') + 1, n.InnerText.Length - n.InnerText.LastIndexOf(' ') - 1);
                        if (n.InnerText.Contains("Source"))
                            i.Source = n.SelectSingleNode("//*[@rel=\"nofollow\"]").Attributes["href"].Value;
                        i.IsExplicit = !(n.InnerText.Contains("Rating") && n.InnerText.Contains("Safe"));
                        if (n.InnerText.Contains("Score"))
                            i.Score = Convert.ToInt32(n.SelectSingleNode("./span").InnerText);
                        if (n.InnerHtml.Contains("Original"))
                            i.OriginalUrl = i.JpegUrl = n.SelectSingleNode("./a").Attributes["href"].Value;
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
