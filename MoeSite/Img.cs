using System.Web;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 圖片詳細訊息獲取代理
    /// </summary>
    /// <param name="img">目標圖片對象</param>
    /// <param name="proxy">全局的代理設定，進行網路操作時請使用該代理</param>
    public delegate void DetailHandler(Img img, System.Net.IWebProxy proxy);


    /// <summary>
    /// 表示一張圖片及其相關訊息
    /// </summary>
    public class Img
    {
        /// <summary>
        /// 原圖地址
        /// </summary>
        public string OriginalUrl { get; set; }

        /// <summary>
        /// 預覽圖地址，尺寸位於原圖與縮圖之間
        /// </summary>
        public string PreviewUrl { get; set; }

        /// <summary>
        /// 原圖地址集合
        /// 若原圖地址不止一個（例如pixiv的漫畫），則將多個地址置於此處，該列表不為空時 OriginalUrl 將被忽略
        /// </summary>
        public List<string> OrignalUrlList { get; set; }

        /// <summary>
        /// 原圖寬度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 原圖高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 縮圖地址
        /// </summary>
        public string SampleUrl { get; set; }

        /// <summary>
        /// 圖片創建日期
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 圖片Tags
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// 原圖檔案尺寸
        /// </summary>
        public string FileSize { get; set; }

        /// <summary>
        /// 圖片id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 圖片得分
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 圖片來源
        /// </summary>
        private string source;
        public string Source
        {
            get { return StringLineBreak(HttpUtility.HtmlDecode(source), 64); }
            set { source = value; }
        }

        /// <summary>
        /// 圖片描述
        /// </summary>
        private string desc;
        public string Desc
        {
            get { return StringLineBreak(HttpUtility.HtmlDecode(desc), 64); }
            set { desc = value; }
        }

        /// <summary>
        /// JPEG格式原圖地址（非所有站點支援，不支援時請與 OriginalUrl 保持一致）
        /// </summary>
        public string JpegUrl { get; set; }

        private string dimension = null;
        /// <summary>
        /// 原圖解析度
        /// </summary>
        public string Dimension
        {
            get
            {
                if (dimension == null)
                    return Width + " x " + Height;
                else return dimension;
            }
            set { dimension = value; }
        }

        /// <summary>
        /// 是否已瀏覽過
        /// </summary>
        public bool IsViewed { get; set; }

        /// <summary>
        /// 是否Explicit內容
        /// </summary>
        public bool IsExplicit { get; set; }

        /// <summary>
        /// 圖片詳情頁地址
        /// </summary>
        public string DetailUrl { get; set; }

        /// <summary>
        /// 作品作者
        /// </summary>
        private string author;
        public string Author
        {
            get { return HttpUtility.HtmlDecode(author); }
            set { author = value; }
        }

        /// <summary>
        /// 圖冊頁數
        /// </summary>
        public string ImgP { get; set; }

        /// <summary>
        /// 不對下載的圖示進行完整性驗證（對於無法獲取原檔案大小的站點）
        /// </summary>
        public bool NoVerify { get; set; }

        /// <summary>
        /// 若圖片的某些訊息需要單獨獲取（例如原圖URL可能位於第二層頁面），則實現該介面，將網路操作、提取訊息操作置於此處
        /// </summary>
        public DetailHandler DownloadDetail;

        /// <summary>
        /// 預設建構式
        /// </summary>
        public Img()
        {
            Date = "NoDate";
            Desc = "";
            FileSize = "N/A";
            Height = 0;
            Id = 0;
            IsExplicit = false;
            IsViewed = false;
            JpegUrl = "";
            OriginalUrl = "";
            PreviewUrl = "";
            SampleUrl = "";
            Score = 0;
            Source = "";
            Tags = "NoTags";
            Width = 0;
            OrignalUrlList = new List<string>();
            DetailUrl = "";
            Author = "UnkwnAuthor";
            ImgP = "";
            NoVerify = false;
        }

        /// <summary>
        /// 字串按字數加換行
        /// </summary>
        /// <param name="Str">原字串</param>
        /// <param name="LineWordCount">每行字數</param>
        /// <returns></returns>
        public static string StringLineBreak(string Str, int LineWordCount)
        {
            string[] strs = Regex.Split(Str, @"(?<=\G.{" + LineWordCount + "})(?!$)");
            int strsl = strs.Length;
            string retsrt = "";
            for (int i = 0; i < strsl; i++)
            {
                retsrt += strs[i] + (i == strsl - 1 ? "" : "\r\n");
            }
            return retsrt;
        }
    }
}
