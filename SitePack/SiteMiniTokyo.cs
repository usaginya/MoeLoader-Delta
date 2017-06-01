using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using MoeLoader;

namespace SitePack
{
    public class SiteMiniTokyo : MoeLoader.AbstractImageSite
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
        public override string ShortType { get { return ""; } }
        //public string Referer { get { return null; } }

        public override bool IsSupportCount { get { return false; } } //fixed 24
        //public override bool IsSupportScore { get { return false; } }
        //public bool IsSupportRes { get { return true; } }
        //public bool IsSupportPreview { get { return true; } }
        //public bool IsSupportTag { get { return true; } }

        public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(180, 180); } }
        public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(180, 180); } }

        private string[] user = { "miniuser2", "miniuser3" };
        private string[] pass = { "minipass", "minipass3" };
        private Random rand = new Random();
        //scans wallpapers
        private string type;
        private string sessionId;

        private const string WALL = "wallpapers";
        private const string SCAN = "scans";

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
        }

        /// <summary>
        /// get images sync
        /// </summary>
        //public List<Img> GetImages(int page, int count, string keyWord, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, System.Net.IWebProxy proxy, bool showExplicit)
        //{
        //    return GetImages(GetPageString(page, count, keyWord, proxy), maskScore, maskRes, lastViewed, maskViewed, proxy, showExplicit);
        //}

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            Login(proxy);

            //http://gallery.minitokyo.net/scans?order=id&display=extensive&page=2
            string url = "http://gallery.minitokyo.net/" + type + "?order=id&display=extensive&page=" + page;

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Headers["Cookie"] = sessionId;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //先使用关键词搜索，然后HTTP 303返回实际地址
                //http://www.minitokyo.net/search?q=haruhi
                url = SiteUrl + "/search?q=" + keyWord;
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "GET";
                //prevent 303 See Other
                req.AllowAutoRedirect = false;
                System.Net.WebResponse rsp = req.GetResponse();
                //http://www.minitokyo.net/Haruhi+Suzumiya
                //HTTP 303然后返回实际地址
                string location = rsp.Headers["Location"];
                rsp.Close();
                if (location != null && location.Length > 0)
                {
                    //非完整地址，需要前缀
                    url = SiteUrl + location;
                    //再次访问，得到真实列表页地址...
                    string html = web.DownloadString(url);
                    //http://browse.minitokyo.net/gallery?tid=2112&amp;index=1 WALL
                    //http://browse.minitokyo.net/gallery?tid=2112&amp;index=3 SCAN
                    //http://browse.minitokyo.net/gallery?tid=2112&index=1&order=id
                    int urlIndex = html.IndexOf("http://browse.minitokyo.net/gallery?tid=");
                    if (type == WALL)
                    {
                        url = html.Substring(urlIndex, html.IndexOf('"', urlIndex) - urlIndex - 1) + "1";
                    }
                    else
                    {
                        url = html.Substring(urlIndex, html.IndexOf('"', urlIndex) - urlIndex - 1) + "3";
                    }
                    //http://browse.minitokyo.net/gallery?tid=2112&amp%3Bindex=1&order=id&display=extensive
                    url += "&order=id&display=extensive&page=" + page;
                    url = url.Replace("&amp;", "&");
                }
                else
                {
                    throw new Exception("搜索失败，请检查您输入的关键词");
                }
            }

            string pageString = web.DownloadString(url);
            web.Dispose();

            return pageString;
        }

        public override List<Img> GetImages(string pageString, System.Net.IWebProxy proxy)
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
                string previewUrl = imgHref.Attributes["src"].Value;
                //http://static2.minitokyo.net/thumbs/24/25/583774.jpg preview
                //http://static2.minitokyo.net/view/24/25/583774.jpg   sample
                //http://static.minitokyo.net/downloads/24/25/583774.jpg   full
                string sampleUrl = "http://static2.minitokyo.net/view" + previewUrl.Substring(previewUrl.IndexOf('/', previewUrl.IndexOf(".net/") + 5));
                string fileUrl = "http://static.minitokyo.net/downloads" + previewUrl.Substring(previewUrl.IndexOf('/', previewUrl.IndexOf(".net/") + 5));
                //Hana Nanaroba - Hana Nanaroba
                //Submitted by YuuichiYouko 1804x2693, 2 Favorites
                string title = imgNode.SelectSingleNode(".//div").InnerText;
                title = title.Replace('\t', ' ').Replace("\n", "");
                string tags = title.Substring(0, title.IndexOf("Submitted") - 1).Trim();
                int favIndex = title.LastIndexOf(',') + 1;
                string score = title.Substring(favIndex, title.LastIndexOf(' ') - favIndex).Trim();
                title = title.Substring(0, favIndex - 1);
                favIndex = title.LastIndexOf(' ');
                if (favIndex > 0)
                {
                    title = title.Substring(favIndex + 1);
                }

                Img img = GenerateImg(fileUrl, previewUrl, title, tags, sampleUrl, score, id, detailUrl);
                if (img != null) imgs.Add(img);
            }
            return imgs;
        }

        private Img GenerateImg(string file_url, string preview_url, string dimension, string tags, string sample_url, string scorestr, string id, string detailUrl)
        {
            int intId = int.Parse(id);

            int width = 0, height = 0, score = 0;
            try
            {
                //706x1000
                width = int.Parse(dimension.Substring(0, dimension.IndexOf('x')));
                height = int.Parse(dimension.Substring(dimension.IndexOf('x') + 1));
                score = int.Parse(scorestr);
            }
            catch { }

            Img img = new Img()
            {
                Date = "",
                FileSize = "",
                Desc = tags,
                Id = intId,
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

        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            //http://www.minitokyo.net/suggest?q=haruhi&limit=8
            List<TagItem> re = new List<TagItem>();

            string url = SiteUrl + "/suggest?limit=8&q=" + word;
            MyWebClient web = new MyWebClient();
            web.Timeout = 8;
            web.Proxy = proxy;
            web.Headers["Cookie"] = sessionId;
            web.Encoding = Encoding.UTF8;

            string txt = web.DownloadString(url);

            string[] lines = txt.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length && i < 8; i++)
            {
                //The Melancholy of Suzumiya Haruhi|Series|Noizi Ito
                if (lines[i].Trim().Length > 0)
                    re.Add(new TagItem() { Name = lines[i].Substring(0, lines[i].IndexOf('|')).Trim(), Count = "N/A" });
            }

            return re;
        }

        private void Login(System.Net.IWebProxy proxy)
        {
            if (sessionId != null) return;
            try
            {  
                int index = rand.Next(0, user.Length);
                //http://my.minitokyo.net/login
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://my.minitokyo.net/login");
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "POST";
                //prevent 303 See Other
                req.AllowAutoRedirect = false;

                byte[] buf = Encoding.UTF8.GetBytes("username=" + user[index] + "&password=" + pass[index]);
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buf.Length;
                System.IO.Stream str = req.GetRequestStream();
                str.Write(buf, 0, buf.Length);
                str.Close();
                System.Net.WebResponse rsp = req.GetResponse();

                //HTTP 303然后返回地址 http://www.minitokyo.net/
                sessionId = rsp.Headers.Get("Set-Cookie");
                //minitokyo_id=376440; expires=Tue, 17-Jul-2012 07:18:32 GMT; path=/; domain=.minitokyo.net, minitokyo_hash=978bb6cb9e0aeac077dcc6032f2e9f3d; expires=Tue, 17-Jul-2012 07:18:32 GMT; path=/; domain=.minitokyo.net
                if (sessionId == null || !sessionId.Contains("minitokyo_hash"))
                {
                    throw new Exception("自动登录失败");
                }
                //minitokyo_id=376440; minitokyo_hash=978bb6cb9e0aeac077dcc6032f2e9f3d
                int idIndex = sessionId.IndexOf("minitokyo_id");
                string idstr = sessionId.Substring(idIndex, sessionId.IndexOf(';', idIndex) + 2 - idIndex);
                idIndex = sessionId.IndexOf("minitokyo_hash");
                string hashstr = sessionId.Substring(idIndex, sessionId.IndexOf(';', idIndex) - idIndex);
                sessionId = idstr + hashstr;
                rsp.Close();
            }
            catch (System.Net.WebException)
            {
                //invalid user will encounter 404
                throw new Exception("自动登录失败");
            }
        }
    }
}
