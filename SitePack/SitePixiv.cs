using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using MoeLoader;
using System.Text.RegularExpressions;

namespace SitePack
{
    public class SitePixiv : AbstractImageSite
    {
        //标签, 作者id, 日榜, 周榜, 月榜
        public enum PixivSrcType { Tag, Author, Day, Week, Month, }

        public override string SiteUrl { get { return "https://www.pixiv.net"; } }
        public override string SiteName
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return "www.pixiv.net [User]";
                else if (srcType == PixivSrcType.Day)
                    return "www.pixiv.net [Day]";
                else if (srcType == PixivSrcType.Week)
                    return "www.pixiv.net [Week]";
                else if (srcType == PixivSrcType.Month)
                    return "www.pixiv.net [Month]";
                else return "www.pixiv.net [Tag]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return "作者搜索";
                else if (srcType == PixivSrcType.Day)
                    return "本日排行";
                else if (srcType == PixivSrcType.Week)
                    return "本周排行";
                else if (srcType == PixivSrcType.Month)
                    return "本月排行";
                else return "最新作品 & 标签搜索";
            }
        }
        public override string ShortType
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return " [U]";
                else if (srcType == PixivSrcType.Day)
                    return " [D]";
                else if (srcType == PixivSrcType.Week)
                    return " [W]";
                else if (srcType == PixivSrcType.Month)
                    return " [M]";
                else return " [T]";
            }
        }
        public override string ShortName { get { return "pixiv"; } }
        public override string Referer { get { return referer; } }
        public override string SubReferer { get { return ShortName + ",pximg"; } }

        public override bool IsSupportCount { get { return false; } } //fixed 20
        //public override bool IsSupportScore { get { return false; } }
        public override bool IsSupportRes { get { return false; } }
        //public override bool IsSupportPreview { get { return true; } }
        //public override bool IsSupportTag { get { if (srcType == PixivSrcType.Author) return true; else return false; } }
        public override bool IsSupportTag { get { return false; } }

        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(150, 150); } }
        //public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(150, 150); } }

        private static string cookie = "";
        private string[] user = { "moe1user", "moe3user", "a-rin-a" };
        private string[] pass = { "630489372", "1515817701", "2422093014" };
        private Random rand = new Random();
        private PixivSrcType srcType = PixivSrcType.Tag;
        private string referer = "https://www.pixiv.net/";

        /// <summary>
        /// pixiv.net site
        /// </summary>
        public SitePixiv(PixivSrcType srcType)
        {
            this.srcType = srcType;
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            //if (page > 1000) throw new Exception("页码过大，若需浏览更多图片请使用关键词限定范围");
            Login(proxy);

            //http://www.pixiv.net/new_illust.php?p=2
            string url = SiteUrl + "/new_illust.php?p=" + page;

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Headers["Cookie"] = cookie;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2
                url = SiteUrl + "/search.php?s_mode=s_tag&word=" + keyWord + "&order=date_d&p=" + page;
            }
            if (srcType == PixivSrcType.Author)
            {
                int memberId = 0;
                if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out memberId))
                {
                    throw new Exception("必须在关键词中指定画师 id；若需要使用标签进行搜索请使用 www.pixiv.net [TAG]");
                }
                //member id
                url = SiteUrl + "/member_illust.php?id=" + memberId + "&p=" + page;
            }
            else if (srcType == PixivSrcType.Day)
            {
                url = SiteUrl + "/ranking.php?mode=daily&p=" + page;
            }
            else if (srcType == PixivSrcType.Week)
            {
                url = SiteUrl + "/ranking.php?mode=weekly&p=" + page;
            }
            else if (srcType == PixivSrcType.Month)
            {
                url = SiteUrl + "/ranking.php?mode=monthly&p=" + page;
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
            HtmlNodeCollection nodes = null;
            if (srcType == PixivSrcType.Tag)
            {
                nodes = doc.DocumentNode.SelectSingleNode("//ul[@class='_image-items autopagerize_page_element']").SelectNodes("li");
                //nodes = doc.DocumentNode.SelectSingleNode("//div[@id='wrapper']/div[2]/div[1]/section[1]/ul").SelectNodes("li");
            }
            else if (srcType == PixivSrcType.Author)
            {
                nodes = doc.DocumentNode.SelectSingleNode("//ul[@class='_image-items']").SelectNodes("li");
            }
            //else if (srcType == PixivSrcType.Day || srcType == PixivSrcType.Month || srcType == PixivSrcType.Week) //ranking
            //nodes = doc.DocumentNode.SelectSingleNode("//section[@class='ranking-items autopagerize_page_element']").SelectNodes("div");
            else
            {
                //ranking
                nodes = doc.DocumentNode.SelectNodes("//section[@class='ranking-item']");
            }

            if (nodes == null)
            {
                return imgs;
            }

            foreach (HtmlNode imgNode in nodes)
            {
                try
                {
                    HtmlNode anode = imgNode.SelectSingleNode("a");
                    if (srcType == PixivSrcType.Day || srcType == PixivSrcType.Month || srcType == PixivSrcType.Week)
                    {
                        anode = imgNode.SelectSingleNode(".//div[@class='ranking-image-item']").SelectSingleNode("a");
                    }
                    //details will be extracted from here
                    //eg. member_illust.php?mode=medium&illust_id=29561307&ref=rn-b-5-thumbnail
                    string detailUrl = anode.Attributes["href"].Value.Replace("amp;", "");
                    string previewUrl = "";
                    previewUrl = anode.SelectSingleNode(".//img").Attributes["src"].Value;

                    if (previewUrl.ToLower().Contains("images/common"))
                        previewUrl = anode.SelectSingleNode(".//img").Attributes["data-src"].Value;

                    if (previewUrl.Contains('?'))
                        previewUrl = previewUrl.Substring(0, previewUrl.IndexOf('?'));

                    //extract id from detail url
                    //string id = detailUrl.Substring(detailUrl.LastIndexOf('=') + 1);
                    string id = Regex.Match(detailUrl, @"illust_id=\d+").Value;
                    id = id.Substring(id.IndexOf('=') + 1);

                    Img img = GenerateImg(detailUrl, previewUrl, id);
                    if (img != null) imgs.Add(img);
                }
                catch
                {
                    //int i = 0;
                }
            }

            return imgs;
        }

        // DO NOT SUPPORT TAG HINT
        //public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        //{
        //    List<TagItem> re = new List<TagItem>();
        //    return re;
        //}

        private Img GenerateImg(string detailUrl, string preview_url, string id)
        {
            int intId = int.Parse(id);

            if (!detailUrl.StartsWith("http") && !detailUrl.StartsWith("/"))
                detailUrl = "/" + detailUrl;

            //convert relative url to absolute
            if (detailUrl.StartsWith("/"))
                detailUrl = SiteUrl + detailUrl;
            if (preview_url.StartsWith("/"))
                preview_url = SiteUrl + preview_url;

            referer = detailUrl;
            //string fileUrl = preview_url.Replace("_s.", ".");
            //string sampleUrl = preview_url.Replace("_s.", "_m.");

            //http://i1.pixiv.net/img-inf/img/2013/04/10/00/11/37/34912478_s.png
            //http://i1.pixiv.net/img03/img/tukumo/34912478_m.png
            //http://i1.pixiv.net/img03/img/tukumo/34912478.png

            Img img = new Img()
            {
                //Date = "N/A",
                //FileSize = file_size.ToUpper(),
                //Desc = intId + " ",
                Id = intId,
                //JpegUrl = fileUrl,
                //OriginalUrl = fileUrl,
                PreviewUrl = preview_url,
                //SampleUrl = sampleUrl,
                //Score = 0,
                //Width = width,
                //Height = height,
                //Tags = tags,
                DetailUrl = detailUrl
            };

            img.DownloadDetail = new DetailHandler((i, p) =>
            {
                //retrieve details
                MyWebClient web = new MyWebClient();
                web.Proxy = p;
                web.Encoding = Encoding.UTF8;
                web.Headers["Cookie"] = cookie;
                web.Headers["Referer"] = Referer;
                string page = web.DownloadString(i.DetailUrl);

                Regex reg = new Regex(@"^「(?<Desc>.*?)」/「(?<Author>.*?)」");
                HtmlDocument doc = new HtmlDocument();
                HtmlDocument ds = new HtmlDocument();
                doc.LoadHtml(page);

                //04/16/2012 17:44｜600×800｜SAI  or 04/16/2012 17:44｜600×800 or 04/19/2012 22:57｜漫画 6P｜SAI
                i.Date = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[1]").InnerText;
                //总点数
                i.Score = int.Parse(doc.DocumentNode.SelectSingleNode("//dd[@class='rated-count']").InnerText);
                //「カルタ＆わたぬき」/「えれっと」のイラスト [pixiv]
                //标题中取名字和作者
                try
                {
                    MatchCollection mc = reg.Matches(doc.DocumentNode.SelectSingleNode("//title").InnerText);
                    i.Desc = mc[0].Groups["Desc"].Value;
                    i.Author = mc[0].Groups["Author"].Value;
                }
                catch { }
                //URLS
                //http://i2.pixiv.net/c/600x600/img-master/img/2014/10/08/06/13/30/46422743_p0_master1200.jpg
                //http://i2.pixiv.net/img-original/img/2014/10/08/06/13/30/46422743_p0.png
                i.SampleUrl = doc.DocumentNode.SelectSingleNode("//div[@class='works_display']").SelectSingleNode(".//img").Attributes["src"].Value;
                i.PreviewUrl = i.SampleUrl.Replace("600x600", "150x150");
                i.SampleUrl = i.SampleUrl.Replace("600x600", "1200x1200");
                i.JpegUrl = i.SampleUrl;
                try
                {
                    i.OriginalUrl = doc.DocumentNode.SelectSingleNode("//*[@id='wrapper']/div[2]/div").SelectSingleNode(".//img").Attributes["data-src"].Value;
                }
                catch { }
                i.OriginalUrl = i.OriginalUrl == "" ? i.JpegUrl : i.OriginalUrl;

                //600×800 or 漫画 6P
                string dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]").InnerText;
                try
                {
                    //706×1000
                    i.Width = int.Parse(dimension.Substring(0, dimension.IndexOf('×')));
                    i.Height = int.Parse(System.Text.RegularExpressions.Regex.Match(dimension.Substring(dimension.IndexOf('×') + 1), @"\d+").Value);
                }
                catch { }
                try
                {
                    if (i.Width == 0 && i.Height == 0)
                    {
                        //i.OriginalUrl = i.SampleUrl.Replace("600x600", "1200x1200");
                        //i.JpegUrl = i.OriginalUrl;
                        //manga list
                        //漫画 6P
                        string oriul = "";
                        int index = dimension.IndexOf(' ') + 1;
                        string mangaPart = dimension.Substring(index, dimension.IndexOf('P') - index);
                        int mangaCount = int.Parse(mangaPart);
                        i.Dimension = "Manga " + mangaCount + "P";
                        for (int j = 0; j < mangaCount; j++)
                        {
                            //保存漫画时优先下载原图 找不到原图则下jpg
                            try
                            {
                                page = web.DownloadString(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + j);
                                ds.LoadHtml(page);
                                oriul = ds.DocumentNode.SelectSingleNode("/html/body/img").Attributes["src"].Value;
                                img.OrignalUrlList.Add(oriul);
                                if (j == 0)
                                    img.OriginalUrl = oriul;
                            }
                            catch
                            {
                                //oriUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_p0." + ext;
                                img.OrignalUrlList.Add(i.OriginalUrl.Replace("_p0", "_p" + j));
                            }
                        }
                    }
                }
                catch { }
            });

            return img;
        }

        private void Login(System.Net.IWebProxy proxy)
        {
            HtmlDocument hdoc = new HtmlDocument();
            SessionClient Sweb = new SessionClient();

            if (!cookie.Contains("pixiv"))
            {
                try
                {
                    cookie = "";
                    string data = "";
                    string post_key = "";
                    int index = rand.Next(0, user.Length);

                    //请求1 获取post_key
                    data = Sweb.Get("https://accounts.pixiv.net/login?lang=zh&source=pc&view_type=page&ref=wwwtop_accounts_index", proxy, Encoding.GetEncoding("UTF-8"));
                    hdoc.LoadHtml(data);
                    post_key = hdoc.DocumentNode.SelectSingleNode("//input[@name='post_key']").Attributes["value"].Value;
                    if (post_key.Length < 9)
                        throw new Exception("自动登录失败");

                    //请求2 POST取登录Cookie
                    data = "pixiv_id=" + user[index] + "&password=" + pass[index] + "&captcha=&g_recaptcha_response=&post_key=" + post_key + "&source=pc&ref=wwwtop_accounts_index&return_to=http%3A%2F%2Fwww.pixiv.net%2F";
                    data = Sweb.Post("https://accounts.pixiv.net/api/login?lang=zh", data, proxy, Encoding.GetEncoding("UTF-8"));
                    cookie = Sweb.GetURLCookies(SiteUrl);

                    if (cookie.Length < 9)
                        throw new Exception("自动登录失败");
                    else
                        cookie = "pixiv;" + cookie;

                }
                catch (Exception e)
                {
                    throw new Exception(e.Message.TrimEnd("。".ToCharArray()) + "或无法连接到远程服务器");
                }
            }
        }


    }
}
