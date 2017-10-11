using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 处理Booru类型站点
    /// </summary>
    public class BooruProcessor
    {
        //private int mask = -1;
        //private int maskRes = -1;
        //private bool maskViewed = false;
        //private bool showExplicit = false;
        //private int lastViewed = -1;
        //private ViewedID viewedId;
        private SourceType type;

        /// <summary>
        /// 处理类型
        /// </summary>
        public enum SourceType
        {
            /// <summary>
            /// XML
            /// </summary>
            XML,
            /// <summary>
            /// JSON
            /// </summary>
            JSON,
            /// <summary>
            /// Sankaku JSON
            /// </summary>
            JSONSku,
            /// <summary>
            /// HTML
            /// </summary>
            HTML,
            /// <summary>
            /// XML No Verify
            /// </summary>
            XMLNV,
            /// <summary>
            /// JSON No Verify
            /// </summary>
            JSONNV,
            /// <summary>
            /// HTML No Verify
            /// </summary>
            HTMLNV
        }

        /// <summary>
        /// 获取图片源信息
        /// </summary>
        /// <param name="type">处理类型</param>
        public BooruProcessor(SourceType type)
        {
            //this.mask = mask;
            //this.maskRes = maskRes;
            //this.viewedId = viewedId;
            //this.showExplicit = showExplicit;
            //UseJpeg = useJpeg;
            //Url = url;
            this.type = type;
            //this.maskViewed = maskViewed;
        }
        /*
        //private bool stop = false;
        //public string Url { get; set; }
        //public bool UseJpeg { get; set; }

        /// <summary>
        /// Stop retrieving
        /// </summary>
        //public bool Stop
        //{
        //get { return stop; }
        //set { stop = value; }
        //}

        /// <summary>
        /// Retrieving complete
        /// </summary>
        //public event EventHandler processComplete;

        /// <summary>
        /// Retrieve image objects from a url
        /// </summary>
        /// <param name="url">moe imouto post api url, eg. http://moe.imouto.org/post/index.xml?page=3&limit=10 (limit up to 100)</param>
        //public string ProcessSingleLink(System.Net.IWebProxy proxy)
        //{
        //    //try
        //    //{
        //    //string pageString = null;
        //    //if (PreFetcher.Fetcher.PreFetchUrl == Url)
        //    //{
        //    //pageString = PreFetcher.Fetcher.PreFetchedPage;
        //    //}
        //    //else
        //    //{
        //    MyWebClient web = new MyWebClient();

        //    //web.Proxy = MainWindow.GetProxy(web.Proxy);
        //    web.Proxy = proxy;

        //    //web.Headers["User-Agent"] = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36";

        //    web.Encoding = Encoding.UTF8;
        //    string pageString = web.DownloadString(Url);
        //    web.Dispose();

        //    return pageString;
        //    //}

        //    //if (!stop)
        //    //{
        //    //extract properties
        //    //List<Img> imgs = new List<Img>();

        //    //switch (type)
        //    //{
        //    //    case SourceType.HTML:
        //    //        ProcessHTML(Url, pageString, imgs);
        //    //        break;
        //    //    case SourceType.JSON:
        //    //        ProcessJSON(Url, pageString, imgs);
        //    //        break;
        //    //    case SourceType.XML:
        //    //        ProcessXML(Url, pageString, imgs);
        //    //        break;
        //    //}

        //    //return imgs;
        //    //if (processComplete != null)
        //    //processComplete(imgs, null);
        //    //}
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //if (!stop)
        //    //{
        //    //MessageBox.Show(null, "获取图片遇到错误: " + e.Message, "Moe Loader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    //if (processComplete != null)
        //    //processComplete(null, null);
        //    //}
        //    //}
        //}
        */

        /// <summary>
        /// 提取页面中的图片信息
        /// </summary>
        /// <param name="url">页面地址</param>
        /// <param name="pageString">页面源代码</param>
        /// <returns></returns>
        public List<Img> ProcessPage(string url, string pageString)
        {
            List<Img> imgs = new List<Img>();

            switch (type)
            {
                case SourceType.HTML:
                    ProcessHTML(url, pageString, imgs, "");
                    break;
                case SourceType.JSON:
                    ProcessJSON(url, pageString, imgs, "");
                    break;
                case SourceType.JSONSku:
                    ProcessJSON(url, pageString, imgs, "sku");
                    break;
                case SourceType.XML:
                    ProcessXML(url, pageString, imgs, "");
                    break;
                case SourceType.HTMLNV:
                    ProcessHTML(url, pageString, imgs, "nv");
                    break;
                case SourceType.JSONNV:
                    ProcessJSON(url, pageString, imgs, "nv");
                    break;
                case SourceType.XMLNV:
                    ProcessXML(url, pageString, imgs, "nv");
                    break;
            }

            return imgs;
        }

        /// <summary>
        /// HTML 格式信息
        /// </summary>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        /// <param name="sub">标记 (nv 不验证完整性)</param>
        private void ProcessHTML(string url, string pageString, List<Img> imgs, string sub)
        {
            /* Post.register({"jpeg_height":1200,"sample_width":1333,"md5":"1550bb8d9fa4e1ee7903ee103459f69a","created_at":{"n":666146000,"json_class":"Time","s":1290715184},
             * "status":"active","jpeg_file_size":215756,"sample_height":1000,"score":4,"sample_url":"http://yuinyan.imouto.org/sample/1550bb8d9fa4e459f69a/moe%20163698%20sample.jpg",
             * "actual_preview_height":225,"author":"paku-paku","has_children":false,"change":758975,"height":1200,"sample_file_size":142868,
             * "preview_url":"http://mio3.imouto.org/data/preview/15/50/1550bb8d9fa4e1ee7903ee103459f69a.jpg","tags":"akiyama_mio bikini k-on! swimsuits transparent_png vector_trace",
             * "source":"","width":1600,"rating":"s","jpeg_url":"http://yuinyan.imouto.org/jpeg/1550bb8d9fa4e1ee7903ee103459f69a/moe%20163698%20msuitst_png%20vector_trace.jpg",
             * "preview_width":150,"file_size":113055,"jpeg_width":1600,"preview_height":113,"is_shown_in_index":true,
             * "file_url":"http://yuinyan.imouto.org/image/1550bb8d9fa4e1ee7903ee103459f69a/moe%20163698%20amio%20swimsctor_trace.png",
             * "id":163698,"parent_id":null,"actual_preview_width":300,"creator_id":70875}) */

            //当前字符串位置
            int index = 0;

            while (index < pageString.Length)
            {
                index = pageString.IndexOf("Post.register({", index);
                if (index == -1)
                    break;
                string item = pageString.Substring(index + 14, pageString.IndexOf("})", index) - index - 13);

                #region Analyze json
                //替换有可能干扰分析的 [ ] "
                //item = item.Replace('[', '1').Replace(']', '1').Replace("\\\"", "");
                //JSONObject obj = JSONConvert.DeserializeObject(item);
                Dictionary<string, object> obj = (new JavaScriptSerializer()).DeserializeObject(item) as Dictionary<string, object>;

                string sample = "";
                if (obj.ContainsKey("sample_url"))
                    sample = obj["sample_url"].ToString();

                int file_size = 0;
                try
                {
                    if (obj.ContainsKey("file_size"))
                        file_size = int.Parse(obj["file_size"].ToString());
                }
                catch { }

                string created_at = "N/A";
                if (obj.ContainsKey("created_at"))
                    created_at = obj["created_at"].ToString();

                string preview_url = obj["preview_url"].ToString();
                string file_url = obj["file_url"].ToString();

                string jpeg_url = file_url;
                if (obj.ContainsKey("jpeg_url"))
                    jpeg_url = obj["jpeg_url"].ToString();

                string tags = obj["tags"].ToString();
                string id = obj["id"].ToString();
                string author = obj["author"].ToString();
                string source = obj["source"].ToString();
                //string width = obj["width"].ToString();
                //string height = obj["height"].ToString();
                int width = 0;
                int height = 0;
                try
                {
                    width = int.Parse(obj["width"].ToString().Trim());
                    height = int.Parse(obj["height"].ToString().Trim());
                }
                catch { }

                string score = "N/A";
                if (obj.ContainsKey("rating"))
                {
                    score = "Safe ";
                    if (obj["rating"].ToString() == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (obj.ContainsKey("score"))
                        score += obj["score"].ToString();
                }

                string host = url.Substring(0, url.IndexOf('/', 8));

                preview_url = FormattedImgUrl(host, preview_url);
                file_url = FormattedImgUrl(host, file_url);
                sample = FormattedImgUrl(host, sample);
                jpeg_url = FormattedImgUrl(host, jpeg_url);

                //if (!UseJpeg)
                //jpeg_url = file_url;

                bool noVerify = sub.Length == 2 && sub.Contains("nv");

                Img img = GenerateImg(url, id, author, source, width, height, file_size, created_at, score, sample, preview_url, file_url, jpeg_url, tags, noVerify);
                if (img != null) imgs.Add(img);
                #endregion

                index += 15;
            }
        }

        /// <summary>
        /// XML 格式信息
        /// </summary>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        /// <param name="sub">标记 (nv 不验证完整性)</param>
        private void ProcessXML(string url, string pageString, List<Img> imgs, string sub)
        {

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(pageString);
            }
            catch
            {
                xmlDoc.LoadXml(HttpUtility.HtmlDecode(pageString));
            }

            XmlElement root = null;
            if (xmlDoc.SelectSingleNode("posts") == null)
            {
                root = (XmlElement)(xmlDoc.SelectSingleNode("IbSearch/response")); //root
            }
            else root = (XmlElement)(xmlDoc.SelectSingleNode("posts")); //root

            foreach (XmlNode postN in root.ChildNodes)
            {
                XmlElement post = (XmlElement)postN;

                int file_size = 0;
                try
                {
                    if (post.HasAttribute("file_size"))
                        file_size = int.Parse(post.GetAttribute("file_size"));
                }
                catch { }

                string created_at = "N/A";
                if (post.HasAttribute("created_at"))
                    created_at = post.GetAttribute("created_at");

                string preview_url = post.GetAttribute("preview_url");
                string file_url = post.GetAttribute("file_url");

                string jpeg_url = file_url;
                if (post.HasAttribute("jpeg_url"))
                    jpeg_url = post.GetAttribute("jpeg_url");

                string sample = file_url;
                if (post.HasAttribute("sample_url"))
                    sample = post.GetAttribute("sample_url");

                string tags = post.GetAttribute("tags");
                string id = post.GetAttribute("id");
                string author = post.GetAttribute("author");
                string source = post.GetAttribute("source");
                //string width = post.GetAttribute("width");
                //string height = post.GetAttribute("height");
                int width = 0;
                int height = 0;
                try
                {
                    width = int.Parse(post.GetAttribute("width").Trim());
                    height = int.Parse(post.GetAttribute("height").Trim());
                }
                catch { }

                string score = "N/A";
                if (post.HasAttribute("rating"))
                {
                    score = "Safe ";
                    if (post.GetAttribute("rating") == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (post.HasAttribute("score"))
                        score += post.GetAttribute("score");
                }

                string host = url.Substring(0, url.IndexOf('/', 8));

                preview_url = FormattedImgUrl(host, preview_url);
                file_url = FormattedImgUrl(host, file_url);
                sample = FormattedImgUrl(host, sample);
                jpeg_url = FormattedImgUrl(host, jpeg_url);

                //if (!UseJpeg)
                //jpeg_url = file_url;
                bool noVerify = sub.Length == 2 && sub.Contains("nv");

                Img img = GenerateImg(url, id, author, source, width, height, file_size, created_at, score, sample, preview_url, file_url, jpeg_url, tags, noVerify);
                if (img != null) imgs.Add(img);
            }
        }

        /// <summary>
        /// JSON format
        /// </summary>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        /// <param name="sub">站点标记</param>
        private void ProcessJSON(string url, string pageString, List<Img> imgs, string sub)
        {
            object[] array = (new JavaScriptSerializer()).DeserializeObject(pageString) as object[];
            foreach (object o in array)
            {
                Dictionary<string, object> obj = o as Dictionary<string, object>;
                string
                    id = "",
                    tags = "",
                    host = "",
                    score = "N/A",
                    source = "",
                    sample = "",
                    jpeg_url = "",
                    file_url = "",
                    created_at = "N/A",
                    preview_url = "",
                    author = "";
                int width = 0, height = 0, file_size = 0;
                bool skin_parm;

                //域名
                host = url.Substring(0, url.IndexOf('/', 8));

                //图片ID
                if (obj["id"] != null)
                    id = obj["id"].ToString();

                //投稿者
                if (obj.ContainsKey("author") && obj["author"] != null)
                    author = obj["author"].ToString();

                //图片来源
                if (obj.ContainsKey("source") && obj["source"] != null)
                    source = obj["source"].ToString();

                //原图宽高 width height
                try
                {
                    width = int.Parse(obj["width"].ToString().Trim());
                    height = int.Parse(obj["height"].ToString().Trim());
                }
                catch { }

                //文件大小
                try
                {
                    if (obj.ContainsKey("file_size") && obj["file_size"] != null)
                        file_size = int.Parse(obj["file_size"].ToString());
                }
                catch { }

                //上传时间
                if (obj.ContainsKey("created_at") && obj["created_at"] != null)
                {
                    if (sub == "sku")
                    {
                        Dictionary<string, object> objs = (Dictionary<string, object>)obj["created_at"];
                        if (objs.ContainsKey("s"))
                            created_at = objs["s"].ToString();
                    }
                    else
                    {
                        created_at = obj["created_at"].ToString();
                    }
                }

                //评级和评分
                if (obj.ContainsKey("rating") && obj["rating"] != null)
                {
                    score = "Safe ";
                    if (obj["rating"].ToString() == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (obj.ContainsKey("score"))
                        score += obj["score"].ToString();
                    else if (obj.ContainsKey("total_score"))
                        score += obj["total_score"].ToString();
                }

                //缩略图
                if (obj.ContainsKey("sample_url") && obj["sample_url"] != null)
                    sample = obj["sample_url"].ToString();

                //预览图
                if (obj.ContainsKey("preview_url") && obj["preview_url"] != null)
                    preview_url = obj["preview_url"].ToString();

                //原图

                if (obj.ContainsKey("file_url") && obj["file_url"] != null)
                {
                    file_url = obj["file_url"].ToString();
                    jpeg_url = file_url;
                }

                //JPG
                if (obj.ContainsKey("jpeg_url") && obj["jpeg_url"] != null)
                    jpeg_url = obj["jpeg_url"].ToString();

                //Formatted
                skin_parm = sub.Contains("sku");

                sample = FormattedImgUrl(host, sample, skin_parm);
                preview_url = FormattedImgUrl(host, preview_url, skin_parm);
                file_url = FormattedImgUrl(host, file_url, skin_parm);
                jpeg_url = FormattedImgUrl(host, jpeg_url, skin_parm);


                //标签
                if (obj.ContainsKey("tags") && obj["tags"] != null)
                {
                    if (sub == "sku")
                    {
                        object ov = obj["tags"];
                        StringBuilder ovsb = new StringBuilder();

                        if (ov.GetType().FullName.Contains("Object[]"))
                        {
                            (new JavaScriptSerializer()).Serialize(ov, ovsb);
                            string ovsbstr = ovsb.ToString();
                            object[] ovarr = (new JavaScriptSerializer()).DeserializeObject(ovsbstr) as object[];
                            for (int i = 0; i < ovarr.Count(); i++)
                            {
                                obj = ovarr[i] as Dictionary<string, object>;
                                if (obj.ContainsKey("name"))
                                    tags += i < ovarr.Count() - 1 ? obj["name"] + " " : obj["name"];
                            }
                        }
                    }
                    else
                    {
                        tags = obj["tags"].ToString();
                    }
                }

                bool noVerify = sub.Length == 2 && sub.Contains("nv");

                Img img = GenerateImg(url, id, author, source, width, height, file_size, created_at, score, sample, preview_url, file_url, jpeg_url, tags, noVerify);
                if (img != null) imgs.Add(img);
            }
        }

        /// <summary>
        /// 生成 Img 对象
        /// </summary>
        /// <param name="url"></param>
        /// <param name="id"></param>
        /// <param name="author"></param>
        /// <param name="src"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="file_size"></param>
        /// <param name="created_at"></param>
        /// <param name="score"></param>
        /// <param name="sample"></param>
        /// <param name="preview_url"></param>
        /// <param name="file_url"></param>
        /// <param name="jpeg_url"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        private Img GenerateImg(string url, string id, string author,
            string src, int width, int height, int file_size, string created_at,
            string score, string sample, string preview_url, string file_url, string jpeg_url, string tags, bool noVerify)
        {
            int scoreInt = 0, intId = 0;
            try
            {
                intId = int.Parse(id);
            }
            catch { }
            try
            {
                scoreInt = int.Parse(score.Substring(score.IndexOf(' '), score.Length - score.IndexOf(' ')));
            }
            catch { }

            #region DateTime Convert
            //eg. Fri Aug 28 20:05:57 -0600 2009 or 1291280246
            try
            {
                //1291280246   ==   2010/12/2 16:57
                long sec = long.Parse(created_at);
                DateTime startDate = new DateTime(1970, 1, 1, 8, 0, 0, 0);
                created_at = startDate.AddSeconds(sec).ToString();
            }
            catch
            {
                //Thu Dec 31 06:54:54 +0000 2009
                //2012/01/28 01:59:10 -0500
                //1323123123
                //Dec Nov Oct Sep Aug Jul Jun May Apr Mar Feb Jan
                try
                {
                    created_at = DateTime.Parse(created_at).ToString();
                }
                catch { }
            }
            #endregion

            string host = url.Substring(0, url.IndexOf('/', 8));
            string detailUrl = host + "/post/show/" + id;
            if (url.Contains("index.php"))
                detailUrl = host + "/index.php?page=post&s=view&id=" + id;

            Img img = new Img()
            {
                Date = created_at,
                Desc = tags,
                FileSize = file_size > 1048576 ? (file_size / 1048576.0).ToString("0.00MB") : (file_size / 1024.0).ToString("0.00KB"),
                Height = height,
                Id = intId,
                Author = author == "" ? "UnkwnAuthor" : author,
                IsExplicit = score.StartsWith("E"),
                JpegUrl = jpeg_url,
                OriginalUrl = file_url,
                PreviewUrl = preview_url,
                SampleUrl = sample,
                Score = scoreInt,
                Source = src,
                Tags = tags,
                Width = width,
                DetailUrl = detailUrl,
                NoVerify = noVerify
            };
            return img;
        }

        /// <summary>
        /// 图片地址格式化
        /// 2016年12月对带域名型地址格式化
        /// by YIU
        /// </summary>
        /// <param name="pr_host">图站域名</param>
        /// <param name="pr_url">预处理的URL</param>
        /// <param name="skin_parameters">不处理链接带的参数</param>
        /// <returns>处理后的图片URL</returns>
        public static string FormattedImgUrl(string pr_host, string pr_url, bool skin_parameters)
        {
            //System.Diagnostics.Trace.WriteLine("host: " + pr_host);
            try
            {
                //域名处理 - 如果有
                if (!string.IsNullOrWhiteSpace(pr_host))
                {
                    int po = pr_host.IndexOf("//");
                    string phh = pr_host.Substring(0, pr_host.IndexOf(':') + 1);
                    string phu = pr_host.Substring(po, pr_host.Length - po);

                    //地址中有主域名 去掉主域名
                    if (pr_url.StartsWith(phu))
                        pr_url = pr_host + pr_url.Replace(phu, "");

                    //地址中有子域名 补完子域名
                    else if (pr_url.StartsWith("//"))
                        pr_url = phh + pr_url;

                    //地址没有域名 补完地址
                    else if (pr_url.StartsWith("/"))
                        pr_url = pr_host + pr_url;
                }
                //过滤图片地址?后的内容
                if (!skin_parameters && pr_url.Contains("?"))
                    pr_url = pr_url.Substring(0, pr_url.LastIndexOf('?'));

                return pr_url;
            }
            catch
            {
                return pr_url;
            }
        }

        /// <summary>
        /// 图片地址格式化-All
        /// </summary>
        /// <param name="pr_host">图站域名</param>
        /// <param name="pr_url">预处理的URL</param>
        /// <returns>处理后的图片URL</returns>
        public static string FormattedImgUrl(string pr_host, string pr_url)
        {
            return FormattedImgUrl(pr_host, pr_url, false);
        }

    }
}
