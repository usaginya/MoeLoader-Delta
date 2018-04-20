using MoeLoaderDelta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SitePack
{
    /// <summary>
    /// atfbooru.ninja
    /// Change 2018-3-16
    /// by YIU
    /// </summary>
    class SiteATFBooru : AbstractImageSite
    {
        public override string SiteUrl { get { return "https://atfbooru.ninja"; } }
        public override string SiteName { get { return "atfbooru.ninja"; } }
        public override string ShortName { get { return "atfbooru"; } }
        private SiteBooru booru;
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();

        public SiteATFBooru()
        {
            booru = new SiteBooru(
               "https://atfbooru.ninja/post/index.xml?page={0}&limit={1}&tags={2}", "",
               "atfbooru.ninja", "atfbooru", "https://atfbooru.ninja/", false, BooruProcessor.SourceType.XML);
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            string pageString = booru.GetPageString(page, count, keyWord, proxy);
            if (pageString.Contains("錯誤"))
                pageString = booru.GetPageString(page, count + 1, keyWord, proxy);
            if (pageString.Contains("錯誤"))
                pageString = booru.GetPageString(page, count + 2, keyWord, proxy);
            return pageString;
        }

        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();
            try
            {
                string url = string.Format(SiteUrl + "/tags/autocomplete.json?search[name_matches]={0}", word);
                shc.Accept = SessionHeadersValue.AcceptAppJson;
                url = Sweb.Get(url, proxy, "UTF-8", shc);

                object[] jsonobj = (new JavaScriptSerializer()).DeserializeObject(url) as object[];

                foreach (Dictionary<string, object> o in jsonobj)
                {
                    string name = "", count = "";
                    if (o.ContainsKey("name"))
                        name = o["name"].ToString();
                    if (o.ContainsKey("post_count"))
                        count = o["post_count"].ToString();
                    re.Add(new TagItem()
                    {
                        Name = name,
                        Count = count
                    });
                }
            }
            catch { }

            return re.Count > 0 ? re : booru.GetTags(word, proxy);
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = booru.GetImages(pageString, proxy);
            int imgscount = imgs.Count;

            for (int i = 0; i < imgscount; i++)
            {
                string str = imgs[i].SampleUrl;
                if (str.EndsWith("g"))
                {
                    str = str.Substring(0, str.LastIndexOf('.'));
                    StringBuilder sb = new StringBuilder(str);
                    sb.Replace("data//", "data/sample/sample-");
                    sb.Append(".jpg");
                    imgs[i].SampleUrl = sb.ToString();
                }
            }
            return imgs;
        }

    }
}
