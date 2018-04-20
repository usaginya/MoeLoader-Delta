using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using MoeLoaderDelta;

namespace SitePack
{
    /// <summary>
    /// danbooru.donmai.us, Thanks to Realanan
    /// </summary>
    class SiteDanbooru : AbstractImageSite
    {
        public override string SiteUrl { get { return "https://danbooru.donmai.us"; } }
        //http://donmai.us/post?page={0}&limit={1}&tags={2}
        public override string SiteName { get { return "danbooru.donmai.us"; } }
        public override string ShortName { get { return "donmai"; } }

        public override bool IsSupportTag { get { return false; } }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            string address = string.Format(SiteUrl + "/posts?page={0}&limit={1}&tags={2}", page, count, keyWord);
            if (keyWord.Length == 0)
            {
                address = address.Substring(0, address.Length - 6);
            }
            MyWebClient client = new MyWebClient
            {
                Proxy = proxy,
                Encoding = Encoding.UTF8
            };
            string pageString = client.DownloadString(address);
            client.Dispose();
            return pageString;
        }

        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            List<Img> list = new List<Img>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(pageString);
            HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//article");
            if (nodes == null)
            {
                return list;
            }

            foreach (HtmlNode node in nodes)
            {
                HtmlNode node2 = node.SelectSingleNode("a");
                HtmlNode node3 = node2.SelectSingleNode("img");

                string detailUrl = FormattedImgUrl(node2.Attributes["href"].Value);

                Img item = new Img()
                {
                    Desc = node.Attributes["data-tags"].Value,
                    Height = Convert.ToInt32(node.Attributes["data-height"].Value),
                    Id = Convert.ToInt32(node.Attributes["data-id"].Value),
                    Author = node.Attributes["data-uploader"].Value,
                    IsExplicit = node.Attributes["data-rating"].Value == "e",
                    Tags = node.Attributes["data-tags"].Value,
                    Width = Convert.ToInt32(node.Attributes["data-width"].Value),
                    PreviewUrl = FormattedImgUrl(node3.Attributes["src"].Value),
                    DetailUrl = detailUrl
                };


                item.DownloadDetail = (i, p) =>
                {
                    string html = new MyWebClient { Proxy = p, Encoding = Encoding.UTF8 }.DownloadString(i.DetailUrl);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    HtmlNodeCollection sectionNodes = doc.DocumentNode.SelectNodes("//section");
                    bool haveurl = false, iszip = false;
                    foreach (HtmlNode n in sectionNodes)
                    {
                        var ns = n.SelectNodes(".//li");
                        if (ns == null) continue;
                        foreach (HtmlNode n1 in ns)
                        {
                            if (n1.InnerText.Contains("Date:"))
                            {
                                i.Date = n1.SelectSingleNode(".//time").Attributes["title"].Value;
                            }
                            if (n1.InnerText.Contains("Size:"))
                            {
                                haveurl = true;
                                i.OriginalUrl = FormattedImgUrl(n1.SelectSingleNode(".//a").Attributes["href"].Value);
                                i.JpegUrl = i.OriginalUrl;
                                i.FileSize = n1.SelectSingleNode(".//a").InnerText;
                                i.Dimension = n1.InnerText.Substring(n1.InnerText.IndexOf('(') + 1, n1.InnerText.LastIndexOf(')') - n1.InnerText.IndexOf('(') - 1);
                            }
                            if (n1.InnerText.Contains("Score:"))
                            {
                                i.Score = Convert.ToInt32(n1.SelectSingleNode(".//span").InnerText);
                            }
                        }
                        //原圖地址不應該是zip
                        if (haveurl && i.OriginalUrl.Substring(i.OriginalUrl.LastIndexOf("."), i.OriginalUrl.Length - i.OriginalUrl.LastIndexOf(".")).ToLower() == ".zip" &&
                            n.InnerText.Contains("Save this video"))
                        {
                            iszip = true;
                            HtmlNode n2 = n.SelectSingleNode("//section[@id='image-container']");
                            i.OriginalUrl = FormattedImgUrl(n2.Attributes["data-large-file-url"].Value);
                            i.JpegUrl = i.OriginalUrl;
                        }
                    }
                    i.SampleUrl = iszip ? i.JpegUrl : FormattedImgUrl(doc.DocumentNode.SelectSingleNode("//img[@id='image']").Attributes["src"].Value);
                };
                list.Add(item);
            }

            return list;
        }

        private string FormattedImgUrl(string prUrl)
        {
            try
            {
                if (prUrl.StartsWith("/"))
                    prUrl = SiteUrl + prUrl;
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
