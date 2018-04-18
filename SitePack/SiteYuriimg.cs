using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MoeLoaderDelta;
using System.Linq;
using System.Net;

namespace SitePack
{
    /// <summary>
    /// yuriimg.com
    /// Last change 180417
    /// </summary>
    class SiteYuriimg : AbstractImageSite
    {
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private static string cookie = "";
        private string user = "mluser1";
        private string pass = "ml1yuri";
        public override string SiteUrl { get { return "http://yuriimg.com"; } }
        public override string ShortName { get { return "yuriimg"; } }
        public override string SiteName { get { return "yuriimg.com"; } }
        public override bool IsSupportCount { get { return false; } }
        public override string Referer { get { return "http://yuriimg.com"; } }
        public override bool IsSupportTag { get { return false; } }
        public override string SubReferer { get { return ShortName; } }
        public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(250, 250); } }

        public SiteYuriimg()
        {
            CookieRestore();
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            Login(proxy);
            //http://yuriimg.com/post/?.html
            string url = SiteUrl + "/post/" + page + ".html";
            // string url = "http://yuriimg.com/show/ge407xd5o.jpg";

            if (keyWord.Length > 0)
            {
                //http://yuriimg.com/search/index/tags/?/p/?.html
                url = SiteUrl + "/search/index/tags/" + keyWord + "/p/" + page + ".html";
            }

            shc.Remove("Accept-Ranges");
            shc.Accept = SessionHeadersValue.AcceptTextHtml;
            shc.ContentType = SessionHeadersValue.AcceptTextHtml;
            string pageString = Sweb.Get(url, proxy, "UTF-8", shc);

            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            shc.Add("Accept-Ranges", "bytes");
            shc.ContentType = SessionHeadersValue.ContentTypeAuto;
            List<Img> list = new List<Img>();

            HtmlDocument dococument = new HtmlDocument();
            dococument.LoadHtml(pageString);
            HtmlNodeCollection imageItems = dococument.DocumentNode.SelectNodes("//*[@class='image-list cl']");
            if (imageItems == null)
            {
                return list;
            }
            foreach (HtmlNode imageItem in imageItems)
            {
                HtmlNode imgNode = imageItem.SelectSingleNode("./div[1]/img");
                string tags = imgNode.Attributes["alt"].Value;
                Img item = new Img()
                {
                    Height = Convert.ToInt32(imageItem.SelectSingleNode(".//div[@class='image']").Attributes["data-height"].Value),
                    Width = Convert.ToInt32(imageItem.SelectSingleNode(".//div[@class='image']").Attributes["data-width"].Value),
                    Author = imageItem.SelectSingleNode("//small/a").InnerText,
                    IsExplicit = false,
                    Tags = tags,
                    Desc = tags,
                    SampleUrl = imgNode.Attributes["data-original"].Value.Replace("!single","!320px"),
                    //JpegUrl = SiteUrl + imgNode.Attributes["data-viewersss"].Value,
                    Id = StringToInt(imgNode.Attributes["id"].Value),
                    DetailUrl = SiteUrl + imgNode.Attributes["data-href"].Value,
                    Score = Convert.ToInt32(imageItem.SelectSingleNode(".//span[@class='num']").InnerText)
                };

                item.DownloadDetail = (i, p) =>
                {
                    string html = Sweb.Get(i.DetailUrl, proxy, "UTF-8", shc);

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    HtmlNode showIndexs = doc.DocumentNode.SelectSingleNode("//div[@class='logo']");
                    HtmlNode imgDownNode = showIndexs.SelectSingleNode("//div[@class='img-control']");
                    string nodeHtml = showIndexs.OuterHtml;
                    i.Date = TimeConvert(nodeHtml);

                    if (nodeHtml.Contains("pixiv page"))
                    {
                        i.Source = showIndexs.SelectSingleNode(".//a[@target='_blank']").Attributes["href"].Value;
                    }
                    else
                    {
                        i.Source = Regex.Match(nodeHtml, @"(?<=源地址).*?(?=</p>)").Value.Trim();
                    }
                    i.PreviewUrl = doc.DocumentNode.SelectSingleNode("//figure[@class=\'show-image\']/img").Attributes["src"].Value;
                    if (Regex.Matches(imgDownNode.OuterHtml, "href").Count > 1)
                    {
                        i.OriginalUrl = SiteUrl + imgDownNode.SelectSingleNode("./a[1]").Attributes["href"].Value;
                        i.FileSize = Regex.Match(imgDownNode.SelectSingleNode("./a[1]").InnerText, @"(?<=().*?(?=))").Value;
                    }
                    else
                    {
                        i.OriginalUrl = SiteUrl + imgDownNode.SelectSingleNode("./a").Attributes["href"].Value;
                        i.FileSize = Regex.Match(imgDownNode.SelectSingleNode("./a").InnerText, @"(?<=().*?(?=))").Value;
                    }
                    i.JpegUrl = i.PreviewUrl.Length > 0 ? i.PreviewUrl : i.OriginalUrl;
                };
                list.Add(item);
            }

            return list;
        }

        private int StringToInt(string id)
        {
            string str = id.Trim();                            // 去掉字符串首尾处的空格
            char[] charBuf = str.ToArray();                    // 将字符串转换为字符数组
            ASCIIEncoding charToASCII = new ASCIIEncoding();
            byte[] TxdBuf = new byte[charBuf.Length];          // 定义发送缓冲区；
            TxdBuf = charToASCII.GetBytes(charBuf);
            int idOut = BitConverter.ToInt32(TxdBuf, 0);
            return idOut;
        }

        /// <summary>
        /// 还原Cookie
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
            //第二次上传账户密码,使cookie可以用于登录
            if (!cookie.Contains("otome_"))
            {
                try
                {
                    string loginUrl = "http://yuriimg.com/account/login";

                    /*
                     * 开始边界符
                     * 分隔边界符
                     * 结束边界符
                     * Post数据
                     */
                    string
                        boundary = "---------------" + DateTime.Now.Ticks.ToString("x"),
                        pboundary = "--" + boundary,
                        endBoundary = "--" + boundary + "--\r\n",
                        postData = pboundary + "\r\nContent-Disposition: form-data; name=\"username\"\r\n\r\n"
                        + user + "\r\n" + pboundary
                        + "\r\nContent-Disposition: form-data; name=\"password\"\r\n\r\n"
                        + pass + "\r\n" + endBoundary;

                    string retData = "";

                    cookie = "";
                    shc.Referer = Referer;
                    shc.AllowAutoRedirect = false;
                    shc.Accept = SessionHeadersValue.AcceptAppJson;
                    shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
                    shc.ContentType = SessionHeadersValue.ContentTypeFormData + "; boundary=" + boundary;
                    shc.AutomaticDecompression = DecompressionMethods.GZip;
                    shc.Remove("Accept-Ranges");

                    retData = Sweb.Post(loginUrl, postData, proxy, "UTF-8", shc);
                    cookie = Sweb.GetURLCookies(SiteUrl);

                    if (retData.Contains("-2"))
                    {
                        throw new Exception("密码错误");
                    }
                    else if ((!cookie.Contains("otome_")))
                    {
                        throw new Exception("登录时出错");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message.TrimEnd("。".ToCharArray()) + "自动登录失败");
                }
            }
        }
        private string TimeConvert(string html)
        {
            string date = Regex.Match(html, @"(?<=<span>).*?(?=</span>)").Value;
            if (date.Contains("时前"))
            {
                date = DateTime.Now.AddHours(-Convert.ToDouble(Regex.Match(date, @"\d+").Value)).ToString("yyyy-MM-dd hh.mm");
            }
            else if (date.Contains("天前"))
            {
                date = DateTime.Now.AddDays(-Convert.ToDouble(Regex.Match(date, @"\d+").Value)).ToString("yyyy-MM-dd hh.mm");
            }
            else if (date.Contains("月前"))
            {
                date = DateTime.Now.AddMonths(-Convert.ToInt32(Regex.Match(date, @"\d+").Value)).ToString("yyyy-MM-dd hh.mm");
            }
            return date;
        }
    }
}