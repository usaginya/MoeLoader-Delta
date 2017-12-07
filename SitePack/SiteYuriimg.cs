using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MoeLoaderDelta;
using System.Linq;
using System.Net;
using System.IO;

namespace SitePack
{
    /// <summary>
    /// yuriimg.com
    /// </summary>
    class SiteYuriimg : AbstractImageSite
    {
        private static string cookie = "";
        private string user = "mluser1";
        private string pass = "ml1yuri";
        public override string SiteUrl { get { return "http://yuriimg.com"; } }
        public override string ShortName { get { return "yuriimg"; } }
        public override string SiteName { get { return "yuriimg.com"; } }
        public override string ShortType { get { return ""; } }
        public override bool IsSupportCount { get { return false; } }
        public override string Referer { get { return "http://yuriimg.com"; } }
        public override bool IsSupportTag { get { return false; } }
        public override string SubReferer { get { return ShortName; } }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            Login(proxy);
            //http://yuriimg.com/post/?.html
            string url = SiteUrl + "/post/" + page + ".html";
            // string url = "http://yuriimg.com/show/ge407xd5o.jpg";

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;
            web.Headers["Cookie"] = cookie;

            if (keyWord.Length > 0)
            {
                //http://yuriimg.com/search/index/tags/?/p/?.html
                url = SiteUrl + "/search/index/tags/" + keyWord + "/p/" + page + ".html";
            }
            string pageString = web.DownloadString(url);
            web.Dispose();

            return pageString;
        }

        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {

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
                    PreviewUrl = imgNode.Attributes["data-original"].Value,
                    //JpegUrl = SiteUrl + imgNode.Attributes["data-viewersss"].Value,
                    Id = StringToInt(imgNode.Attributes["id"].Value),
                    DetailUrl = SiteUrl + imgNode.Attributes["data-href"].Value,
                    Score = Convert.ToInt32(imageItem.SelectSingleNode(".//span[@class='num']").InnerText)
                };

                item.DownloadDetail = (i, p) =>
                {

                    MyWebClient web = new MyWebClient();
                    web.Proxy = p;
                    web.Encoding = Encoding.UTF8;
                    web.Headers["Cookie"] = cookie;
                    string html = web.DownloadString(i.DetailUrl);

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
                    i.SampleUrl = doc.DocumentNode.SelectSingleNode("//figure[@class=\'show-image\']/img").Attributes["src"].Value;
                    if (Regex.Matches(imgDownNode.OuterHtml, "href").Count > 1)
                    {
                        i.OriginalUrl = SiteUrl + imgDownNode.SelectSingleNode("./a[1]").Attributes["href"].Value;
                        i.JpegUrl = i.OriginalUrl;
                        i.FileSize = Regex.Match(imgDownNode.SelectSingleNode("./a[1]").InnerText, @"(?<=().*?(?=))").Value;
                    }
                    else
                    {
                        i.OriginalUrl = SiteUrl + imgDownNode.SelectSingleNode("./a").Attributes["href"].Value;
                        i.JpegUrl = i.OriginalUrl;
                        i.FileSize = Regex.Match(imgDownNode.SelectSingleNode("./a").InnerText, @"(?<=().*?(?=))").Value;
                    }
                };
                list.Add(item);
            }

            return list;
        }

        private int StringToInt(string id)
        {
            string str = id.Trim();                            // 去掉字串首尾處的空格
            char[] charBuf = str.ToArray();                    // 將字串轉換為字元數組
            ASCIIEncoding charToASCII = new ASCIIEncoding();
            byte[] TxdBuf = new byte[charBuf.Length];          // 定義發送緩衝區；
            TxdBuf = charToASCII.GetBytes(charBuf);
            int idOut = BitConverter.ToInt32(TxdBuf, 0);
            return idOut;
        }

        private void Login(IWebProxy proxy)
        {
            //第二次上傳帳戶密碼,使cookie可以用於登入
            if (!cookie.Contains("otome_"))
            {
                try
                {
                    HttpWebRequest postRequest = (HttpWebRequest)WebRequest.Create("http://yuriimg.com/account/login");
                    HttpWebResponse postResponse;

                    // 生成邊界符
                    string boundary = "---------------" + DateTime.Now.Ticks.ToString("x");
                    // post資料中的邊界符
                    string pboundary = "--" + boundary;
                    // 最後的結束符
                    string endBoundary = "--" + boundary + "--\r\n";
                    // post資料
                    string postData = pboundary + "\r\nContent-Disposition: form-data; name=\"username\"\r\n\r\n"
                        + user + "\r\n" + pboundary
                        + "\r\nContent-Disposition: form-data; name=\"password\"\r\n\r\n"
                        + pass + "\r\n" + endBoundary;

                    // 設定屬性
                    postRequest.Proxy = proxy;
                    postRequest.Method = "POST";
                    postRequest.Timeout = 8000;
                    postRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                    postRequest.Accept = "application/json";
                    postRequest.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                    postRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                    postRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                    //postRequest.Headers.Add("Cookie", cookie);
                    postRequest.Referer = "http://yuriimg.com/account/login";
                    postRequest.KeepAlive = true;
                    postRequest.AllowAutoRedirect = false;
                    //postRequest.CookieContainer = cookieContainer;
                    postRequest.AutomaticDecompression = DecompressionMethods.GZip;
                    // 上傳post資料
                    byte[] bt_postData = Encoding.UTF8.GetBytes(postData);
                    postRequest.ContentLength = bt_postData.Length;
                    Stream writeStream = postRequest.GetRequestStream();
                    writeStream.Write(bt_postData, 0, bt_postData.Length);
                    writeStream.Close();

                    //獲取響應
                    postResponse = (HttpWebResponse)postRequest.GetResponse();
                    Stream responseStream = postResponse.GetResponseStream();
                    string resData = "";
                    StreamReader resSR = new StreamReader(responseStream, Encoding.UTF8);

                    resData = resSR.ReadToEnd();
                    resSR.Close();
                    responseStream.Close();

                    cookie = postResponse.Headers["Set-Cookie"];

                    if (resData.Contains("-2"))
                    {
                        throw new Exception("密碼錯誤");
                    }
                    else if ((!cookie.Contains("otome_")))
                    {
                        throw new Exception("登入時出錯");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message.TrimEnd("。".ToCharArray()) + "自動登入失敗");
                }
            }
        }
        private string TimeConvert(string html)
        {
            string date = Regex.Match(html, @"(?<=<span>).*?(?=</span>)").Value;
            if (date.Contains("時前"))
            {
                date =DateTime.Now.AddHours(-Convert.ToDouble(Regex.Match(date, @"\d+").Value)).ToString("yyyy-MM-dd hh.mm");
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
