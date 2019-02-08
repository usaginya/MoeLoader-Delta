using HtmlAgilityPack;
using MoeLoaderDelta;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SitePack
{
    /// <summary>
    /// PIXIV
    /// Last change 190208
    /// </summary>

    public class SitePixiv : AbstractImageSite
    {
        //标签, 完整标签, 作者id, 日榜, 周榜, 月榜, 作品id, 作品id及相关作品
        public enum PixivSrcType { Tag, TagFull, Author, Day, Week, Month, Pid, PidPlus }

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
                else if (srcType == PixivSrcType.Pid)
                    return "www.pixiv.net [IllustId]";
                else if (srcType == PixivSrcType.PidPlus)
                    return "www.pixiv.net [IllustId+]";
                return "www.pixiv.net [Tag]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (srcType == PixivSrcType.Author)
                    return "搜索作者";
                else if (srcType == PixivSrcType.Day)
                    return "本日排行";
                else if (srcType == PixivSrcType.Week)
                    return "本周排行";
                else if (srcType == PixivSrcType.Month)
                    return "本月排行";
                else if (srcType == PixivSrcType.TagFull)
                    return "搜索完整标签";
                else if (srcType == PixivSrcType.Pid)
                    return "搜索作品id";
                else if (srcType == PixivSrcType.PidPlus)
                    return "搜索作品id并显示相关作品";
                return "最新作品 & 搜索标签";
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
                else if (srcType == PixivSrcType.Pid)
                    return "[PID]";
                else if (srcType == PixivSrcType.PidPlus)
                    return "[PID+]";
                return "[T]";
            }
        }
        public override string ShortName => "pixiv";
        public override string Referer => referer;
        public override string SubReferer => ShortName + ",pximg";
        public override string LoginURL => "https://accounts.pixiv.net/login?lang=zh&source=pc&view_type=page&ref=";
        public override bool LoginSite { get => IsLoginSite; set => IsLoginSite = value; }
        public override string LoginUser => nowUser ?? base.LoginUser;

        public override bool IsSupportCount  //fixed 20
        {
            get
            {
                if (srcType == PixivSrcType.PidPlus)
                    return true;
                else if (srcType == PixivSrcType.Author)
                    return true;
                else
                    return false;
            }
        }
        //public override bool IsSupportScore { get { return false; } }
        public override bool IsSupportRes => false;
        //public override bool IsSupportPreview { get { return true; } }
        //public override bool IsSupportTag { get { if (srcType == PixivSrcType.Author) return true; else return false; } }
        public override bool IsSupportTag => true;

        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(150, 150); } }
        //public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(150, 150); } }
        private int page = 1;
        private int count = 1;
        private string keyWord = null;
        private static string cookie = string.Empty, nowUser = null;
        private string[] user = { "moe1user", "moe3user", "a-rin-a" };
        private string[] pass = { "630489372", "1515817701", "2422093014" };
        private static string tempPage = null;
        private Random rand = new Random();
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private PixivSrcType srcType = PixivSrcType.Tag;
        private string referer = "https://www.pixiv.net/";
        private static bool startLogin, IsLoginSite;

        /// <summary>
        /// pixiv.net site
        /// </summary>
        public SitePixiv(PixivSrcType srcType, IWebProxy proxy)
        {
            this.srcType = srcType;
            Task.Factory.StartNew(() => FirstLogin(proxy));
        }

        /// <summary>
        /// 首次尝试登录
        /// </summary>
        private void FirstLogin(IWebProxy proxy)
        {
            if (!startLogin)
            {
                startLogin = true;
                CookieRestore();
                Login(proxy);
            }
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            Login(proxy);
            //if (page > 1000) throw new Exception("页码过大，若需浏览更多图片请使用关键词限定范围");
            int memberId = 0;
            string url = null;
            this.page = page;
            this.count = count;
            this.keyWord = keyWord;
            if (srcType == PixivSrcType.Pid || srcType == PixivSrcType.PidPlus)
            {
                if (keyWord.Length > 0 && int.TryParse(keyWord.Trim(), out memberId))
                {
                    url = SiteUrl + "/member_illust.php?mode=medium&illust_id=" + memberId;
                }
                else
                {
                    throw new Exception("请输入图片id");
                }
            }
            else
            {
                //http://www.pixiv.net/new_illust.php?p=2
                url = SiteUrl + "/new_illust.php?p=" + page;

                if (keyWord.Length > 0)
                {
                    //http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2
                    url = SiteUrl + "/search.php?s_mode=s_tag"
                        + (srcType == PixivSrcType.TagFull ? "_full" : "")
                    + "&word=" + keyWord + "&order=date_d&p=" + page;
                }

                memberId = 0;
                if (srcType == PixivSrcType.Author)
                {
                    if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out memberId))
                    {
                        throw new Exception("必须在关键词中指定画师 id；若需要使用标签进行搜索请使用 www.pixiv.net [TAG]");
                    }
                    //member id 
                    //url = SiteUrl + "/member_illust.php?id=" + memberId + "&p=" + page;
                    //https://www.pixiv.net/ajax/user/212801/profile/all
                    //https://www.pixiv.net/ajax/user/212801/profile/illusts?ids%5B%5D=70095905&ids%5B%5D=69446164&is_manga_top=0
                    url = SiteUrl + "/ajax/user/" + memberId + "/profile/all";
                }
                else if (srcType == PixivSrcType.Day)
                {
                    url = $"{SiteUrl}/ranking.php?mode=daily&p={page}";
                    url = $"{url}{(keyWord.Trim().Length > 0 && int.TryParse(keyWord.Trim(), out memberId) ? $"&date={memberId}" : string.Empty)}";
                }
                else if (srcType == PixivSrcType.Week)
                {
                    url = $"{SiteUrl}/ranking.php?mode=weekly&p={page}";
                    url = $"{url}{(keyWord.Trim().Length > 0 && int.TryParse(keyWord.Trim(), out memberId) ? $"&date={memberId}" : string.Empty)}";
                }
                else if (srcType == PixivSrcType.Month)
                {
                    url = $"{SiteUrl}/ranking.php?mode=monthly&p={page}";
                    url = $"{url}{(keyWord.Trim().Length > 0 && int.TryParse(keyWord.Trim(), out memberId) ? $"&date={memberId}" : string.Empty)}";
                }
            }
            shc.Remove("X-Requested-With");
            shc.Remove("Accept-Ranges");
            shc.ContentType = SessionHeadersValue.AcceptTextHtml;
            shc.Set("Cookie", cookie);
            string pageString = Sweb.Get(url, proxy, shc);
            if (srcType == PixivSrcType.PidPlus)
            {
                //相关作品json信息
                //https://www.pixiv.net/ajax/illust/70575612/recommend/init?limit=18
                tempPage = Sweb.Get(SiteUrl + "/ajax/illust/" + keyWord + "/recommend/init?limit=18", proxy, shc);
            }
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
                    if (tagNode == null)
                    {
                        nodes = doc.DocumentNode.SelectSingleNode("//div[@id='wrapper']/div[1]/div/ul").SelectNodes("li");
                    }
                }
                else if (srcType == PixivSrcType.Author)
                {
                    //181013遗弃的方案
                    //nodes = doc.DocumentNode.SelectSingleNode("//ul[@class='_image-items']").SelectNodes("li");

                    //ROOT ->body -> illusts
                    //ROOT ->body -> manga
                    //获取图片id
                    List<string> illustsList = new List<string>();
                    List<string> mangaList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(pageString))
                    {
                        JObject jsonObj = JObject.Parse(pageString);
                        JToken jToken;
                        if (!string.IsNullOrWhiteSpace(jsonObj["body"].ToString()))
                        {
                            jToken = ((JObject)jsonObj["body"])["illusts"];
                            foreach (JProperty jp in jToken)
                            {
                                illustsList.Add(jp.Name);
                            }
                            jToken = ((JObject)jsonObj["body"])["manga"];
                            foreach (JProperty jp in jToken)
                            {
                                mangaList.Add(jp.Name);
                            }
                        }
                    }
                    string ids = string.Empty;
                    int ilistcount = illustsList.Count,
                        mlistcount = mangaList.Count,
                        scount = ilistcount + mlistcount;
                    for (int j = (page - 1) * count; j < page * count & scount > 0 & j <= scount; j++)
                    {
                        if (j < ilistcount)
                            ids += $"ids[]={illustsList[j]}&";
                        if (j < mlistcount)
                            ids += $"ids[]={mangaList[j]}&";
                    }
                    if (!string.IsNullOrWhiteSpace(ids))
                    {
                        pageString = Sweb.Get($"{SiteUrl}/ajax/user/{keyWord}/profile/illusts?{ids}is_manga_top=0", proxy, shc);
                        //ROOT->body->works
                        //获取图片详细信息
                        if (!string.IsNullOrWhiteSpace(pageString))
                        {
                            JObject jsonObj = JObject.Parse(pageString);
                            JToken jToken;
                            if (!string.IsNullOrWhiteSpace(jsonObj["body"].ToString()))
                            {
                                jToken = ((JObject)jsonObj["body"])["works"];
                                foreach (JProperty jp in jToken)
                                {
                                    JToken nextJToken = (((JObject)jsonObj["body"])["works"])[jp.Name];
                                    Img img = GenerateImg(SiteUrl + "/member_illust.php?mode=medium&illust_id=" + jp.Name, (string)nextJToken["url"], (string)nextJToken["id"]);
                                    if (img != null) imgs.Add(img);
                                }

                            }
                        }
                    }
                    return imgs;
                }
                //else if (srcType == PixivSrcType.Day || srcType == PixivSrcType.Month || srcType == PixivSrcType.Week) //ranking
                //nodes = doc.DocumentNode.SelectSingleNode("//section[@class='ranking-items autopagerize_page_element']").SelectNodes("div");
                else if (srcType == PixivSrcType.PidPlus)
                {
                    //相关作品json信息
                    string relatePicJson = tempPage;
                    string imagesJson = string.Empty;
                    //ROOT ->body -> recommendMethods
                    List<string> rmsList = new List<string>();//recommendMethods 数据
                    if (!string.IsNullOrWhiteSpace(relatePicJson))
                    {
                        JObject jsonObj = JObject.Parse(relatePicJson);
                        JToken jToken;
                        if (!string.IsNullOrWhiteSpace(jsonObj["body"].ToString()))
                        {
                            imagesJson = jsonObj["body"].ToString();
                            jToken = ((JObject)jsonObj["body"])["recommendMethods"];
                            foreach (JProperty jp in jToken)
                            {
                                rmsList.Add(jp.Name);
                            }
                        }
                    }
                    string ids = string.Empty;
                    ids = (page == 1 ? "ids[]=" + keyWord + "&" : ids);

                    for (int j = (page - 1) * count; j < page * count & rmsList.Count > 0 & j <= rmsList.Count; j++)
                        ids += "ids[]=" + rmsList[j] + "&";
                    if (!string.IsNullOrWhiteSpace(ids))
                    {
                        pageString = Sweb.Get($"{SiteUrl}/ajax/user/{keyWord}/profile/illusts?{ids}is_manga_top=0", proxy, shc);
                        if (!string.IsNullOrWhiteSpace(pageString))
                        {
                            JObject jsonObj = JObject.Parse(pageString);
                            JToken jToken;
                            if (!string.IsNullOrWhiteSpace(jsonObj["body"].ToString()))
                            {
                                jToken = ((JObject)jsonObj["body"])["works"];
                                foreach (JProperty jp in jToken)
                                {
                                    JToken nextJToken = (((JObject)jsonObj["body"])["works"])[jp.Name];
                                    Img img = GenerateImg($"{SiteUrl}/member_illust.php?mode=medium&illust_id={ jp.Name}", (string)nextJToken["url"], (string)nextJToken["id"]);
                                    if (keyWord == jp.Name) img.Source = "相关作品";
                                    if (img != null) imgs.Add(img);
                                }
                            }
                        }
                    }
                    return imgs;
                }
                else if (srcType == PixivSrcType.Pid)
                {
                    if (!(Regex.Match(pageString, @"<h2.*?/h2>").Value.Contains("错误")))
                    {
                        int mangaCount = 1;
                        string id, SampleUrl;
                        id = SampleUrl = string.Empty;
                        if (!pageString.Contains("globalInitData"))
                        {
                            //----- 旧版 -----
                            SampleUrl = doc.DocumentNode.SelectSingleNode("/html/head/meta[@property='og:image']").Attributes["content"].Value;
                            id = SampleUrl.Substring(SampleUrl.LastIndexOf("/") + 1, SampleUrl.IndexOf("_") - SampleUrl.LastIndexOf("/") - 1);

                            string dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]").InnerText;
                            if (dimension.EndsWith("P"))
                                mangaCount = int.Parse(Regex.Match(dimension, @"\d+").Value);
                        }
                        else
                        {
                            //----- 新版 -----
                            Match strRex = Regex.Match(pageString, @"(?<=(?:,illust\:.{.))\d+(?=(?:\:.))");
                            id = strRex.Value;

                            strRex = Regex.Match(pageString, @"(?<=(?:" + id + ":.)).*?(?=(?:.},user))");

                            JObject jobj = JObject.Parse(strRex.Value);
                            try { mangaCount = int.Parse(jobj["pageCount"].ToSafeString()); } catch { }

                            jobj = JObject.Parse(jobj["urls"].ToSafeString());
                            SampleUrl = jobj["thumb"].ToSafeString();
                        }
                        string detailUrl = SiteUrl + "/member_illust.php?mode=medium&illust_id=" + id;
                        for (int j = 0; j < mangaCount; j++)
                        {
                            Img img = GenerateImg(detailUrl, SampleUrl.Replace("_p0_", "_p" + j.ToString() + "_"), id);
                            //if (i != 0) img.Source = "相关作品";
                            StringBuilder sb = new StringBuilder();
                            sb.Append("P");
                            sb.Append(j.ToString());
                            img.Dimension = sb.ToString();
                            if (img != null) imgs.Add(img);
                        }
                        return imgs;
                    }
                    else throw new Exception("该作品已被删除，或作品ID不存在");
                }
                else
                {
                    //ranking
                    nodes = doc.DocumentNode.SelectNodes("//section[@class='ranking-item']");
                }
            }
            catch (Exception ex)
            {
                SiteManager.echoErrLog(SiteName, ex);
                throw new Exception("没有找到图片哦～ .=ω=");
            }

            if (nodes == null && tagNode == null)
            {
                return imgs;
            }

            if (nodes != null && nodes.Count > 0)
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
                        //sampleUrl 正则 @"https://i\.pximg\..+?(?=")"
                        string detailUrl = anode.Attributes["href"].Value.Replace("amp;", string.Empty);
                        string sampleUrl = string.Empty;
                        sampleUrl = anode.SelectSingleNode(".//img").Attributes["src"].Value;

                        if (sampleUrl.ToLower().Contains("images/common"))
                            sampleUrl = anode.SelectSingleNode(".//img").Attributes["data-src"].Value;

                        if (sampleUrl.Contains('?'))
                            sampleUrl = sampleUrl.Substring(0, sampleUrl.IndexOf('?'));

                        //extract id from detail url
                        //string id = detailUrl.Substring(detailUrl.LastIndexOf('=') + 1);
                        string id = Regex.Match(detailUrl, @"illust_id=\d+").Value;
                        id = id.Substring(id.IndexOf('=') + 1);

                        Img img = GenerateImg(detailUrl, sampleUrl, id);
                        if (img != null) imgs.Add(img);
                    }
                    catch
                    {
                        //int i = 0;
                    }
                }
            }
            else if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
            {//Tag search js-mount-point-search-related-tags Json
                string jsonData = tagNode.Attributes["data-items"].Value.Replace("&quot;", "\"");
                object[] array = (new JavaScriptSerializer()).DeserializeObject(jsonData) as object[];
                foreach (object o in array)
                {
                    Dictionary<string, object> obj = o as Dictionary<string, object>;
                    string
                        detailUrl = "",
                        SampleUrl = "",
                        id = "";
                    if (obj["illustId"] != null)
                    {
                        id = obj["illustId"].ToString();
                        detailUrl = SiteUrl + "/member_illust.php?mode=medium&illust_id=" + id;
                    }
                    if (obj["url"] != null)
                    {
                        SampleUrl = obj["url"].ToString();
                    }
                    Img img = GenerateImg(detailUrl, SampleUrl, id);
                    if (img != null) imgs.Add(img);
                }
            }

            return imgs;
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();

            if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
            {
                Dictionary<string, object> tags = new Dictionary<string, object>();
                Dictionary<string, object> tag = new Dictionary<string, object>();

                string url = string.Format(SiteUrl + "/rpc/cps.php?keyword={0}", word);

                shc.Referer = referer;
                //shc.ContentType = SessionHeadersValue.AcceptAppJson;
                shc.Remove("Accept-Ranges");
                string json = Sweb.Get(url, proxy, shc);

                //object[] array = (new JavaScriptSerializer()).DeserializeObject(json) as object[];
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

        private Img GenerateImg(string detailUrl, string sample_url, string id)
        {
            shc.Add("Accept-Ranges", "bytes");
            shc.ContentType = SessionHeadersValue.ContentTypeAuto;

            int intId = int.Parse(id);

            if (!detailUrl.StartsWith("http") && !detailUrl.StartsWith("/"))
                detailUrl = "/" + detailUrl;

            //convert relative url to absolute
            if (detailUrl.StartsWith("/"))
                detailUrl = SiteUrl + detailUrl;
            if (sample_url.StartsWith("/"))
                sample_url = SiteUrl + sample_url;

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
                //PreviewUrl = preview_url,
                SampleUrl = sample_url,
                //Score = 0,
                //Width = width,
                //Height = height,
                //Tags = tags,
                DetailUrl = detailUrl
            };

            img.DownloadDetail = new DetailHandler((i, p) =>
            {
                int pageCount = 1;
                string page, dimension, Pcount;
                page = dimension = string.Empty;
                //retrieve details
                page = Sweb.Get(i.DetailUrl, p, shc);

                Regex reg = new Regex(@"】「(?<Desc>.*?)」.*?/(?<Author>.*?)\s\[pixi");
                HtmlDocument doc = new HtmlDocument();
                HtmlDocument ds = new HtmlDocument();
                doc.LoadHtml(page);
                Pcount = Regex.Match(i.SampleUrl, @"(?<=_p)\d+(?=_)").Value;

                //=================================================
                //[R-XX] 【XX】「Desc」插画/Author [pixiv]
                //标题中取名字和作者
                try
                {
                    MatchCollection mc = reg.Matches(doc.DocumentNode.SelectSingleNode("//title").InnerText);
                    if (srcType == PixivSrcType.Pid)
                        i.Desc = mc[0].Groups["Desc"].Value + "P" + Pcount;
                    else
                        i.Desc = mc[0].Groups["Desc"].Value;
                    i.Author = mc[0].Groups["Author"].Value;
                }
                catch { }
                //------------------------
                if (!page.Contains("globalInitData"))
                {
                    //++++旧版详情页+++++
                    //04/16/2012 17:44｜600×800｜SAI  or 04/16/2012 17:44｜600×800 or 04/19/2012 22:57｜漫画 6P｜SAI
                    i.Date = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[1]").InnerText;
                    //总点数
                    i.Score = int.Parse(doc.DocumentNode.SelectSingleNode("//dd[@class='rated-count']").InnerText);

                    //URLS
                    //http://i2.pixiv.net/c/600x600/img-master/img/2014/10/08/06/13/30/46422743_p0_master1200.jpg
                    //http://i2.pixiv.net/img-original/img/2014/10/08/06/13/30/46422743_p0.png
                    Regex rx = new Regex(@"/\d+x\d+/");
                    i.PreviewUrl = rx.Replace(i.SampleUrl, "/1200x1200/");
                    i.JpegUrl = i.PreviewUrl;
                    try
                    {
                        i.OriginalUrl = doc.DocumentNode.SelectSingleNode("//*[@id='wrapper']/div[2]/div").SelectSingleNode(".//img").Attributes["data-src"].Value;
                    }
                    catch { }
                    i.OriginalUrl = string.IsNullOrWhiteSpace(i.OriginalUrl) ? i.JpegUrl : i.OriginalUrl;

                    //600×800 or 漫画 6P
                    dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]").InnerText;
                    try
                    {
                        //706×1000
                        i.Width = int.Parse(dimension.Substring(0, dimension.IndexOf('×')));
                        i.Height = int.Parse(Regex.Match(dimension.Substring(dimension.IndexOf('×') + 1), @"\d+").Value);
                    }
                    catch { }
                }
                else
                {
                    //+++++新版详情页+++++
                    Match strRex = Regex.Match(page, @"(?<=(?:" + i.Id + ":.)).*?(?=(?:.},user))");

                    JObject jobj = JObject.Parse(strRex.Value);

                    i.Date = jobj["uploadDate"].ToSafeString();

                    try
                    {
                        i.Score = int.Parse(jobj["likeCount"].ToSafeString());
                        i.Width = int.Parse(jobj["width"].ToSafeString());
                        i.Height = int.Parse(jobj["height"].ToSafeString());
                        pageCount = int.Parse(jobj["pageCount"].ToSafeString());
                    }
                    catch { }

                    jobj = JObject.Parse(jobj["urls"].ToSafeString());
                    Regex rex = new Regex(@"(?<=.*)p\d+(?=[^/]*[^\._]*$)");
                    i.PreviewUrl = rex.Replace(jobj["regular"].ToSafeString(), "p" + Pcount);
                    i.JpegUrl = rex.Replace(jobj["small"].ToSafeString(), "p" + Pcount);
                    i.OriginalUrl = rex.Replace(jobj["original"].ToSafeString(), "p" + Pcount);
                }
                //----------------------------

                try
                {
                    if (pageCount > 1 || i.Width == 0 && i.Height == 0)
                    {
                        //i.OriginalUrl = i.SampleUrl.Replace("600x600", "1200x1200");
                        //i.JpegUrl = i.OriginalUrl;
                        //manga list
                        //漫画 6P
                        string oriul = "";
                        int mangaCount = pageCount;
                        if (pageCount > 1)
                        {
                            mangaCount = pageCount;
                        }
                        else
                        {
                            int index = dimension.IndexOf(' ') + 1;
                            string mangaPart = dimension.Substring(index, dimension.IndexOf('P') - index);
                            mangaCount = int.Parse(mangaPart);
                        }
                        if (srcType == PixivSrcType.Pid)
                        {
                            try
                            {
                                page = Sweb.Get(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + Pcount, p, shc);
                                ds.LoadHtml(page);
                                i.OriginalUrl = ds.DocumentNode.SelectSingleNode("/html/body/img").Attributes["src"].Value;
                            }
                            catch { }
                        }
                        else
                        {
                            i.Dimension = "Manga " + mangaCount + "P";
                            for (int j = 0; j < mangaCount; j++)
                            {
                                //保存漫画时优先下载原图 找不到原图则下jpg
                                try
                                {
                                    page = Sweb.Get(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + j, p, shc);
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
                }
                catch { }
            });

            return img;
        }

        /// <summary>
        /// 还原Cookie
        /// </summary>
        private void CookieRestore()
        {
            if (!string.IsNullOrWhiteSpace(cookie)) return;

            if (!IELogin())
            {
                string ck = Sweb.GetURLCookies(SiteUrl);
                cookie = string.IsNullOrWhiteSpace(ck) ? string.Empty : $"pixiv;{ck}";
            }
        }

        private void Login(IWebProxy proxy)
        {
            if ((!cookie.Contains("pixiv") && !cookie.Contains("token=")) || IsLoginSite)
            {
                try
                {
                    nowUser = null;
                    cookie = string.Empty;
                    string data = string.Empty, post_key = string.Empty,
                        loginpost = "https://accounts.pixiv.net/api/login?lang=zh";

                    if (IsLoginSite)
                    {
                        if (!IELogin())
                        {
                            Login(proxy); //重新自动登录
                        }
                    }
                    else
                    {
                        int index = rand.Next(0, user.Length);

                        shc.Referer = Referer;
                        shc.Remove("X-Requested-With");
                        shc.Remove("Accept-Ranges");
                        shc.Remove("Cookie");
                        shc.ContentType = SessionHeadersValue.AcceptTextHtml;
                        HtmlDocument hdoc = new HtmlDocument();

                        //请求1 获取post_key
                        data = Sweb.Get(LoginURL, proxy, shc);
                        hdoc.LoadHtml(data);
                        post_key = hdoc.DocumentNode.SelectSingleNode("//input[@name='post_key']").Attributes["value"].Value;
                        if (post_key.Length < 9)
                        {
                            SiteManager.echoErrLog(SiteName, "自动登录失败 ");
                            return;
                        }

                        //请求2 POST取登录Cookie
                        shc.ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;
                        data = "pixiv_id=" + user[index]
                            + "&captcha=&g_recaptcha_response="
                            + "&password=" + pass[index]
                            + "&post_key=" + post_key
                            + "&source=pc&ref=&return_to=https%3A%2F%2Fwww.pixiv.net%2F";
                        data = Sweb.Post(loginpost, data, proxy, shc);
                        cookie = Sweb.GetURLCookies(SiteUrl);

                        if (!data.Contains("success"))
                        {
                            if (data.Contains("locked"))
                            {
                                throw new Exception("登录Pixiv时IP被封锁，剩余时间：" + Regex.Match(data, "lockout_time_by_ip\":\"(\\d+)\"").Groups[1].Value);
                            }
                            else if (cookie.Length < 9)
                                SiteManager.echoErrLog(SiteName, "自动登录失败 ");
                            else
                                SiteManager.echoErrLog(SiteName, $"自动登录失败 {data}");
                        }
                        else
                        {
                            cookie = $"pixiv;{cookie}";
                            nowUser = "内置账号";
                        }
                    }

                }
                catch (Exception e)
                {
                    SiteManager.echoErrLog(SiteName, e, e.Message.Contains("IP") ? e.Message : "可能无法连接到服务器");
                }
            }
        }

        /// <summary>
        /// 从IE登录
        /// </summary>
        private bool IELogin()
        {
            IsLoginSite = false;

            bool result = SiteManager.LoginSite(this, ref cookie, "/logout", ref Sweb, ref shc);

            if (result)
            {
                nowUser = "你的账号";
                cookie = $"pixiv;{cookie}";
            }
            else if (!startLogin)
            {
                SiteManager.echoErrLog(SiteName, "用户登录失败 ");
            }

            return result;
        }

    }
}