using System.Collections.Generic;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 抽象图片站点
    /// </summary>
    public abstract class AbstractImageSite : ImageSite
    {
        /// <summary>
        /// 默认的缩略图宽高
        /// </summary>
        private const int PICWIDTH = 150;

        /// <summary>
        /// 站点URL，用于打开该站点主页。eg. http://yande.re
        /// </summary>
        public abstract string SiteUrl { get; }

        /// <summary>
        /// 站点名称，用于站点列表中的显示。eg. yande.re
        /// 提示：当站点名称含有空格时，第一个空格前的字符串相同的站点将在站点列表中自动合并为一项，
        /// 例如“www.pixiv.net [User]”和“www.pixiv.net [Day]”将合并为“www.pixiv.net”
        /// </summary>
        public abstract string SiteName { get; }

        /// <summary>
        /// 站点的短名称，将作为站点的唯一标识，eg. yandere
        /// 提示：可以在程序集中加入以短名称作为文件名的ico图标（eg. yandere.ico），该图标会自动作为该站点的图标显示在站点列表中。
        /// 注意：需要选择该图标文件的 Build Action 为 Embedded Resource
        /// </summary>
        public abstract string ShortName { get; }

        /// <summary>
        /// 站点的搜索方式短名称，用于显示在下拉表标题上
        /// </summary>
        public abstract string ShortType { get; }

        /// <summary>
        /// 向该站点发起请求时需要伪造的Referer，若不需要则保持null
        /// </summary>
        public virtual string Referer { get { return null; } }

        /// <summary>
        /// 子站映射关键名，用于下载时判断不同于主站短域名的子站，以此返回主站的Referer,用半角逗号分隔
        /// </summary>
        public virtual string SubReferer { get { return null; } }

        /// <summary>
        /// 是否支持设置单页数量，若为false则单页数量不可修改
        /// </summary>
        public virtual bool IsSupportCount { get { return true; } }

        /// <summary>
        /// 是否支持评分，若为false则不可按分数过滤图片
        /// </summary>
        public virtual bool IsSupportScore { get { return true; } }

        /// <summary>
        /// 是否支持分辨率，若为false则不可按分辨率过滤图片
        /// </summary>
        public virtual bool IsSupportRes { get { return true; } }

        /// <summary>
        /// 是否支持预览图，若为false则缩略图上无查看预览图的按钮
        /// </summary>
        public virtual bool IsSupportPreview { get { return true; } }

        /// <summary>
        /// 是否支持搜索框自动提示，若为false则输入关键词时无自动提示
        /// </summary>
        public virtual bool IsSupportTag { get { return true; } }

        /// <summary>
        /// 鼠标悬停在站点列表项上时显示的工具提示信息
        /// </summary>
        public virtual string ToolTip { get { return null; } }

        /// <summary>
        /// 该站点在站点列表中是否可见
        /// 提示：若该站点默认不希望被看到可以设为false，当满足一定条件时（例如存在某个文件）再显示
        /// </summary>
        public virtual bool IsVisible { get { return true; } }

        /// <summary>
        /// 大缩略图尺寸
        /// </summary>
        public virtual System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(PICWIDTH, PICWIDTH); } }
        /// <summary>
        /// 小缩略图尺寸
        /// 若大小缩略图尺寸不同，则可以通过右键菜单中的“使用小缩略”切换显示大小
        /// </summary>
        public virtual System.Drawing.Point SmallImgSize { get { return new System.Drawing.Point(PICWIDTH, PICWIDTH); } }

        /// <summary>
        /// 获取页面的源代码，例如HTML
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="count">单页数量（可能不支持）</param>
        /// <param name="keyWord">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>页面源代码</returns>
        public abstract string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy);

        /// <summary>
        /// 从页面源代码获取图片列表
        /// </summary>
        /// <param name="pageString">页面源代码</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>图片信息列表</returns>
        public abstract List<Img> GetImages(string pageString, System.Net.IWebProxy proxy);

        /// <summary>
        /// 获取关键词自动提示列表
        /// </summary>
        /// <param name="word">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>提示列表项集合</returns>
        public virtual List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        {
            return new List<TagItem>();
        }

        #region 实现者无需关注此处代码
        /// <summary>
        /// 站点列表中显示的图标
        /// </summary>
        public virtual System.IO.Stream IconStream
        {
            get
            {
                return GetType().Assembly.GetManifestResourceStream("SitePack.image." + ShortName + ".ico");
            }
        }

        /// <summary>
        /// 获取图片列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="count">单页数量（可能不支持）</param>
        /// <param name="keyWord">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>图片信息列表</returns>
        public virtual List<Img> GetImages(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            return GetImages(GetPageString(page, count, keyWord, proxy), proxy);
        }

        /// <summary>
        /// 图片过滤
        /// </summary>
        /// <param name="imgs">图片集合</param>
        /// <param name="maskScore">屏蔽分数</param>
        /// <param name="maskRes">屏蔽分辨率</param>
        /// <param name="lastViewed">已浏览的图片id</param>
        /// <param name="maskViewed">屏蔽已浏览</param>
        /// <param name="showExplicit">屏蔽Explicit评级</param>
        /// <param name="updateViewed">更新已浏览列表</param>
        /// <returns></returns>
        public virtual List<Img> FilterImg(List<Img> imgs, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, bool showExplicit, bool updateViewed)
        {
            List<Img> re = new List<Img>();
            foreach (Img img in imgs)
            {
                //标记已阅
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
