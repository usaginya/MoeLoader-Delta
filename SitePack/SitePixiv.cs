using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using MoeLoaderDelta;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Net;

namespace SitePack
{
    public class SitePixiv : AbstractImageSite
    {
        //標籤, 完整標籤, 作者id, 日榜, 周榜, 月榜
        public enum PixivSrcType { Tag, TagFull, Author, Day, Week, Month }

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
                else if (srcType == PixivSrcType.TagFull)
                    return "www.pixiv.net [TagFull]";
                return "www.pixiv.net [Tag]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return "搜尋作者";
                else if (srcType == PixivSrcType.Day)
                    return "本日排行";
                else if (srcType == PixivSrcType.Week)
                    return "本週排行";
                else if (srcType == PixivSrcType.Month)
                    return "本月排行";
                else if (srcType == PixivSrcType.TagFull)
                    return "搜尋完整標籤";
                return "最新作品 & 搜尋標籤";
            }
        }
        public override string ShortType
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return "[U]";
                else if (srcType == PixivSrcType.Day)
                    return "[D]";
                else if (srcType == PixivSrcType.Week)
                    return "[W]";
                else if (srcType == PixivSrcType.Month)
                    return "[M]";
                else if (srcType == PixivSrcType.TagFull)
                    return "[TF]";
                return "[T]";
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
        public override bool IsSupportTag { get { return true; } }

        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(150, 150); } }
        //public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(150, 150); } }

        private static string cookie = "";
        private string[] user = { "moe1user", "moe3user", "a-rin-a" };
        private string[] pass = { "630489372", "1515817701", "2422093014" };
        private Random rand = new Random();
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private PixivSrcType srcType = PixivSrcType.Tag;
        private string referer = "https://www.pixiv.net/";

        /// <summary>
        /// pixiv.net site
        /// </summary>
        public SitePixiv(PixivSrcType srcType)
        {
            this.srcType = srcType;
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            //if (page > 1000) throw new Exception("頁碼過大，若需瀏覽更多圖片請使用關鍵字限定範圍");
            Login(proxy);

            //http://www.pixiv.net/new_illust.php?p=2
            string url = SiteUrl + "/new_illust.php?p=" + page;

            if (keyWord.Length > 0)
            {
                //http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2
                url = SiteUrl + "/search.php?s_mode=s_tag"
                    + (srcType == PixivSrcType.TagFull ? "_full" : "")
                + "&word=" + keyWord + "&order=date_d&p=" + page;
            }
            if (srcType == PixivSrcType.Author)
            {
                int memberId = 0;
                if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out memberId))
                {
                    throw new Exception("必須在關鍵字中指定畫師 id；若需要使用標籤進行搜尋請使用 www.pixiv.net [TAG]");
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

            shc.Remove("X-Requested-With");
            shc.Remove("Accept-Ranges");
            shc.ContentType = SessionHeadersValue.AcceptTextHtml;
            string pageString = Sweb.Get(url, proxy, "UTF-8", shc);

            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageString);

            //retrieve all elements via xpath
            HtmlNodeCollection nodes = null;
            HtmlNode tagNode = null;
            try
            {
                if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
                {
                    tagNode = doc.DocumentNode.SelectSingleNode("//input[@id='js-mount-point-search-result-list']");
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
            }
            catch
            {
                throw new Exception("沒有找到圖片哦～ .=ω=");
            }

            if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
            {
                if (tagNode == null)
                {
                    return imgs;
                }
            }
            else if (nodes == null)
            {
                return imgs;
            }

            //Tag search js-mount-point-search-related-tags Json
            if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
            {
                string jsonData = tagNode.Attributes["data-items"].Value.Replace("&quot;", "\"");
                object[] array = (new JavaScriptSerializer()).DeserializeObject(jsonData) as object[];
                foreach (object o in array)
                {
                    Dictionary<string, object> obj = o as Dictionary<string, object>;
                    string
                        detailUrl = "",
                        previewUrl = "",
                        id = "";
                    if (obj["illustId"] != null)
                    {
                        id = obj["illustId"].ToString();
                        detailUrl = SiteUrl + "/member_illust.php?mode=medium&illust_id=" + id;
                    }
                    if (obj["url"] != null)
                    {
                        previewUrl = obj["url"].ToString();
                    }
                    Img img = GenerateImg(detailUrl, previewUrl, id);
                    if (img != null) imgs.Add(img);
                }
            }
            else
            {
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
            }

            return imgs;
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();

            if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
            {

                Login(proxy);
                Dictionary<string, object> tags = new Dictionary<string, object>();
                Dictionary<string, object> tag = new Dictionary<string, object>();

                string url = string.Format(SiteUrl + "/rpc/cps.php?keyword={0}", word);

                shc.Referer = referer;
                shc.ContentType = SessionHeadersValue.AcceptAppJson;
                shc.Add("X-Requested-With", "XMLHttpRequest");
                shc.Remove("Accept-Ranges");
                string json = Sweb.Get(url, proxy, "UTF-8", shc);

                object[] array = (new JavaScriptSerializer()).DeserializeObject(json) as object[];

                tags = (new JavaScriptSerializer()).DeserializeObject(json) as Dictionary<string, object>;
                if (tags.ContainsKey("candidates"))
                {
                    foreach (object obj in tags["candidates"] as object[])
                    {
                        tag = obj as Dictionary<string, object>;
                        re.Add(new TagItem() { Name = tag["tag_name"].ToString() });
                    }
                }
            }
            return re;
        }

        private Img GenerateImg(string detailUrl, string preview_url, string id)
        {
            shc.Add("Accept-Ranges", "bytes");
            shc.ContentType = SessionHeadersValue.ContentTypeAuto;

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
                string page = Sweb.Get(i.DetailUrl, p, "UTF-8", shc);

                Regex reg = new Regex(@"^「(?<Desc>.*?)」/「(?<Author>.*?)」");
                HtmlDocument doc = new HtmlDocument();
                HtmlDocument ds = new HtmlDocument();
                doc.LoadHtml(page);

                //04/16/2012 17:44｜600×800｜SAI  or 04/16/2012 17:44｜600×800 or 04/19/2012 22:57｜漫畫 6P｜SAI
                i.Date = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[1]").InnerText;
                //總點數
                i.Score = int.Parse(doc.DocumentNode.SelectSingleNode("//dd[@class='rated-count']").InnerText);
                //「カルタ＆わたぬき」/「えれっと」のイラスト [pixiv]
                //標題中取名字和作者
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

                //600×800 or 漫畫 6P
                string dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]").InnerText;
                try
                {
                    //706×1000
                    i.Width = int.Parse(dimension.Substring(0, dimension.IndexOf('×')));
                    i.Height = int.Parse(Regex.Match(dimension.Substring(dimension.IndexOf('×') + 1), @"\d+").Value);
                }
                catch { }
                try
                {
                    if (i.Width == 0 && i.Height == 0)
                    {
                        //i.OriginalUrl = i.SampleUrl.Replace("600x600", "1200x1200");
                        //i.JpegUrl = i.OriginalUrl;
                        //manga list
                        //漫畫 6P
                        string oriul = "";
                        int index = dimension.IndexOf(' ') + 1;
                        string mangaPart = dimension.Substring(index, dimension.IndexOf('P') - index);
                        int mangaCount = int.Parse(mangaPart);
                        i.Dimension = "Manga " + mangaCount + "P";
                        for (int j = 0; j < mangaCount; j++)
                        {
                            //儲存漫畫時優先下載原圖 找不到原圖則下jpg
                            try
                            {
                                page = Sweb.Get(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + j, p, "UTF-8", shc);
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

        private void Login(IWebProxy proxy)
        {
            if (!cookie.Contains("pixiv") || !cookie.Contains("token=") || string.IsNullOrWhiteSpace(Sweb.GetURLCookies(SiteUrl)))
            {
                try
                {
                    HtmlDocument hdoc = new HtmlDocument();

                    cookie = "";
                    string
                        data = "",
                        post_key = "",
                        loginpost = "https://accounts.pixiv.net/api/login?lang=zh",
                        loginurl = "https://accounts.pixiv.net/login?lang=zh&source=pc&view_type=page&ref=wwwtop_accounts_index";

                    int index = rand.Next(0, user.Length);

                    shc.Referer = Referer;
                    shc.Remove("X-Requested-With");
                    shc.Remove("Accept-Ranges");
                    shc.ContentType = SessionHeadersValue.AcceptTextHtml;

                    //請求1 獲取post_key
                    data = Sweb.Get(loginurl, proxy, "UTF-8", shc);
                    hdoc.LoadHtml(data);
                    post_key = hdoc.DocumentNode.SelectSingleNode("//input[@name='post_key']").Attributes["value"].Value;
                    if (post_key.Length < 9)
                        throw new Exception("自動登入失敗");

                    //請求2 POST取登入Cookie
                    shc.ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;
                    data = "pixiv_id=" + user[index]
                        + "&password=" + pass[index]
                        + "&captcha=&g_recaptcha_response=&post_key=" + post_key
                        + "&source=pc&ref=wwwtop_accounts_index&return_to=http://www.pixiv.net/";
                    data = Sweb.Post(loginpost, data, proxy, "UTF-8", shc);
                    cookie = Sweb.GetURLCookies(SiteUrl);

                    if (data.Contains("400"))
                        throw new Exception(data);
                    else if (cookie.Length < 9)
                        throw new Exception("自動登入失敗 ");
                    else
                        cookie = "pixiv;" + cookie;
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message.TrimEnd("。".ToCharArray()) + "或無法連接到遠程伺服器");
                }
            }
        }

    }
}
