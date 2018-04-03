using System;
using System.Collections.Generic;
using System.Net;
using MoeLoaderDelta;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace SitePack
{
    class SiteKawaiinyan : AbstractImageSite
    {
        //Tags|Min size,px Orientation Portrait Landscape
        public enum KawaiiSrcType { TagPxO,TagPxP,TagPxL}
        public override string SiteUrl { get { return "https://kawaiinyan.com"; } }
        public override string ShortName{ get{ return "kawaiinyan"; } }
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
        public override bool IsSupportTag { get { return true; } }
        public override bool IsSupportRes { get { return false; } }
        public SiteKawaiinyan(KawaiiSrcType srcType)
        {
            this.srcType = srcType;
        }

        private KawaiiSrcType srcType = KawaiiSrcType.TagPxO;
        private SessionClient Sweb = new SessionClient();
        /// <summary>
        /// kawaiinyan.com
        /// by ulrevenge
        /// ver 1.0
        /// last update at 180330
        /// </summary>
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            //https://kawaiinyan.com/new.json?tags=&size=&orient=
            //https://kawaiinyan.com/new.json?tags=&size=&orient=l
            //https://kawaiinyan.com/new.json?tags=&size=&orient=p
            //https://kawaiinyan.com/new.json?tags=&size=&orient=l&page=2
            string tag = null,px = null,url =null;
            if (keyWord.Contains("|"))
            {
                tag = keyWord.Split('|')[0];
                px = keyWord.Split('|')[1];
            }
            else if (Regex.IsMatch(keyWord, @"\d+"))
                px = keyWord;
            else
                tag = keyWord;
            if (srcType == KawaiiSrcType.TagPxO)
                url = SiteUrl + "/new.json?tags=" + tag +"&size=" +px +"&orient=" +"&page=" + page;
            else if (srcType == KawaiiSrcType.TagPxP)
                url = SiteUrl + "/new.json?tags=" + tag + "&size=" + px + "&orient=p" + "&page=" + page;
            else if (srcType == KawaiiSrcType.TagPxL)
                url = SiteUrl + "/new.json?tags=" + tag + "&size=" + px + "&orient=l" + "&page=" + page;
            string pageString = Sweb.Get(url, proxy, "UTF-8");
            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();
            if (string.IsNullOrWhiteSpace(pageString)) return imgs;
            object[] AllArray = (new JavaScriptSerializer()).DeserializeObject(pageString) as object[];
            string images = null;
            foreach (object o in AllArray)
            {
                Dictionary<string, object> obj = o as Dictionary<string, object>;
                //imges json
                if (obj.ContainsKey("images") && obj["images"] != null)
                    images = obj["images"].ToString();
            }
            if (images != null)
            {
                object[] array = (new JavaScriptSerializer()).DeserializeObject(images) as object[];
                foreach (object o in array)
                {
                    Dictionary<string, object> obj = o as Dictionary<string, object>;
                    string
                        id = "",
                        tags = "",
                        score = "N/A",
                        source = "",
                        sample = "",
                        jpeg_url = "",
                        file_url = "",
                        preview_url = "",
                        author = "";
                    //图片ID
                    if (obj["id"] != null)
                        id = obj["id"].ToString();
                    //投稿者
                    if (obj.ContainsKey("user_name") && obj["user_name"] != null)
                        author = obj["user_name"].ToString();
                    //图片来源
                    if (obj.ContainsKey("adv_link") && obj["adv_link"] != null)
                        source = obj["adv_link"].ToString();
                    //评级和评分
                    if (obj.ContainsKey("yes") && obj.ContainsKey("no"))
                        score = (Convert.ToInt32(obj["yes"].ToString()) 
                            - Convert.ToInt32(obj["no"].ToString())).ToString();
                    //标签
                    if (obj.ContainsKey("tags") && obj["tages"] != null)
                        tags = obj["tags"].ToString();
                    //预览图
                    if(obj.ContainsKey("small") && obj["small"] != null)
                        
                    //jpg
                    //原图
                }
            }
            else return imgs;
        }
    }
}
