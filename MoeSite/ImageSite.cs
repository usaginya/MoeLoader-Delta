using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 自动提示列表中的一项
    /// </summary>
    public struct TagItem
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;
        /// <summary>
        /// 数量
        /// </summary>
        public string Count;
    }

    /// <summary>
    /// 图片站点的接口
    /// </summary>
    public interface ImageSite
    {
        /// <summary>
        /// 站点URL，用于打开该站点主页。eg. http://yande.re
        /// </summary>
        string SiteUrl { get; }

        /// <summary>
        /// 站点名称，用于站点列表中的显示。eg. yande.re
        /// 提示：当站点名称含有空格时，第一个空格前的字符串相同的站点将在站点列表中自动合并为一项，
        /// 例如“www.pixiv.net [User]”和“www.pixiv.net [Day]”将合并为“www.pixiv.net”
        /// </summary>
        string SiteName { get; }

        /// <summary>
        /// 站点的短名称，将作为站点的唯一标识，eg. yandere
        /// </summary>
        string ShortName { get; }

        /// <summary>
        /// 站点的搜索方式短名称，用于显示在下拉表标题上
        /// </summary>
        string ShortType { get; }

        /// <summary>
        /// 向该站点发起请求时需要伪造的Referer，若不需要则保持null
        /// </summary>
        string Referer { get; }

        /// <summary>
        /// 子站映射关键名，用于下载时判断不同于主站短域名的子站，以此返回主站的Referer,用半角逗号分隔
        /// </summary>
        string SubReferer { get; }

        /// <summary>
        /// 鼠标悬停在站点列表项上时显示的工具提示信息
        /// </summary>
        string ToolTip { get; }

        /// <summary>
        /// 是否支持设置单页数量，若为false则单页数量不可修改
        /// </summary>
        bool IsSupportCount { get; }

        /// <summary>
        /// 是否支持评分，若为false则不可按分数过滤图片
        /// </summary>
        bool IsSupportScore { get; }

        /// <summary>
        /// 是否支持分辨率，若为false则不可按分辨率过滤图片
        /// </summary>
        bool IsSupportRes { get; }

        /// <summary>
        /// 是否支持预览图，若为false则缩略图上无查看预览图的按钮
        /// </summary>
        bool IsSupportPreview { get; }

        /*
        /// <summary>
        /// 是否支持jpeg格式图片【Not yet used】
        /// </summary>
        //bool IsSupportJpeg { get; }
         */

        /// <summary>
        /// 是否支持搜索框自动提示，若为false则输入关键词时无自动提示
        /// </summary>
        bool IsSupportTag { get; }

        /// <summary>
        /// 大缩略图尺寸
        /// </summary>
        System.Drawing.Point LargeImgSize { get; }

        /// <summary>
        /// 小缩略图尺寸
        /// 若大小缩略图尺寸不同，则可以通过右键菜单中的“使用小缩略”切换显示大小
        /// </summary>
        System.Drawing.Point SmallImgSize { get; }

        /// <summary>
        /// 该站点在站点列表中是否可见
        /// 提示：若该站点默认不希望被看到可以设为false，当满足一定条件时（例如存在某个文件）再显示
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// 站点列表中显示的图标
        /// </summary>
        System.IO.Stream IconStream { get; }

        /// <summary>
        /// 获取图片列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="count">单页数量（可能不支持）</param>
        /// <param name="keyWord">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>图片信息列表</returns>
        List<Img> GetImages(int page, int count, string keyWord, System.Net.IWebProxy proxy);

        /// <summary>
        /// 获取页面的源代码，例如HTML
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="count">单页数量（可能不支持）</param>
        /// <param name="keyWord">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>页面源代码</returns>
        string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy);

        /// <summary>
        /// 从页面源代码获取图片列表
        /// </summary>
        /// <param name="pageString">页面源代码</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>图片信息列表</returns>
        List<Img> GetImages(string pageString, System.Net.IWebProxy proxy);

        /// <summary>
        /// 获取关键词自动提示列表
        /// </summary>
        /// <param name="word">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>提示列表项集合</returns>
        List<TagItem> GetTags(string word, System.Net.IWebProxy proxy);

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
        List<Img> FilterImg(List<Img> imgs, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, bool showExplicit, bool updateViewed);
    }
}
