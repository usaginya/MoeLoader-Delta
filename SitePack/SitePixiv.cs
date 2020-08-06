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
using System.Windows.Forms;

namespace SitePack
{
    /// <summary>
    /// PIXIV
    /// Last change 200806
    /// </summary>

    public class SitePixiv : AbstractImageSite
    {
        //标签, 完整标签, 作者id, 日榜, 周榜, 月榜, 作品id, 作品id及相关作品, 扩展设置0
        public enum PixivSrcType { Tag, TagFull, Author, Day, Week, Month, Pid, PidPlus, ExtStteing_0 }

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
                else if (srcType == PixivSrcType.ExtStteing_0)
                    return "ExtStteing_0";
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
        public override string LoginURL => SiteManager.SiteLoginType.Cookie.ToSafeString();
        public override bool LoginSiteIsLogged => IsLoginSite;
        public override string LoginUser => nowUser ?? base.LoginUser;
        public override string LoginPwd { get => nowPwd; set => nowPwd = value; }
        public override string LoginHelpUrl => "https://docs.qq.com/doc/DWWhUcHlzbE9aeXZE?pub=1";

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

        public override List<SiteExtendedSetting> ExtendedSettings { get => extendedSettings; set => extendedSettings = value; }

        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(150, 150); } }
        //public override System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(150, 150); } }
        private int page = 1;
        private int count = 1;
        private string keyWord = null;
        private static string cookie = string.Empty, nowUser = null, nowPwd = nowUser;
        private static string tempPage = null;
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private readonly PixivSrcType srcType = PixivSrcType.Author;
        private string referer = "https://www.pixiv.net/";
        private string LoginUrl => "https://accounts.pixiv.net/login?lang=zh&source=pc&view_type=page&ref=";
        private static bool startLogin, IsLoginSite, IsRunLogin;
        private const string pixivCat = "i.pixiv.cat";
        private const string extSettingOff = "0";
        private const string extSettingOn = "1";
        private static bool enableThirdParty = false;
        private List<SiteExtendedSetting> extendedSettings = new List<SiteExtendedSetting>();
        private delegate void delegateExtSetting();

        /// <summary>
        /// pixiv.net site
        /// </summary>
        public SitePixiv(PixivSrcType srcType)
        {
            this.srcType = srcType;
            if (string.IsNullOrWhiteSpace(cookie))
            {
                cookie = SiteManager.SiteConfig(ShortName, "Login", "Cookie");
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    SiteManager.SiteConfig(ShortName, "Login", "Cookie", null, true);
                }
            }
            if (!startLogin && srcType == PixivSrcType.Author)
            {
                FirstLogin();
            }
            CreateExtSetting();
        }

        /// <summary>
        /// 创建扩展菜单方法
        /// </summary>
        private void CreateExtSetting()
        {
            if (!SiteName.Contains("ExtStteing")) { return; }

            ExtendedSettings = new List<SiteExtendedSetting>();
            SiteExtendedSetting ses;
            #region 第三方站点服务选项设置
            string cfgValue = SiteManager.SiteConfig(ShortName, "Cfg", "EnableThirdParty");
            enableThirdParty = !string.IsNullOrWhiteSpace(cfgValue) && cfgValue.Trim() != extSettingOff;
            ses = new SiteExtendedSetting()
            {
                Title = "使用pixiv.cat服务下载图片",
                Enable = enableThirdParty,
                SettingAction = new delegateExtSetting(ExtSetting_EnableThirdParty)
            };
            ExtendedSettings.Add(ses);
            #endregion
        }

        /// <summary>
        /// 首次尝试登录
        /// </summary>
        private async void FirstLogin()
        {
            await Task.Run(() =>
            {
                if (!IsLoginSite) { LoginCall(new LoginSiteArgs() { Cookie = cookie }); }
                startLogin = true;
            });
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            if (!IsLoginSite) { return string.Empty; }

            //if (page > 1000) throw new Exception("页码过大，若需浏览更多图片请使用关键词限定范围");
            this.keyWord = keyWord;
            int memberId = 0;
            string url = null;
            this.page = page;
            this.count = count;

            if (srcType == PixivSrcType.Pid || srcType == PixivSrcType.PidPlus)
            {
                if (keyWord.Length > 0 && int.TryParse(keyWord.Trim(), out memberId))
                {
                    url = $"{SiteUrl}/artworks/{memberId}";
                }
                else
                {
                    throw new Exception("请输入图片id");
                }
            }
            else
            {
                if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
                {
                    if (keyWord.Length > 0)
                    {
                        //http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2 //旧版
                        //https://www.pixiv.net/ajax/search/artworks/hatsune?p=2&word=hatsune
                        url = $"{SiteUrl}/ajax/search/artworks/{keyWord}?word={keyWord}&p={page}";  //默认按最新排序
                    }
                    else
                    {
                        //http://www.pixiv.net/new_illust.php?p=2
                        url = $"{SiteUrl}/new_illust.php?p={page}";
                    }
                }

                memberId = 0;
                if (srcType == PixivSrcType.Author)
                {
                    if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out memberId))
                    {
                        throw new Exception($"必须在关键词中指定画师 id{Environment.NewLine}如果想要使用标签进行搜索请选择 www.pixiv.net [TAG]");
                    }
                    //member id 
                    //url = SiteUrl + "/member_illust.php?id=" + memberId + "&p=" + page;
                    //https://www.pixiv.net/ajax/user/212801/profile/all
                    //https://www.pixiv.net/ajax/user/212801/profile/illusts?ids%5B%5D=70095905&ids%5B%5D=69446164&is_manga_top=0
                    url = $"{SiteUrl}/ajax/user/{memberId}/profile/all";
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
                shc.ContentType = SessionHeadersValue.AcceptAppJson;
                tempPage = Sweb.Get($"{SiteUrl}/ajax/illust/{keyWord}/recommend/init?limit=18", proxy, shc);
            }
            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(pageString);

            //retrieve all elements via xpath
            HtmlNodeCollection nodes = null;
            HtmlNode tagNode = null;

            try
            {
                if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
                {
                    //-----191121 tag解析方法用不上了------
                    //tagNode = doc.DocumentNode.SelectSingleNode("//input[@id='js-mount-point-search-result-list']");
                    ////nodes = doc.DocumentNode.SelectSingleNode("//div[@id='wrapper']/div[2]/div[1]/section[1]/ul").SelectNodes("li");
                    //if (tagNode == null)
                    //{
                    //    nodes = doc.DocumentNode.SelectSingleNode("//div[@id='wrapper']/div[1]/div/ul").SelectNodes("li");
                    //}

                    string id, SampleUrl, detailUrl;
                    detailUrl = id = SampleUrl = string.Empty;

                    //Root->body->illustManga->data->illustId
                    JObject jobj = JObject.Parse(pageString);
                    JArray jArray = (JArray)jobj["body"]["illustManga"]["data"];
                    foreach (JObject jobjPicInfo in jArray)
                    {
                        id = jobjPicInfo["illustId"].ToSafeString();
                        SampleUrl = jobjPicInfo["url"].ToSafeString();
                        detailUrl = SiteUrl + "/artworks/" + id;
                        Img img = GenerateImg(detailUrl, SampleUrl, id);
                        if (img != null) imgs.Add(img);
                    }
                    return imgs;
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
                    int ilistcount = illustsList.Count,
                        mlistcount = mangaList.Count,
                        ill_num = 0, mng_num = 0,
                        scount = ilistcount + mlistcount;
                    List<string> ids = new List<string>();
                    for (int j = 0; j < page * count && scount > 0 && j < scount; j++)
                    {
                        if (j < (page - 1) * count)
                        {
                            if (ill_num < ilistcount && mng_num < mlistcount)
                            {
                                if (int.Parse(illustsList[ill_num]) > int.Parse(mangaList[mng_num])) ill_num++;
                                else mng_num++;
                            }
                            else if (ill_num < ilistcount) ill_num++;
                            else if (mng_num < mlistcount) mng_num++;
                        }

                        else
                        {
                            if ((j - (page - 1) * count) % 48 == 0)
                                ids.Add(string.Empty);
                            if (ill_num < ilistcount && mng_num < mlistcount)
                            {
                                if (int.Parse(illustsList[ill_num]) > int.Parse(mangaList[mng_num]))
                                    ids[(j - (page - 1) * count) / 48] += $"ids[]={illustsList[ill_num++]}&";
                                else
                                    ids[(j - (page - 1) * count) / 48] += $"ids[]={mangaList[mng_num++]}&";
                            }
                            else if (ill_num < ilistcount)
                                ids[(j - (page - 1) * count) / 48] += $"ids[]={illustsList[ill_num++]}&";

                            else if (mng_num < mlistcount)
                                ids[(j - (page - 1) * count) / 48] += $"ids[]={mangaList[mng_num++]}&";
                        }
                    }
                    if (!ids.Exists(string.IsNullOrWhiteSpace))
                    {
                        shc.ContentType = SessionHeadersValue.AcceptAppJson;
                        List<string> tempPageString = new List<string>();
                        for (int i = 0; i < ids.Count; i++)
                        {
                            tempPageString.Add(Sweb.Get($"{SiteUrl}/ajax/user/{keyWord}/profile/illusts?{ids[i]}work_category=illustManga&is_first_page=0", proxy, shc));
                        }
                        if (!tempPageString.Exists(string.IsNullOrWhiteSpace))
                        {
                            //ROOT->body->works
                            //获取图片详细信息
                            foreach (string tempString in tempPageString)
                            {
                                if (!string.IsNullOrWhiteSpace(tempString))
                                {
                                    JObject jsonObj = JObject.Parse(tempString);
                                    JToken jToken;
                                    if (!string.IsNullOrWhiteSpace(jsonObj["body"].ToString()))
                                    {
                                        jToken = ((JObject)jsonObj["body"])["works"];
                                        foreach (JProperty jp in jToken)
                                        {
                                            JToken nextJToken = (((JObject)jsonObj["body"])["works"])[jp.Name];
                                            Img img = GenerateImg(SiteUrl + "/artworks/" + jp.Name, (string)nextJToken["url"], (string)nextJToken["id"]);
                                            if (img != null) imgs.Add(img);
                                        }

                                    }
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
                    string tempuid = string.Empty;
                    //ROOT ->body -> recommendMethods
                    List<string> rmsList = new List<string>();//recommendMethods 数据
                    if (!string.IsNullOrWhiteSpace(relatePicJson))
                    {
                        JObject JOdata = JObject.Parse(relatePicJson);
                        JToken JTillusts, JTrecommend;
                        if (!string.IsNullOrWhiteSpace(JOdata["body"].ToString()))
                        {
                            JTillusts = ((JObject)JOdata["body"])["illusts"];
                            JTrecommend = ((JObject)JOdata["body"])["recommendMethods"];

                            // get userid
                            if (((JArray)JTillusts).Count > 0)
                            {
                                tempuid = ((JArray)JTillusts)[0]["userId"].ToSafeString();
                            }

                            // recommendMethods
                            foreach (JProperty jp in JTrecommend)
                            {
                                rmsList.Add(jp.Name);
                            }
                        }
                    }
                    string ids = string.Empty;
                    ids = (page == 1 ? $"ids[]={keyWord}&" : ids);

                    for (int j = (page - 1) * count; j < page * count & rmsList.Count > 0 & j < rmsList.Count; j++)
                    {
                        ids += $"ids[]={rmsList[j]}&";
                    }

                    if (!string.IsNullOrWhiteSpace(ids))
                    {
                        shc.ContentType = SessionHeadersValue.AcceptAppJson;
                        pageString = Sweb.Get($"{SiteUrl}/ajax/user/{tempuid}/profile/illusts?{ids}work_category=illustManga&is_first_page=0", proxy, shc);
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
                                    Img img = GenerateImg($"{SiteUrl}/artworks/{ jp.Name}", (string)nextJToken["url"], (string)nextJToken["id"]);
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
                    if (!Regex.Match(pageString, @"<h2.*?/h2>").Value.Contains("错误"))
                    {

                        int mangaCount = 1;
                        string id, SampleUrl, detailUrl;
                        detailUrl = id = SampleUrl = string.Empty;

                        //if (pageString.Contains("globalInitData"))
                        //{
                        //    -----旧版 191120放弃此解析方法---- -
                        //   SampleUrl = doc.DocumentNode.SelectSingleNode("/html/head/meta[@property='og:image']").Attributes["content"].Value;
                        //    id = SampleUrl.Substring(SampleUrl.LastIndexOf("/") + 1, SampleUrl.IndexOf("_") - SampleUrl.LastIndexOf("/") - 1);

                        //    string dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]").InnerText;
                        //    if (dimension.EndsWith("P"))
                        //        mangaCount = int.Parse(Regex.Match(dimension, @"\d+").Value);
                        //}
                        //else
                        //{
                        //    //----- 新版 -----
                        //    Match strRex = Regex.Match(pageString, @"(?<=(?:,illust\:.{.))\d+(?=(?:\:.))");
                        //    id = strRex.Value;

                        //    strRex = Regex.Match(pageString, @"(?<=(?:" + id + ":.)).*?(?=(?:.},user))");

                        //    JObject jobj = JObject.Parse(strRex.Value);
                        //    try { mangaCount = int.Parse(jobj["pageCount"].ToSafeString()); } catch { }

                        //    jobj = JObject.Parse(jobj["urls"].ToSafeString());
                        //    SampleUrl = jobj["thumb"].ToSafeString();
                        //}
                        Match strRex = Regex.Match(pageString, @"(?<=(""meta-preload-data"")\s*content=').*?(?=('>))");

                        JObject jobj = JObject.Parse(strRex.Value);

                        //Root->illust->PID(图片id)
                        //{"timestamp":"xx","illust":{"id":{xx}},"user":{xx}}
                        JObject jobjPicInfo = (
                            (JObject.Parse($@"{jobj["illust"].First.First.ToSafeString()}"))
                            );
                        id = jobjPicInfo["illustId"].ToSafeString();

                        JObject jobjUrl = JObject.Parse(jobjPicInfo["urls"].ToSafeString());

                        SampleUrl = jobjUrl["regular"].ToSafeString();

                        detailUrl = SiteUrl + "/artworks/" + id;

                        try
                        {
                            //判断是否为动图或者多图
                            mangaCount = int.Parse(jobjPicInfo["pageCount"].ToSafeString());
                        }
                        catch { }

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
                SiteManager.EchoErrLog(SiteName, ex, $"获取 [{Uri.UnescapeDataString(keyWord)}] 失败", true);
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
                        string id = Regex.Match(detailUrl, @"artworks/(?<id>\d+)").Groups["id"].ToSafeString();

                        Img img = GenerateImg(detailUrl, sampleUrl, id);
                        if (img != null) { imgs.Add(img); }
                    }
                    catch
                    {
                        //int i = 0;
                    }
                }
            }
            //-----191121 tag解析方法用不上了------
            //else if (srcType == PixivSrcType.Tag || srcType == PixivSrcType.TagFull)
            //{
            //    //Tag search js - mount - point - search - related - tags Json
            //    string jsonData = tagNode.Attributes["data-items"].Value.Replace("&quot;", "\"");
            //    object[] array = (new JavaScriptSerializer()).DeserializeObject(jsonData) as object[];
            //    foreach (object o in array)
            //    {
            //        Dictionary<string, object> obj = o as Dictionary<string, object>;
            //        string
            //            detailUrl = "",
            //            SampleUrl = "",
            //            id = "";
            //        if (obj["illustId"] != null)
            //        {
            //            id = obj["illustId"].ToString();
            //            detailUrl = SiteUrl + "/artworks/" + id;
            //        }
            //        if (obj["url"] != null)
            //        {
            //            SampleUrl = obj["url"].ToString();
            //        }
            //        Img img = GenerateImg(detailUrl, SampleUrl, id);
            //        if (img != null) imgs.Add(img);
            //    }
            //}

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

                Match regDesc = new Regex(@"illustTitle"":""(.*?)""").Match(page),
                            regAuthor = new Regex(@"userName"":""(.*?)""").Match(page);
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                HtmlAgilityPack.HtmlDocument ds = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(page);
                Pcount = Regex.Match(i.SampleUrl, @"(?<=_p)\d+(?=_)").Value;

                //=================================================
                //[#XX] 标题 - 作者名的插画 - pixiv
                //标题中取名字和作者
                try
                {
                    i.Desc = Regex.Unescape(regDesc.Groups[1].Value);
                    if (srcType == PixivSrcType.Pid)
                        i.Desc += $"{i.Desc} P{Pcount}";
                    i.Author = Regex.Unescape(regAuthor.Groups[1].Value);
                }
                catch { }
                //------------------------
                if (!page.Contains("preload-data") && !page.Contains("globalInitData"))
                {
                    #region 旧版详情页
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
                    #endregion //旧版详情页
                }
                else
                {
                    //+++++191120新版详情页+++++
                    //Match strRex = Regex.Match(page, $@"(?<=(?:{i.Id}:.)).*?(?=(?:.}},user))");

                    //匹配<meta name="global-data" id="meta-global-data" content='中的json数据
                    Match strRex = Regex.Match(page, @"(?<=(""meta-preload-data"")\s*content=').*?(?=('>))");

                    JObject jobj = JObject.Parse(strRex.Value);

                    //Root->illust->PID(图片id)
                    //{"timestamp":"xx","illust":{"id":{xx}},"user":{xx}}
                    JObject jobjPicInfo = (
                        (JObject.Parse($@"{jobj["illust"].First.First.ToSafeString()}"))
                        );

                    i.Date = jobjPicInfo["createDate"].ToSafeString();

                    try
                    {
                        i.Score = int.Parse(jobjPicInfo["likeCount"].ToSafeString());
                        i.Width = int.Parse(jobjPicInfo["width"].ToSafeString());
                        i.Height = int.Parse(jobjPicInfo["height"].ToSafeString());
                        pageCount = int.Parse(jobjPicInfo["pageCount"].ToSafeString());
                    }
                    catch { }

                    jobj = JObject.Parse(jobjPicInfo["urls"].ToSafeString());
                    Regex rex = new Regex(@"(?<=.*)p\d+(?=[^/]*[^\._]*$)");
                    i.PreviewUrl = ThirdPrtyUrl(rex.Replace(jobj["regular"].ToSafeString(), "p" + Pcount));
                    i.JpegUrl = ThirdPrtyUrl(rex.Replace(jobj["small"].ToSafeString(), "p" + Pcount));
                    i.OriginalUrl = ThirdPrtyUrl(rex.Replace(jobj["original"].ToSafeString(), "p" + Pcount));
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
                        string oriul = string.Empty;
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
                        //if (srcType == PixivSrcType.Pid)
                        //{
                        //    try
                        //    {
                        //        page = Sweb.Get(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + Pcount, p, shc);
                        //        ds.LoadHtml(page);
                        //        i.OriginalUrl = ds.DocumentNode.SelectSingleNode("/html/body/img").Attributes["src"].Value;
                        //    }
                        //    catch { }
                        //}
                        //else
                        //{
                        i.Dimension = "Manga " + mangaCount + "P";
                        //+++++191120json 解析+++++
                        page = Sweb.Get($@"{SiteUrl}/ajax/illust/{img.Id}/pages", p, shc);
                        JObject jobj = JObject.Parse(page);
                        JArray jArray = (JArray)(jobj["body"]);
                        for (int j = 0; j < mangaCount; j++)
                        {
                            //保存漫画时优先下载原图 找不到原图则下jpg
                            try
                            {
                                //+++++旧方法+++++
                                //page = Sweb.Get(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + j, p, shc);
                                //ds.LoadHtml(page);
                                //oriul = ds.DocumentNode.SelectSingleNode("/html/body/img").Attributes["src"].Value;

                                //+++++191120json 解析+++++
                                oriul = jArray[j]["urls"]["original"].ToSafeString();
                                oriul = ThirdPrtyUrl(oriul);
                                img.OrignalUrlList.Add(oriul);
                                if (j < 1)
                                    img.OriginalUrl = oriul;
                            }
                            catch
                            {
                                oriul = ThirdPrtyUrl(i.OriginalUrl.Replace("_p0", $"_p{ j }"));
                                img.OrignalUrlList.Add(oriul);
                            }
                        }
                        //}
                    }
                    else if (i.OriginalUrl.Contains("ugoira"))//动图 ugoira  以上面的漫画解析为蓝本修改而来
                    {
                        //为预防Pixiv在未来修改动图页面机制，若连接格式有变则直接抛出异常。
                        if (!i.OriginalUrl.Contains("_ugoira0."))
                        {
                            throw new ArgumentException();
                        }

                        try
                        {
                            i.PixivUgoira = true;//标记动图类型
                            int mangaCount = pageCount;
                            string ugoira_meta = Sweb.Get($"{SiteUrl}/ajax/illust/{i.Id}/ugoira_meta", p, shc);

                            if (!string.IsNullOrWhiteSpace(ugoira_meta))
                            {
                                ugoira_meta = (Convert.ToString(((JObject)JObject.Parse(ugoira_meta)["body"])["frames"]));

                                //直接统计“{”的个数即可知道动图帧数
                                mangaCount = ugoira_meta.Count(c => c == '{');

                                i.Dimension = "Ugoira " + mangaCount + "P";
                                for (int j = 0; j < mangaCount; j++)
                                {
                                    //Generate urls for each frame
                                    i.OrignalUrlList.Add(
                                        ThirdPrtyUrl(i.OriginalUrl.Replace("_ugoira0.", $"_ugoira{j}."))
                                        );
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });

            #region 转换第三方服务的图片地址
            img.SampleUrl = ThirdPrtyUrl(img.SampleUrl);
            img.PreviewUrl = ThirdPrtyUrl(img.PreviewUrl);
            img.JpegUrl = ThirdPrtyUrl(img.JpegUrl);
            img.OriginalUrl = ThirdPrtyUrl(img.OriginalUrl);
            if (img.OrignalUrlList.Count > 0)
            {
                List<string> newOrigUrlList = new List<string>();
                foreach (string origUrl in img.OrignalUrlList)
                {
                    newOrigUrlList.Add(ThirdPrtyUrl(origUrl));
                }
                img.OrignalUrlList = newOrigUrlList;
                newOrigUrlList = null;
            }
            #endregion
            return img;
        }

        /// <summary>
        /// 菜单调用登录
        /// </summary>
        public override void LoginCall(LoginSiteArgs loginArgs)
        {
            if (IsRunLogin || string.IsNullOrWhiteSpace(loginArgs.Cookie)) { return; }
            cookie = loginArgs.Cookie;
            CookieLogin();
        }

        /// <summary>
        /// 用本地Cookie登录
        /// </summary>
        private void CookieLogin()
        {
            IsRunLogin = true;
            IsLoginSite = false;
            string tmp_cookie = cookie, loggedFlags = "login: 'yes'";
            if (string.IsNullOrWhiteSpace(tmp_cookie)) { IsRunLogin = false; return; }

            try
            {
                bool result = false;
                shc.Timeout = shc.Timeout * 2;
                shc.Set("Cookie", tmp_cookie);

                string pageString = Sweb.Get(SiteUrl, SiteManager.Mainproxy, shc);
                result = !string.IsNullOrWhiteSpace(pageString);
                if (!result) { SiteManager.EchoErrLog(SiteName, "登录失败 站点没有响应", true); }

                string[] LFlagsArray = loggedFlags.Split('|');
                foreach (string Flag in LFlagsArray)
                {
                    result &= pageString.Contains(Flag);
                }

                if (result)
                {
                    IsLoginSite = result;
                    cookie = tmp_cookie;
                    Regex rx = new Regex("pixivId\":\"(.*?)\",", RegexOptions.IgnoreCase);
                    GroupCollection group = rx.Match(pageString).Groups;
                    if (group.Count > 1) { nowUser = group[1].Value; }
                    SiteManager.SiteConfig(ShortName, "Login", "Cookie", cookie, true);
                }
                else
                {
                    nowUser = nowPwd = cookie = null;
                    result = IsLoginSite = false;
                }
            }
            catch (Exception e)
            {
                IsLoginSite = false;
                nowUser = nowPwd = cookie = null;
                SiteManager.EchoErrLog(SiteName, $"登录失败\r\n{e.Message}", startLogin);
            }
            finally { IsRunLogin = false; }

        }

        /// <summary>
        /// 扩展设置启用第三方站点服务
        /// </summary>
        private void ExtSetting_EnableThirdParty()
        {
            const int ExtSettingId = 0;
            string isEnable = ExtendedSettings.Count > 0 ? (ExtendedSettings[ExtSettingId].Enable ? extSettingOff : extSettingOn) : extSettingOff;

            if (string.IsNullOrWhiteSpace(isEnable)) { return; }
            enableThirdParty = isEnable.Trim() != extSettingOff;
            ExtendedSettings[ExtSettingId].Enable = enableThirdParty;
            SiteManager.SiteConfig(ShortName, "Cfg", "EnableThirdParty", isEnable, true);
        }

        /// <summary>
        /// 取第三方站点服务图链
        /// </summary>
        /// <param name="imgUrl">原始图片地址</param>
        /// <returns></returns>
        private string ThirdPrtyUrl(string imgUrl)
        {
            //随机数取16%概率使用原地址、减轻pixiv.cat服务器压力
            return enableThirdParty && SiteManager.RandomRNG(1, 100) > 16 ? Regex.Replace(imgUrl, @"i\.[a-zA-Z]+\.net", pixivCat) : imgUrl;
        }

        private void ShowMessage(string text)
        {
            MessageBox.Show(text, ShortName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

    }
}