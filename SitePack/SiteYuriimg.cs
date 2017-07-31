using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using MoeLoaderDelta;
using System.Linq;

namespace SitePack
{
    /// <summary>
    /// yuriimg.com
    /// </summary>
    class SiteYuriimg : AbstractImageSite
    {
        public override string SiteUrl { get { return "http://yuriimg.com"; } }
        public override string ShortName { get { return "yuriimg"; } }
        public override string SiteName { get { return "yuriimg.com"; } }
        public override string ShortType { get { return ""; } }
        public override bool IsSupportCount { get { return false; } }
        public override string Referer { get { return "https://yuriimg.com"; } }
        public override bool IsSupportTag { get { return false; } }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            //http://yuriimg.com/post/?.html
            string url = SiteUrl + "/post/" + page + ".html";
            // string url = "http://yuriimg.com/show/ge407xd5o.jpg";

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //http://yuriimg.com/search/index/tags/?/p/?.html
                url = SiteUrl + "/search/index/tags/" + keyWord + "/p/" + page + ".html";
            }
            string pageString = web.DownloadString(url);
            web.Dispose();

            return pageString;
        }

        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            List<Img> list = new List<Img>();

            HtmlDocument dococument = new HtmlDocument();
            dococument.LoadHtml(pageString);
            HtmlNodeCollection imageItems = dococument.DocumentNode.SelectNodes("//*[@class='image-list cl']");
            if (imageItems == null)
            {
                return list;
            }
            foreach (HtmlNode imageItem in imageItems)
            {
                HtmlNode imgNode = imageItem.SelectSingleNode("./div[1]/img");
                string detailUrl = SiteUrl + imgNode.Attributes["data-href"].Value;
                string tags = imgNode.Attributes["alt"].Value;
                int id = StringToInt(imgNode.Attributes["id"].Value);
                Img item = new Img()
                {
                    Height = Convert.ToInt32(imageItem.SelectSingleNode("//div[@class='image']").Attributes["data-height"].Value),
                    Width = Convert.ToInt32(imageItem.SelectSingleNode("//div[@class='image']").Attributes["data-width"].Value),
                    IsExplicit = false,
                    Tags = tags,
                    Desc = tags,
                    PreviewUrl = imgNode.Attributes["data-original"].Value,
                    Id = id,
                    DetailUrl = detailUrl
                };

                item.DownloadDetail = (i, p) =>
                {
                    //string html = new MyWebClient { Proxy = p, Encoding = Encoding.UTF8 }.DownloadString(i.DetailUrl);
                    //HtmlDocument doc = new HtmlDocument();
                    //doc.LoadHtml(html);
                };
                list.Add(item);
            }

            return list;
        }

        private int StringToInt(string id)
        {
            string str = id.Trim();                            // 去掉字符串首尾处的空格
            char[] charBuf = str.ToArray();                    // 将字符串转换为字符数组
            ASCIIEncoding charToASCII = new ASCIIEncoding();
            byte[] TxdBuf = new byte[charBuf.Length];          // 定义发送缓冲区；
            TxdBuf = charToASCII.GetBytes(charBuf);
            int idOut = BitConverter.ToInt32(TxdBuf, 0);
            return idOut;
        }
    }
}