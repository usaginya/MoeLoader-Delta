using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using MoeLoaderDelta;

namespace SitePack
{
    public class SiteEshuu : AbstractImageSite
    {
        public override string SiteUrl => "http://e-shuushuu.net";
        public override string SiteName
        {
            get
            {
                if (type == 1)
                    return "e-shuushuu.net [Tag]";
                else if (type == 2)
                    return "e-shuushuu.net [Source]";
                else if (type == 3)
                    return "e-shuushuu.net [Artist]";
                else return "e-shuushuu.net [Chara]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (type == 1)
                    return "搜索标签";
                else if (type == 2)
                    return "搜索来源";
                else if (type == 3)
                    return "搜索画师";
                else return "搜索角色";
            }
        }
        public override string ShortName => "e-shu";
        public override string ShortType
        {
            get
            {
                if (type == 1)
                    return "[T]";
                else if (type == 2)
                    return "[S]";
                else if (type == 3)
                    return "[A]";
                else return "[C]";
            }
        }

        public override bool IsSupportCount => false;  //fixed 15
        public override bool IsSupportScore => false;

        public override bool IsVisible => type == 1;

        private readonly int type = 1;
        private readonly string defaultUA = SessionClient.DefUA;
        /// <summary>
        /// e-shuushuu.net site
        /// </summary>
        /// <param name="type">search keyword type, 1 tag 2 source 3 artist 4 chara</param>
        public SiteEshuu(int type)
        {
            if (type > 0 && type < 5)
                this.type = type;
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
            string url = SiteUrl + "/?page=" + page;

            MyWebClient web = new MyWebClient
            {
                Proxy = proxy,
                Encoding = Encoding.UTF8
            };

            if (keyWord.Length > 0)
            {
                url = SiteUrl + "/search/process/";
                //multi search
                string data = "tags=" + keyWord + "&source=&char=&artist=&postcontent=&txtposter=";
                if (type == 2)
                {
                    data = "tags=&source=" + keyWord + "&char=&artist=&postcontent=&txtposter=";
                }
                else if (type == 3)
                {
                    data = "tags=&source=&char=&artist=" + keyWord + "&postcontent=&txtposter=";
                }
                else if (type == 4)
                {
                    data = "tags=&source=&char=" + keyWord + "&artist=&postcontent=&txtposter=";
                }

                //e-shuushuu需要将关键词转换为tag id，然后进行搜索
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                req.UserAgent = defaultUA;
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "POST";
                //prevent 303
                req.AllowAutoRedirect = false;
                byte[] buf = Encoding.UTF8.GetBytes(data);
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buf.Length;
                System.IO.Stream str = req.GetRequestStream();
                str.Write(buf, 0, buf.Length);
                str.Close();
                System.Net.WebResponse rsp = req.GetResponse();
                //http://e-shuushuu.net/search/results/?tags=2
                //HTTP 303然后返回实际地址
                string location = rsp.Headers["Location"];
                rsp.Close();
                if (location != null && location.Length > 0)
                {
                    //非完整地址，需要前缀
                    url = rsp.Headers["Location"] + "&page=" + page;
                }
                else
                {
                    throw new Exception("没有搜索到关键词相关的图片（每个关键词前后需要加双引号如 \"sakura\"））");
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
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@class='image_thread display']");
            if (nodes == null)
            {
                return imgs;
            }
            foreach (HtmlNode imgNode in nodes)
            {
                string id = imgNode.Attributes["id"].Value;
                HtmlNode imgHref = imgNode.SelectSingleNode(".//a[@class='thumb_image']");
                string fileUrl = imgHref.Attributes["href"].Value;
                string previewUrl = imgHref.SelectSingleNode("img").Attributes["src"].Value;
                HtmlNode meta = imgNode.SelectSingleNode(".//div[@class='meta']");
                string date = meta.SelectSingleNode(".//dd[2]").InnerText;
                string fileSize = meta.SelectSingleNode(".//dd[3]").InnerText;
                string dimension = meta.SelectSingleNode(".//dd[4]").InnerText;
                string tags = "";
                try
                {
                    tags = meta.SelectSingleNode(".//dd[5]").InnerText;
                }
                catch { }

                Img img = GenerateImg(fileUrl, previewUrl, dimension, date, tags, fileSize, id);
                if (img != null) imgs.Add(img);
            }

            return imgs;
        }

        public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            //type 1 tag 2 source 3 artist | chara no type
            List<TagItem> re = new List<TagItem>();

            //chara without hint
            if (type == 4) return re;

            string url = SiteUrl + "/httpreq.php?mode=tag_search&tags=" + word + "&type=" + type;
            MyWebClient web = new MyWebClient
            {
                Timeout = 8,
                Proxy = proxy,
                Encoding = Encoding.UTF8
            };

            string txt = web.DownloadString(url);

            string[] lines = txt.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length && i < 8; i++)
            {
                if (lines[i].Trim().Length > 0)
                    re.Add(new TagItem() { Name = lines[i].Trim(), Count = "N/A" });
            }

            return re;
        }

        private Img GenerateImg(string file_url, string preview_url, string dimension, string created_at, string tags, string file_size, string id)
        {
            int intId = int.Parse(id.Substring(1));

            int width = 0, height = 0;
            try
            {
                //706x1000 (0.706 MPixel)
                dimension = dimension.Substring(0, dimension.IndexOf('(')).Trim();
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
                Date = created_at.Replace("\t", "").Replace("\n", ""),
                FileSize = file_size.Replace("\t", "").Replace("\n", "").ToUpper(),
                Desc = tags.Replace("\t", "").Replace("\n", ""),
                Id = intId,
                JpegUrl = file_url,
                OriginalUrl = file_url,
                PreviewUrl = file_url,
                SampleUrl = preview_url,
                Width = width,
                Height = height,
                Tags = tags.Replace("\t", "").Replace("\n", ""),
                DetailUrl = SiteUrl + "/image/" + intId,
            };
            return img;
        }
    }
}
