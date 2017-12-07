using System.Collections.Generic;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 抽象圖片站點
    /// </summary>
    public abstract class AbstractImageSite : ImageSite
    {
        /// <summary>
        /// 預設的縮圖寬高
        /// </summary>
        private const int PICWIDTH = 150;

        /// <summary>
        /// 站點URL，用於打開該站點首頁。eg. http://yande.re
        /// </summary>
        public abstract string SiteUrl { get; }

        /// <summary>
        /// 站點名稱，用於站點列表中的顯示。eg. yande.re
        /// 提示：當站點名稱含有空格時，第一個空格前的字串相同的站點將在站點列表中自動合併為一項，
        /// 例如"www.pixiv.net [User]」和"www.pixiv.net [Day]」將合併為"www.pixiv.net"
        /// </summary>
        public abstract string SiteName { get; }

        /// <summary>
        /// 站點的短名稱，將作為站點的唯一標識，eg. yandere
        /// 提示：可以在程式集中加入以短名稱作為檔案名的ico圖示（eg. yandere.ico），該圖示會自動作為該站點的圖示顯示在站點列表中。
        /// 注意：需要選擇該圖示檔案的 Build Action 為 Embedded Resource
        /// </summary>
        public abstract string ShortName { get; }

        /// <summary>
        /// 站點的搜尋方式短名稱，用於顯示在下拉表標題上
        /// </summary>
        public abstract string ShortType { get; }

        /// <summary>
        /// 向該站點發起請求時需要偽造的Referer，若不需要則保持null
        /// </summary>
        public virtual string Referer { get { return null; } }

        /// <summary>
        /// 子站映射關鍵名，用於下載時判斷不同於主站短域名的子站，以此返回主站的Referer,用半形逗號分隔
        /// </summary>
        public virtual string SubReferer { get { return null; } }

        /// <summary>
        /// 是否支援設定單頁數量，若為false則單頁數量不可修改
        /// </summary>
        public virtual bool IsSupportCount { get { return true; } }

        /// <summary>
        /// 是否支援評分，若為false則不可按分數過濾圖片
        /// </summary>
        public virtual bool IsSupportScore { get { return true; } }

        /// <summary>
        /// 是否支援解析度，若為false則不可按解析度過濾圖片
        /// </summary>
        public virtual bool IsSupportRes { get { return true; } }

        /// <summary>
        /// 是否支援預覽圖，若為false則縮圖上無查看預覽圖的按鈕
        /// </summary>
        public virtual bool IsSupportPreview { get { return true; } }

        /// <summary>
        /// 是否支援搜尋框自動提示，若為false則輸入關鍵字時無自動提示
        /// </summary>
        public virtual bool IsSupportTag { get { return true; } }

        /// <summary>
        /// 滑鼠懸停在站點列表項上時顯示的工具提示訊息
        /// </summary>
        public virtual string ToolTip { get { return null; } }

        /// <summary>
        /// 該站點在站點列表中是否可見
        /// 提示：若該站點預設不希望被看到可以設為false，當滿足一定條件時（例如存在某個檔案）再顯示
        /// </summary>
        public virtual bool IsVisible { get { return true; } }

        /// <summary>
        /// 大縮圖尺寸
        /// </summary>
        public virtual System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(PICWIDTH, PICWIDTH); } }
        /// <summary>
        /// 小縮圖尺寸
        /// 若大小縮圖尺寸不同，則可以透過右鍵選單中的「使用小縮略」切換顯示大小
        /// </summary>
        public virtual System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(PICWIDTH, PICWIDTH); } }

        /// <summary>
        /// 獲取頁面的原始碼，例如HTML
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="count">單頁數量（可能不支援）</param>
        /// <param name="keyWord">關鍵字</param>
        /// <param name="proxy">全局的代理設定，進行網路操作時請使用該代理</param>
        /// <returns>頁面原始碼</returns>
        public abstract string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy);

        /// <summary>
        /// 從頁面原始碼獲取圖片列表
        /// </summary>
        /// <param name="pageString">頁面原始碼</param>
        /// <param name="proxy">全局的代理設定，進行網路操作時請使用該代理</param>
        /// <returns>圖片訊息列表</returns>
        public abstract List<Img> GetImages(string pageString, System.Net.IWebProxy proxy);

        /// <summary>
        /// 獲取關鍵字自動提示列表
        /// </summary>
        /// <param name="word">關鍵字</param>
        /// <param name="proxy">全局的代理設定，進行網路操作時請使用該代理</param>
        /// <returns>提示列表項集合</returns>
        public virtual List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            return new List<TagItem>();
        }

        #region 實現者無需關注此處程式碼
        /// <summary>
        /// 站點列表中顯示的圖示
        /// </summary>
        public virtual System.IO.Stream IconStream
        {
            get
            {
                return GetType().Assembly.GetManifestResourceStream("SitePack.image." + ShortName + ".ico");
            }
        }

        /// <summary>
        /// 獲取圖片列表
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="count">單頁數量（可能不支援）</param>
        /// <param name="keyWord">關鍵字</param>
        /// <param name="proxy">全局的代理設定，進行網路操作時請使用該代理</param>
        /// <returns>圖片訊息列表</returns>
        public virtual List<Img> GetImages(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            return GetImages(GetPageString(page, count, keyWord, proxy), proxy);
        }

        /// <summary>
        /// 圖片過濾
        /// </summary>
        /// <param name="imgs">圖片集合</param>
        /// <param name="maskScore">封鎖分數</param>
        /// <param name="maskRes">封鎖解析度</param>
        /// <param name="lastViewed">已瀏覽的圖片id</param>
        /// <param name="maskViewed">封鎖已瀏覽</param>
        /// <param name="showExplicit">封鎖Explicit評級</param>
        /// <param name="updateViewed">更新已瀏覽列表</param>
        /// <returns></returns>
        public virtual List<Img> FilterImg(List<Img> imgs, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, bool showExplicit, bool updateViewed)
        {
            List<Img> re = new List<Img>();
            foreach (Img img in imgs)
            {
                //標記已閱
                img.IsViewed = true;
                if (lastViewed != null && !lastViewed.IsViewed(img.Id))
                {
                    img.IsViewed = false;
                    if (updateViewed)
                        lastViewed.AddViewingId(img.Id);
                }
                else if (maskViewed) continue;

                int res = img.Width * img.Height;
                //score filter & resolution filter & explicit filter
                if (IsSupportScore && img.Score <= maskScore || IsSupportRes && res < maskRes || !showExplicit && img.IsExplicit)
                {
                    continue;
                }
                else
                {
                    re.Add(img);
                }
            }
            return re;
        }
        #endregion
    }
}
