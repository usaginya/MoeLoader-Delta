using System;
using System.Collections.Generic;
using System.Net;
using MoeLoaderDelta;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace SitePack
{
    class SiteKawaiinyan : AbstractImageSite
    {
        /// <summary>
        /// kawaiinyan.com
        /// by ulrevenge
        /// ver 1.0
        /// last change 180417
        /// </summary>

        //Tags|Min size,px Orientation Portrait Landscape
        public enum KawaiiSrcType { TagPxO, TagPxP, TagPxL }
        public override string SiteUrl { get { return "https://kawaiinyan.com"; } }
        public override string ShortName { get { return "kawaiinyan"; } }
        public override string SiteName
        {
            get
            {
                if (srcType == KawaiiSrcType.TagPxP)
                    return "kawaiinyan.com [Portrait]";
                else if (srcType == KawaiiSrcType.TagPxL)
                    return "kawaiinyan.com [Landscape]";
                return "kawaiinyan.com [Orientation]";
            }
        }
        public override string ShortType
        {
            get
            {
                if (srcType == KawaiiSrcType.TagPxP)
                    return "[P]";
                else if (srcType == KawaiiSrcType.TagPxL)
                    return "[L]";
                return "[O]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (srcType == KawaiiSrcType.TagPxP)
                    return "标签|最小分辨率(单值)\r\nPortrait 立绘图";
                else if (srcType == KawaiiSrcType.TagPxL)
                    return "标签|最小分辨率(单值)\r\nLandscape 有风景的图";
                return "标签|最小分辨率(单值)\r\nOrientation";
            }
        }
        public override bool IsSupportCount { get { return false; } }
        public override bool IsSupportTag { get { return false; } }
        public override bool IsSupportRes { get { return false; } }
        public override bool IsShowRes { get { return false; } }
        public SiteKawaiinyan(KawaiiSrcType srcType)
        {
            this.srcType = srcType;
        }

        private KawaiiSrcType srcType = KawaiiSrcType.TagPxO;
        private SessionClient Sweb = new SessionClient();
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            //https://kawaiinyan.com/new.json?tags=&size=&orient=
            //https://kawaiinyan.com/new.json?tags=&size=&orient=l
            //https://kawaiinyan.com/new.json?tags=&size=&orient=p
            //https://kawaiinyan.com/new.json?tags=&size=&orient=l&page=2
            string tag = null, px = null, url = null;
            if (keyWord.Contains("|"))
            {
                tag = keyWord.Split('|')[0];
                px = keyWord.Split('|')[1];
            }
            else if (Regex.IsMatch(keyWord, @"\d+"))
                px = keyWord;
            else
                tag = keyWord;

            url = SiteUrl + "/new.json?tags=" + tag + "&size=" + px;
            if (srcType == KawaiiSrcType.TagPxO)
                url += "&orient=&page=" + page;
            else if (srcType == KawaiiSrcType.TagPxP)
                url += "&orient=p&page=" + page;
            else if (srcType == KawaiiSrcType.TagPxL)
                url += "&orient=l&page=" + page;
            string pageString = Sweb.Get(url, proxy);
            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();
            string imagesJson = null;
            if (string.IsNullOrWhiteSpace(pageString)) return imgs;
            //外层Json
            JObject jsonObj = JObject.Parse(pageString);
            //取得images Json 字符串
            if (jsonObj["images"].ToString() != null)
                imagesJson = jsonObj["images"].ToString();
            if (string.IsNullOrWhiteSpace(imagesJson)) return imgs;
            //解析images Json
            object[] array = (new JavaScriptSerializer()).DeserializeObject(imagesJson) as object[];
            foreach (object o in array)
            {
                Dictionary<string, object> obj = o as Dictionary<string, object>;

                string
                id = "0",
                subUrl = "",
                tags = "",
                score = "N/A",
                source = "0",
                sample = "",
                jpeg_url = "",
                file_url = "",
                preview_url = "",
                author = "",
                detailUrl = "";
                //图片ID
                if (obj.ContainsKey("id") && obj["id"] != null)
                {
                    id = obj["id"].ToString();
                    //图片子站
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("https://");
                    sb.Append((Convert.ToInt32(id) % 10).ToString());
                    sb.Append(".s.");
                    sb.Append(ShortName);
                    sb.Append(".com/i");
                    subUrl = sb.ToString();
                }
                //投稿者
                if (obj.ContainsKey("user_name") && obj["user_name"] != null)
                    author = obj["user_name"].ToString();
                //图片来源
                if (obj.ContainsKey("adv_link") && obj["adv_link"] != null)
                    source = obj["adv_link"].ToString();
                //评级和评分
                if (obj.ContainsKey("yes") && obj["yes"] != null)
                    if (obj.ContainsKey("no") && obj["no"] != null)
                        score = (Convert.ToInt32(obj["yes"].ToString())
                        - Convert.ToInt32(obj["no"].ToString())).ToString();
                //标签
                if (obj.ContainsKey("tags") && obj["tags"] != null)
                    tags = obj["tags"].ToString();
                //缩略图 small 显示不全
                if (obj.ContainsKey("small") && obj["small"] != null)
                {
                    sample = subUrl + StringJoinString(id) + "/" + "small." + obj["small"].ToString();
                    //https://kawaiinyan.com/new#i27963.jpg
                    detailUrl = SiteUrl + "/new#i" + id + "." + obj["small"].ToString();
                }
                //jpg 预览图
                if (obj.ContainsKey("big") && obj["big"] != null)
                    preview_url = jpeg_url = file_url = subUrl + StringJoinString(id) + "/" + "big." + obj["big"].ToString();

                //原图
                if (obj.ContainsKey("orig") && obj["orig"] != null)
                    file_url = subUrl + StringJoinString(id) + "/" + "orig." + obj["orig"].ToString();

                Img img = new Img
                {
                    Desc = tags,
                    Id = Convert.ToInt32(id),
                    Author = author,
                    JpegUrl = jpeg_url,
                    OriginalUrl = file_url,
                    PreviewUrl = preview_url,
                    SampleUrl = sample,
                    Score = Convert.ToInt32(score),
                    Source = source,
                    Tags = tags,
                    DetailUrl = detailUrl,
                };
                imgs.Add(img);
            }
            return imgs;

        }
        private string StringJoinString(string id)
        {
            int len;
            if (id.Length % 2 == 0)
                len = id.Length - 1;
            else
                len = id.Length;
            for (int a = 0; a <= len / 2; a++)
                id = id.Insert(a + 2 * a, "/");
            return id;
        }
    }
}
