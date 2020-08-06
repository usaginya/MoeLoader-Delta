using MoeLoaderDelta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SitePack
{
    /// <summary>
    /// yuriimg.com
    /// Last change 200807
    /// </summary>
    public class SiteYuriimg : AbstractImageSite
    {
        public override string SiteUrl => "https://yuriimg.com";
        public override string ShortName => "yuriimg";
        public override string SiteName => "yuriimg.com";
        public override bool IsSupportCount => false;
        public override string Referer => "https://yuriimg.com";
        public override bool IsSupportTag => false;
        public override string SubReferer => ShortName;
        public override string LoginURL => SiteManager.SiteLoginType.FillIn.ToSafeString();
        public override string LoginUser { get => nowUser; set => nowUser = value; }
        public override string LoginPwd { get => nowPwd; set => nowPwd = value; }
        public override bool LoginSiteIsLogged => IsLoginSite;

        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private static string authorization = string.Empty, nowUser = authorization, nowPwd = authorization;
        private static bool IsLoginSite = false, IsRunLogin = IsLoginSite, onceLogin = true;

        private const string mainApiUrl = "https://api.yuriimg.com/";
        private readonly string loginUrl = $"{mainApiUrl}login";
        private readonly string postsUrl = $"{mainApiUrl}posts?";
        private readonly string postUrl = $"{mainApiUrl}post/";
        private readonly string imgHostUrl = "https://i.yuriimg.com/";

        /// <summary>
        /// 初始狗子
        /// </summary>
        public SiteYuriimg()
        {
            shc.Referer = SiteUrl;
            shc.Accept = SessionHeadersValue.AcceptAppJson;
            shc.ContentType = SessionHeadersValue.AcceptAppJson;
            shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
        }

        /// <summary>
        /// 取页面内容
        /// </summary>
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            //自动登录
            if (onceLogin && !IsLoginSite)
            {
                onceLogin = false;
                LoadUser();
                LoginCall(new LoginSiteArgs() { User = nowUser, Pwd = nowPwd });
            }

            //获取
            string pageString = Sweb.Get($"{postsUrl}{(string.IsNullOrWhiteSpace(keyWord) ? string.Empty : $"tags={keyWord}&")}mode=&page={page}", proxy, shc);
            return string.IsNullOrWhiteSpace(pageString) ? string.Empty : pageString;
        }

        /// <summary>
        /// 解析图片
        /// </summary>
        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> imgs = new List<Img>();
            JArray jPage = (JArray)JObject.Parse(pageString)["posts"];

            foreach (JObject post in jPage)
            {
                Img img = new Img()
                {
                    Desc = $"{post["id"]}",
                    Id = $"{post["pid"]}".ToSafeInt(),
                    Width = $"{post["width"]}".ToSafeInt(),
                    Height = $"{post["height"]}".ToSafeInt(),
                    IsExplicit = $"{post["rating"]}" == "e",
                    DetailUrl = $"{SiteUrl}/show/{post["id"]}",
                    SampleUrl = ParseImageUrl(post),
                    DownloadDetail = new DetailHandler(ImgDetail)
                };
                if (!IsLoginSite && img.IsExplicit) { continue; }
                imgs.Add(img);
            }
            return imgs;
        }

        /// <summary>
        /// 详细图片详情页
        /// </summary>
        private void ImgDetail(Img img, IWebProxy proxy)
        {
            string pageString = Sweb.Get($"{postUrl}{img.Desc}", proxy, shc);
            if (string.IsNullOrWhiteSpace(pageString)) { return; }
            JObject json = JObject.Parse(pageString);

            img.Tags = ParseTags(json);
            img.Desc = img.Tags;
            img.Author = json["artist"].Type == JTokenType.Object
                ? $"{json["artist"]["name"]}"
                : $"{json["user"]["name"]}";
            img.Score = $"{json["praise"]}".ToSafeInt();
            img.Date = $"{json["format_date"]}";
            img.FileSize = ParseFileSize(json);
            img.PreviewUrl = ParseImageUrl(json, 1);
            img.OriginalUrl = ParseImageUrl(json, 2);
            img.JpegUrl = img.PreviewUrl;

            // if 多图
            if ($"{json["page_count"]}".ToSafeInt() > 1)
            {
                img.Desc = $"P{json["page_count"]} {img.Desc}";
                pageString = Sweb.Get($"{postUrl}{json["pid"]}/multi", proxy, shc);
                if (string.IsNullOrWhiteSpace(pageString)) { return; }
                JArray json2 = JArray.Parse(pageString);

                foreach (JObject img2 in json2)
                {
                    img.OrignalUrlList.Add(ParseImageUrl(img2, 2));
                }
            }
        }

        /// <summary>
        /// 转换FileSize
        /// </summary>
        private string ParseFileSize(JObject json)
        {
            int fileSize = $"{json["size"]}".ToSafeInt();
            return fileSize > 1048576 ? (fileSize / 1048576.0).ToString("0.00MB") : (fileSize / 1024.0).ToString("0.00KB");
        }

        /// <summary>
        /// 拼接Tags
        /// </summary>
        private string ParseTags(JObject json)
        {
            StringBuilder stringBuilder = new StringBuilder();
            JObject tags = (JObject)json["tags"];
            List<JArray> tagsArray = new List<JArray>();
            if (CheckJsonKey(tags, "character")) { tagsArray.Add((JArray)tags["character"]); }
            if (CheckJsonKey(tags, "copyright")) { tagsArray.Add((JArray)tags["copyright"]); }
            if (CheckJsonKey(tags, "general")) { tagsArray.Add((JArray)tags["general"]); }
            foreach (JArray tag in tagsArray)
            {
                tag.ForEach(t => stringBuilder.Append($"{t["tags"]["jp"]} "));
            }
            return stringBuilder.ToSafeString();
        }

        /// <summary>
        /// 图片地址拼接
        /// </summary>
        /// <param name="type">图片类型 0缩略图 1预览图 2原图</param>
        private string ParseImageUrl(JObject json, int type = 0)
        {
            //注意连接字符前后空格
            string fileExt = $"{json["file_ext"]}";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{imgHostUrl}{json["src"]}/");
            stringBuilder.Append($"{SiteName} ");
            stringBuilder.Append($"{json["id"]} ");
            if (!string.IsNullOrWhiteSpace(fileExt))
            {
                switch (type)
                {
                    case 1: stringBuilder.Append("thumb"); break;
                    case 2: stringBuilder.Append($"{fileExt}"); break;
                    default: stringBuilder.Append("contain"); break;
                }
            }
            else { stringBuilder.Append("contain"); }
            stringBuilder.Append($"{(json["page"] == null ? string.Empty : $" p{ $"{ json["page"]}".ToSafeInt() + 1 }")}");
            stringBuilder.Append($".{(string.IsNullOrWhiteSpace(fileExt) ? "webp" : fileExt)}");
            return stringBuilder.ToSafeString();
        }

        /// <summary>
        /// 登录调用
        /// </summary>
        public override void LoginCall(LoginSiteArgs loginArgs)
        {
            if (IsRunLogin || string.IsNullOrWhiteSpace(loginArgs.User) || string.IsNullOrWhiteSpace(loginArgs.Pwd)) { return; }
            nowUser = loginArgs.User;
            nowPwd = loginArgs.Pwd;
            Login();
        }

        /// <summary>
        /// 登录
        /// </summary>
        private void Login()
        {
            IsRunLogin = true;
            IsLoginSite = false;
            try
            {
                JObject user = new JObject
                {
                    ["name"] = nowUser,
                    ["password"] = nowPwd
                };
                string post = JsonConvert.SerializeObject(user);

                //Post登录取Authorization
                post = Sweb.Post(loginUrl, post, SiteManager.Mainproxy, shc);
                if (string.IsNullOrWhiteSpace(post) || !post.Contains("{"))
                {
                    IsRunLogin = false;
                    nowUser = nowPwd = null;
                    SiteManager.EchoErrLog(ShortName, $"登录失败 - {post}{(post.Contains("401") ? $"{Environment.NewLine}账号不正确？" : string.Empty)}", !onceLogin);
                    return;
                }

                JObject jobj = JObject.Parse(post);
                if (jobj.Property("token") != null)
                {
                    authorization = $"Bearer {jobj["token"]}";
                }
                else if (jobj.Property("message") != null)
                {
                    IsRunLogin = false;
                    nowUser = nowPwd = null;
                    SiteManager.EchoErrLog(ShortName, $"登录失败 - {jobj["message"]}", !onceLogin);
                    return;
                }

                if (string.IsNullOrWhiteSpace(authorization))
                {
                    IsRunLogin = false;
                    nowUser = nowPwd = null;
                    SiteManager.EchoErrLog(ShortName, "登录失败 - 获取账号令牌失败", !onceLogin);
                    return;
                }

                //登录成功
                shc.Add(HttpRequestHeader.Authorization, authorization);
                IsLoginSite = true;
                SaveUser();
            }
            catch (Exception e)
            {
                nowUser = nowPwd = null;
                SiteManager.EchoErrLog(ShortName, e, $"登录失败 - {e.Message}", !onceLogin);
            }
            IsRunLogin = false;
        }

        /// <summary>
        /// 保存账号
        /// </summary>
        private void SaveUser()
        {
            SiteManager.SiteConfig(ShortName, "Login", "User", nowUser, true);
            SiteManager.SiteConfig(ShortName, "Login", "Pwd", nowPwd, true);
        }

        /// <summary>
        /// 载入账号
        /// </summary>
        private void LoadUser()
        {
            nowUser = SiteManager.SiteConfig(ShortName, "Login", "User");
            nowPwd = SiteManager.SiteConfig(ShortName, "Login", "Pwd");
        }

        /// <summary>
        /// 检查JSON对象是否存在某键值
        /// </summary>
        private bool CheckJsonKey(JObject jObject, string jKey)
        {
            return !string.IsNullOrWhiteSpace(jObject.Property(jKey).ToSafeString());
        }
    }

}