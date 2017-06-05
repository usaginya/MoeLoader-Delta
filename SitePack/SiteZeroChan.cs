using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using MoeLoaderDelta;

namespace SitePack
{
    public class SiteZeroChan : AbstractImageSite
    {
        public override string SiteUrl { get { return "http://www.zerochan.net"; } }
        public override string SiteName
        {
            get
            {
                return "www.zerochan.net";
            }
        }
        public override string ShortName { get { return "zerochan"; } }
        public override string ShortType { get { return ""; } }
        //public string Referer { get { return null; } }

        public override bool IsSupportCount { get { return false; } } //fixed 24
        public override bool IsSupportScore { get { return false; } }
        //public bool IsSupportRes { get { return true; } }
        //public bool IsSupportPreview { get { return true; } }
        //public bool IsSupportTag { get { return true; } }
        //public override string Referer { get { return "http://www.zerochan.net/"; } }

        public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(240, 240); } }
        public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(240, 240); } }

        private string[] user = { "zerouser1" };
        private string[] pass = { "zeropass" };
        private string sessionId;
        private Random rand = new Random();

        /// <summary>
        /// zerochan.net site
        /// </summary>
        public SiteZeroChan()
        {
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

            string url = SiteUrl + "/?p=" + page;

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Headers["Cookie"] = sessionId;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //先使用关键词搜索，然后HTTP 301返回实际地址
                //http://www.zerochan.net/search?q=tony+taka
                url = SiteUrl + "/search?q=" + keyWord;
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "GET";
                //prevent 301
                req.AllowAutoRedirect = false;
                System.Net.WebResponse rsp = req.GetResponse();
                //http://www.zerochan.net/Tony+Taka?p=1
                //HTTP 301然后返回实际地址
                string location = rsp.Headers["Location"];
                rsp.Close();
                if (location != null && location.Length > 0)
                {
                    //非完整地址，需要前缀
                    url = SiteUrl + location + "?p=" + page;
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
            HtmlNodeCollection nodes = doc.DocumentNode.SelectSingleNode("//ul[@id='thumbs2']").SelectNodes(".//li");
            if (nodes == null)
            {
                return imgs;
            }

            foreach (HtmlNode imgNode in nodes)
            {
                //   /12123123
                string strId = imgNode.SelectSingleNode("a").Attributes["href"].Value;
                int id = int.Parse(strId.Substring(1));
                HtmlNode imgHref = imgNode.SelectSingleNode(".//img");
                string previewUrl = imgHref.Attributes["src"].Value;
                //http://s3.zerochan.net/Morgiana.240.1355397.jpg   preview
                //http://s3.zerochan.net/Morgiana.600.1355397.jpg    sample
                //http://static.zerochan.net/Morgiana.full.1355397.jpg   full
                //先加前一个，再加后一个  范围都是00-49
                //string folder = (id % 2500 % 50).ToString("00") + "/" + (id % 2500 / 50).ToString("00");
                string sample_url = previewUrl.Replace("240", "600");
                string fileUrl = "http://static.zerochan.net" + previewUrl.Substring(previewUrl.IndexOf('/', 8)).Replace("240", "full");
                string title = imgHref.Attributes["title"].Value;
                string dimension = title.Substring(0, title.IndexOf(' '));
                string fileSize = title.Substring(title.IndexOf(' ')).Trim();
                string tags = imgHref.Attributes["alt"].Value;

                Img img = GenerateImg(fileUrl, previewUrl, sample_url, dimension, tags.Trim(), fileSize, id);
                if (img != null) imgs.Add(img);
            }

            return imgs;
        }

        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            //http://www.zerochan.net/suggest?q=tony&limit=8
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
                //Tony Taka|Mangaka|
                if (lines[i].Trim().Length > 0)
                    re.Add(new TagItem() { Name = lines[i].Substring(0, lines[i].IndexOf('|')).Trim(), Count = "N/A" });
            }
          
            return re;
        }

        private Img GenerateImg(string file_url, string preview_url, string sample_url, string dimension, string tags, string file_size, int id)
        {
            //int intId = int.Parse(id.Substring(1));

            int width = 0, height = 0;
            try
            {
                //706x1000
                width = int.Parse(dimension.Substring(0, dimension.IndexOf('x')));
                height = int.Parse(dimension.Substring(dimension.IndexOf('x') + 1));
            }
            catch { }

            //convert relative url to absolute
            if (file_url.StartsWith("/"))
                file_url = SiteUrl + file_url;
            if (preview_url.StartsWith("/"))
                preview_url = SiteUrl + preview_url;

            Img img = new Img()
            {
                //Date = "N/A",
                FileSize = file_size.ToUpper(),
                Desc = tags,
                Id = id,
                //IsViewed = isViewed,
                JpegUrl = file_url,
                OriginalUrl = file_url,
                PreviewUrl = preview_url,
                SampleUrl = sample_url,
                //Score = 0,
                //Size = width + " x " + height,
                Width = width,
                Height = height,
                //Source = "",
                Tags = tags,
                DetailUrl = SiteUrl + "/" + id,
            };
            return img;
        }

        private void Login(System.Net.IWebProxy proxy)
        {
            if (sessionId != null) return;
            try
            {
                int index = rand.Next(0, user.Length);
                //http://my.minitokyo.net/login
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://www.zerochan.net/login");
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "POST";
                //prevent 303 See Other
                req.AllowAutoRedirect = false;

                byte[] buf = Encoding.UTF8.GetBytes("ref=%2F&login=Login&name=" + user[index] + "&password=" + pass[index]);
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buf.Length;
                System.IO.Stream str = req.GetRequestStream();
                str.Write(buf, 0, buf.Length);
                str.Close();
                System.Net.WebResponse rsp = req.GetResponse();

                //HTTP 303然后返回地址 /
                sessionId = rsp.Headers.Get("Set-Cookie");
                //z_id=187999; expires=Fri, 07-Sep-2012 15:59:04 GMT; path=/; domain=.zerochan.net, z_hash=23c10fa5869459ce402ba466c1cbdb6a; expires=Fri, 07-Sep-2012 15:59:04 GMT; path=/; domain=.zerochan.net
                if (sessionId == null || !sessionId.Contains("z_hash"))
                {
                    throw new Exception("自动登录失败");
                }
                //z_id=376440; z_hash=978bb6cb9e0aeac077dcc6032f2e9f3d
                int idIndex = sessionId.IndexOf("z_id");
                string idstr = sessionId.Substring(idIndex, sessionId.IndexOf(';', idIndex) + 2 - idIndex);
                idIndex = sessionId.IndexOf("z_hash");
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
