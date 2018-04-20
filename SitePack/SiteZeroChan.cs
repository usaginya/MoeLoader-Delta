﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        //public string Referer { get { return null; } }

        public override bool IsSupportCount { get { return false; } } //fixed 24
        public override bool IsSupportScore { get { return false; } }
        // public override bool IsSupportRes { get { return false; } }
        public override bool IsSupportTag { get { return true; } }
        //public bool IsSupportRes { get { return true; } }
        //public bool IsSupportPreview { get { return true; } }
        //public bool IsSupportTag { get { return true; } }
        //public override string Referer { get { return "http://www.zerochan.net/"; } }

        public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(240, 240); } }

        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private string[] user = { "zerouser1" };
        private string[] pass = { "zeropass" };
        private string cookie = "", beforeWord = "", beforeUrl = "";
        private Random rand = new Random();

        /// <summary>
        /// zerochan.net site
        /// </summary>
        public SiteZeroChan()
        {
            CookieRestore();
        }


        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            Login(proxy);

            string pageString = "";
            string url = SiteUrl + (keyWord.Length > 0 ? "/search?q=" + keyWord + "&" : "/?") + "p=" + page;

            if (!beforeWord.Equals(keyWord, StringComparison.CurrentCultureIgnoreCase))
            {
                // 301
                WebResponse rsp = Sweb.GetWebResponse(url, proxy, SiteUrl);
                try
                {
                    beforeUrl = rsp.ResponseUri.AbsoluteUri;
                }
                catch
                {
                    throw new Exception("搜尋失敗，請檢查您輸入的關鍵字");
                }

                StreamReader sr = new StreamReader(rsp.GetResponseStream(), Encoding.UTF8);
                pageString = sr.ReadToEnd();
                sr.Close();
                rsp.Close();

                beforeWord = keyWord;
            }
            else
            {
                shc.Referer = beforeUrl;
                url = string.IsNullOrWhiteSpace(keyWord) ? url : beforeUrl + "?p=" + page;
                pageString = Sweb.Get(url, proxy, shc);
            }

            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageString);
            //retrieve all elements via xpath

            HtmlNodeCollection nodes;
            try
            {
                nodes = doc.DocumentNode.SelectSingleNode("//ul[@id='thumbs2']").SelectNodes(".//li");
            }
            catch
            {
                throw new Exception("沒有搜尋到圖片");
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
                //先加前一個，再加後一個  範圍都是00-49
                //string folder = (id % 2500 % 50).ToString("00") + "/" + (id % 2500 / 50).ToString("00");
                string sample_url = previewUrl.Replace("240", "600");
                string fileUrl = imgNode.SelectSingleNode("p//img").ParentNode.Attributes["href"].Value;
                string title = imgHref.Attributes["title"].Value;
                string dimension = title.Substring(0, title.IndexOf(' '));
                string fileSize = title.Substring(title.IndexOf(' ')).Trim();
                string tags = imgHref.Attributes["alt"].Value;

                Img img = GenerateImg(fileUrl, sample_url, previewUrl, dimension, tags.Trim(), fileSize, id);
                if (img != null) imgs.Add(img);
            }

            return imgs;
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            //http://www.zerochan.net/suggest?q=tony&limit=8
            List<TagItem> re = new List<TagItem>();

            string url = SiteUrl + "/suggest?limit=8&q=" + word;
            shc.Referer = url;
            string txt = Sweb.Get(url, proxy, shc);

            string[] lines = txt.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length && i < 8; i++)
            {
                //Tony Taka|Mangaka|
                if (lines[i].Trim().Length > 0)
                    re.Add(new TagItem() { Name = lines[i].Substring(0, lines[i].IndexOf('|')).Trim() });
            }

            return re;
        }

        private Img GenerateImg(string file_url, string sample_url, string preview_url, string dimension, string tags, string file_size, int id)
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
            if (sample_url.StartsWith("/"))
                sample_url = SiteUrl + sample_url;

            Img img = new Img()
            {
                //Date = "N/A",
                FileSize = file_size.ToUpper(),
                Desc = tags,
                Id = id,
                //IsViewed = isViewed,
                JpegUrl = file_url,
                OriginalUrl = file_url,
                PreviewUrl = sample_url,
                SampleUrl = preview_url,
                //Score = 0,
                //Size = width + " x " + height,
                Width = width,
                Height = height,
                //Source = "",
                Tags = tags,
                DetailUrl = SiteUrl + "/" + id,
            };

            img.FileSize = new Regex(@"\d+").Match(img.FileSize).Value;
            int fs = Convert.ToInt32(img.FileSize);
            img.FileSize = (fs > 1024 ? (fs / 1024.0).ToString("0.00MB") : fs.ToString("0KB"));

            return img;
        }

        /// <summary>
        /// 還原Cookie
        /// </summary>
        private void CookieRestore()
        {
            if (!string.IsNullOrWhiteSpace(cookie)) return;

            string ck = Sweb.GetURLCookies(SiteUrl);
            if (!string.IsNullOrWhiteSpace(ck))
                cookie = ck;
        }

        private void Login(IWebProxy proxy)
        {
            if (string.IsNullOrWhiteSpace(cookie) || !cookie.Contains("zeroc"))
            {
                try
                {
                    int index = rand.Next(0, user.Length);
                    string loginurl = "https://www.zerochan.net/login";

                    shc.Referer = loginurl;
                    Sweb.Post(
                        loginurl,
                        "ref=%2F&login=Login&name=" + user[index] + "&password=" + pass[index],
                        proxy, shc);

                    cookie = Sweb.GetURLCookies(SiteUrl);

                    if (string.IsNullOrWhiteSpace(cookie) || !cookie.Contains("z_hash"))
                        throw new Exception("登入失敗");
                    else
                        cookie = "zeroc;" + cookie;

                }
                catch (WebException)
                {
                    //invalid user will encounter 404
                    throw new Exception("訪問伺服器失敗");
                }
            }
        }


    }
}
