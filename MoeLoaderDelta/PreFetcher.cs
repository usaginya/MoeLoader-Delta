using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows.Media;

namespace MoeLoaderDelta
{
    class PreFetcher
    {

        private PreFetcher() { }
        private static PreFetcher fetcher;
        public static PreFetcher Fetcher
        {
            get
            {
                if (fetcher == null)
                    fetcher = new PreFetcher();
                return fetcher;
            }
        }

        private static int cacheimgcount = 6;
        /// <summary>
        /// 缓存的图片数量,最大20,最少1
        /// </summary>
        public static int CachedImgCount
        {
            get
            {
                return cacheimgcount;
            }
            set
            {
                if (value > 20)
                    value = 20;
                else if (value < 1)
                    value = 1;

                cacheimgcount = value;
            }
        }

        //预先加载的url
        //public string PreFetchUrl { get; set; }
        private int prePage, preCount;
        private string preWord;
        private IMageSite preSite;
        private string preFetchedPage;
        //预加载的页面内容
        public string GetPreFetchedPage(int page, int count, string word, IMageSite site)
        {
            if (page == prePage && count == preCount && word == preWord && site == preSite)
            {
                return preFetchedPage;
            }
            else return null;
        }

        //预加载的缩略图
        private Dictionary<string, ImageSource> preFetchedImg = new Dictionary<string, ImageSource>(CachedImgCount);

        /// <summary>
        /// 预加载的缩略图
        /// </summary>
        /// <param name="url">缩略图url</param>
        /// <returns>缩略图，或者 null 若未加载</returns>
        public ImageSource PreFetchedImg(string url)
        {
            if (preFetchedImg.ContainsKey(url))
                return preFetchedImg[url];
            else return null;
        }

        //private System.Net.HttpWebRequest req;
        private List<HttpWebRequest> imgReqs = new List<HttpWebRequest>(CachedImgCount);

        /// <summary>
        /// 反馈图片列表预加载完成事件,用于判断是否有下一页
        /// return imgCount
        /// </summary>
        public event EventHandler PreListLoaded;

        /// <summary>
        /// do in a separate thread
        /// 下载缩略图线程
        /// </summary>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <param name="word"></param>
        public void PreFetchPage(int page, int count, string word, IMageSite site)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    preFetchedPage = site.GetPageString(page, count, word, MainWindow.WebProxy);
                    prePage = page;
                    preCount = count;
                    preWord = word;
                    preSite = site;
                    List<Img> imgs = site.GetImages(preFetchedPage, MainWindow.WebProxy);

                    //获得所有图片列表后反馈得到的数量
                    PreListLoaded(imgs.Count, null);
                    if (imgs.Count < 1)
                        return;

                    SessionClient sweb = new SessionClient(MainWindow.SecurityType);
                    SessionHeadersCollection shc = new SessionHeadersCollection
                    {
                        Accept = null,
                        ContentType = SessionHeadersValue.ContentTypeAuto,
                        Referer = site.Referer
                    };

                    shc = site.SiteHeaders;
                    shc.Add("Accept-Ranges", "bytes");

                    imgs = site.FilterImg(imgs, MainWindow.MainW.MaskInt, MainWindow.MainW.MaskRes,
                        MainWindow.MainW.LastViewed, MainWindow.MainW.MaskViewed, true, false);

                    //预加载缩略图
                    foreach (HttpWebRequest req1 in imgReqs)
                    {
                        if (req1 != null) req1.Abort();
                    }
                    preFetchedImg.Clear();
                    imgReqs.Clear();

                    //prefetch one by one
                    int cacheCount = CachedImgCount < imgs.Count ? CachedImgCount : imgs.Count;
                    for (int i = 0; i < cacheCount; i++)
                    {
                        WebResponse res = sweb.GetWebResponse(imgs[i].PreviewUrl, MainWindow.WebProxy, 10000, shc);
                        System.IO.Stream str = res.GetResponseStream();

                        if (!preFetchedImg.ContainsKey(imgs[i].PreviewUrl))
                        {
                            preFetchedImg.Add(imgs[i].PreviewUrl, MainWindow.MainW.CreateImageSrc(str));
                        }

                        res.Close();
                        res.Dispose();
                        str.Close();
                        str.Dispose();
                    }
                }
                catch
                {
                    //Console.WriteLine("useless");
                }
            })).Start();
        }

        /// <summary>
        /// 异步下载结束
        /// </summary>
        /// <param name="req"></param>
        //private void RespCallback(IAsyncResult re)
        //{
        //    System.Net.WebResponse res = null;
        //    try
        //    {
        //        //res = req.EndGetResponse(re);
        //        System.IO.StreamReader sr = new System.IO.StreamReader(res.GetResponseStream(), Encoding.UTF8);
        //        //PreFetchedPage = sr.ReadToEnd();
        //        //PreFetchUrl = (string)re.AsyncState;

        //        //预加载缩略图
        //        foreach (System.Net.HttpWebRequest req1 in imgReqs)
        //        {
        //            if (req1 != null) req1.Abort();
        //        }
        //        //foreach (string key in preFetchedImg.Keys)
        //        //{
        //        //    preFetchedImg[key].Close();
        //        //}
        //        preFetchedImg.Clear();
        //        imgReqs.Clear();
        //        ImgSrcProcessor processor = new ImgSrcProcessor(MainWindow.MainW.MaskInt, MainWindow.MainW.MaskRes, PreFetchUrl, MainWindow.MainW.SrcType, MainWindow.MainW.LastViewed, MainWindow.MainW.MaskViewed);
        //        processor.processComplete += new EventHandler(processor_processComplete);
        //        processor.ProcessSingleLink();
        //    }
        //    catch (Exception ex)
        //    {
        //        //System.Windows.MessageBox.Show(ex.ToString());
        //    }
        //    finally
        //    {
        //        if (res != null)
        //            res.Close();
        //    }
        //}

        //void processor_processComplete(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (sender != null)
        //        {
        //            List<Img> imgs = sender as List<Img>;

        //            //prefetch one by one
        //            int count = CachedImgCount < imgs.Count ? CachedImgCount : imgs.Count;
        //            //System.Windows.MessageBox.Show(count.ToString());
        //            for (int i = 0; i < count; i++)
        //            {
        //                System.Net.HttpWebRequest req = System.Net.WebRequest.Create(imgs[i].PreUrl) as System.Net.HttpWebRequest;
        //                imgReqs.Add(req);
        //                req.Proxy = proxy;

        //                req.UserAgent = SessionClient.DefUA;
        //                req.Referer = MainWindow.IsNeedReferer(imgs[i].PreUrl);

        //                System.Net.WebResponse res = req.GetResponse();
        //                System.IO.Stream str = res.GetResponseStream();

        //                preFetchedImg.Add(imgs[i].PreUrl, MainWindow.MainW.CreateImageSrc(str));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //System.Windows.MessageBox.Show(ex.ToString());
        //    }
        //}
    }
}
