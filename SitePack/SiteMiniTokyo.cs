using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MoeLoaderDelta;

namespace SitePack
{
    public class SiteMiniTokyo : AbstractImageSite
    {
        public override string SiteUrl { get { return "http://www.minitokyo.net"; } }
        public override string SiteName
        {
            get
            {
                if (type == WALL)
                    return "www.minitokyo.net [Wallpaper]";
                else return "www.minitokyo.net [Scan]";
            }
        }
        public override string ShortName { get { return "minitokyo"; } }
        public override string ShortType
        {
            get
            {
                if (type == WALL)
                {
                    return "[W]";
                }
                return "[S]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (type == WALL)
                    return "搜索壁纸";
                return "搜索扫描图";
            }
        }
        //public string Referer { get { return null; } }

        public override bool IsSupportCount { get { return false; } } //fixed 24
        //public override bool IsSupportScore { get { return false; } }
        //public bool IsSupportRes { get { return true; } }
        //public bool IsSupportPreview { get { return true; } }
        //public bool IsSupportTag { get { return true; } }

        private string[] user = { "miniuser2", "miniuser3" };
        private readonly string[] pass = { "minipass", "minipass3" };
        private Random rand = new Random();
        //scans wallpapers
        private string type;
        private string cookie;

        private const string WALL = "wallpapers";
        private const string SCAN = "scans";

        SessionClient Sweb = new SessionClient();
        SessionHeadersCollection shc = new SessionHeadersCollection();

        /// <summary>
        /// minitokyo site
        /// <param name="type">1 wallpaper 2 scan</param>
        /// </summary>
        public SiteMiniTokyo(int type)
        {
            if (type == 1)
                this.type = WALL;
            else
                this.type = SCAN;

            shc.Referer = SiteUrl;
            shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
            shc.AutomaticDecompression = DecompressionMethods.GZip;
        }

        /// <summary>
        /// get images sync
        /// </summary>
        //public List<Img> GetImages(int page, int count, string keyWord, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, System.Net.IWebProxy proxy, bool showExplicit)
        //{
        //    return GetImages(GetPageString(page, count, keyWord, proxy), maskScore, maskRes, lastViewed, maskViewed, proxy, showExplicit);
        //}

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            Login(proxy);

            //http://gallery.minitokyo.net/scans?order=id&display=extensive&page=2
            string url = "http://gallery.minitokyo.net/" + type + "?order=id&display=extensive&page=" + page, pageString;

            shc.Accept = SessionHeadersValue.AcceptTextHtml;
            shc.ContentType = SessionHeadersValue.AcceptTextHtml;

            if (keyWord.Length > 0)
            {
                //先使用关键词搜索，Referer返回实际地址
                //http://www.minitokyo.net/search?q=haruhi
                url = SiteUrl + "/search?q=" + keyWord;
                pageString = Sweb.Get(url, proxy, shc);

                int urlIndex = pageString.IndexOf("http://browse.minitokyo.net/gallery?tid=");

                url = pageString.Substring(urlIndex, pageString.IndexOf('"', urlIndex) - urlIndex - 1) + (type.Contains(WALL) ? "1" : "3");
                url += "&order=id&display=extensive&page=" + page;
                url = url.Replace("&amp;", "&");
            }

            pageString = Sweb.Get(url, proxy, shc);

            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            try
            {
                List<Img> imgs = new List<Img>();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(pageString);
                //retrieve all elements via xpath
                HtmlNode wallNode = doc.DocumentNode.SelectSingleNode("//ul[@class='wallpapers']");
                HtmlNodeCollection imgNodes = wallNode.SelectNodes(".//li");
                if (imgNodes == null)
                {
                    return imgs;
                }

                for (int i = 0; i < imgNodes.Count - 1; i++)
                {
                    //最后一个是空的，跳过
                    HtmlNode imgNode = imgNodes[i];

                    string detailUrl = imgNode.SelectSingleNode("a").Attributes["href"].Value;
                    string id = detailUrl.Substring(detailUrl.LastIndexOf('/') + 1);
                    HtmlNode imgHref = imgNode.SelectSingleNode(".//img");
                    string sampleUrl = imgHref.Attributes["src"].Value;
                    //http://static2.minitokyo.net/thumbs/24/25/583774.jpg preview
                    //http://static2.minitokyo.net/view/24/25/583774.jpg   sample
                    //http://static.minitokyo.net/downloads/24/25/583774.jpg   full
                    string previewUrl = "http://static2.minitokyo.net/view" + sampleUrl.Substring(sampleUrl.IndexOf('/', sampleUrl.IndexOf(".net/") + 5));
                    string fileUrl = "http://static.minitokyo.net/downloads" + previewUrl.Substring(previewUrl.IndexOf('/', previewUrl.IndexOf(".net/") + 5));

                    // \n\tMasaru -\n\tMasaru \n\tSubmitted by\n\t\tadri24rukiachan\n\t4200x6034, 4 Favorites\n
                    string info = imgNode.SelectSingleNode(".//div").InnerText;
                    Match infomc = Regex.Match(info, @"^\n\t(?<tags>.*?)\s-\n.*?\n\t.*?by\n\t\t(?<author>.*?)\n\t(?<size>\d+x\d+),\s(?<score>\d+)\s");
                    string tags = infomc.Groups["tags"].Value;
                    string author = infomc.Groups["author"].Value;
                    string size = infomc.Groups["size"].Value;
                    string score = infomc.Groups["score"].Value;

                    Img img = GenerateImg(
                        fileUrl, previewUrl, size,
                        tags, author, sampleUrl,
                        score, id, detailUrl
                        );

                    if (img != null) imgs.Add(img);
                }
                return imgs;
            }
            catch
            {
                throw new Exception("没有找到图片哦～ .=ω=");
            }
        }

        private Img GenerateImg(
            string file_url, string preview_url, string size,
            string tags, string author, string sample_url,
            string scorestr, string id, string detailUrl
            )
        {
            int intId = int.Parse(id);

            int width = 0, height = 0, score = 0;
            try
            {
                //706x1000
                width = int.Parse(size.Substring(0, size.IndexOf('x')));
                height = int.Parse(size.Substring(size.IndexOf('x') + 1));
                score = int.Parse(scorestr);
            }
            catch { }

            Img img = new Img()
            {
                Date = "",
                FileSize = "",
                Desc = tags,
                Id = intId,
                Author = author,
                //IsViewed = isViewed,
                JpegUrl = file_url,
                OriginalUrl = file_url,
                PreviewUrl = preview_url,
                SampleUrl = sample_url,
                Score = score,
                //Size = width + " x " + height,
                Width = width,
                Height = height,
                //Source = "",
                Tags = tags,
                DetailUrl = detailUrl
            };
            return img;
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            //http://www.minitokyo.net/suggest?q=haruhi&limit=8
            List<TagItem> re = new List<TagItem>();

            string url = SiteUrl + "/suggest?limit=8&q=" + word;

            shc.Accept = SessionHeadersValue.AcceptTextHtml;
            shc.ContentType = SessionHeadersValue.AcceptTextHtml;

            string txt = Sweb.Get(url, proxy, shc);

            string[] lines = txt.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length && i < 8; i++)
            {
                //The Melancholy of Suzumiya Haruhi|Series|Noizi Ito
                if (lines[i].Trim().Length > 0)
                    re.Add(new TagItem() { Name = lines[i].Substring(0, lines[i].IndexOf('|')).Trim() });
            }

            return re;
        }

        private void Login(IWebProxy proxy)
        {
            if (!string.IsNullOrWhiteSpace(cookie) && cookie.Contains("minitokyo")) return;
            try
            {
                int index = rand.Next(0, user.Length);
                string loginurl = "http://my.minitokyo.net/login";
                string postDat = "username=" + user[index] + "&password=" + pass[index];

                shc.Accept = SessionHeadersValue.AcceptAppJson;
                shc.ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;
                Sweb.Post(loginurl, postDat, proxy, shc);
                cookie = Sweb.GetURLCookies(SiteUrl);
                if (string.IsNullOrWhiteSpace(cookie) || !cookie.Contains("minitokyo_hash"))
                {
                    throw new Exception("自动登录失败");
                }
            }
            catch (WebException)
            {
                //invalid user will encounter 404
                throw new Exception("自动登录失败");
            }
        }
    }
}
