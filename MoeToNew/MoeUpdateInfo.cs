using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 更新信息
    /// </summary>
    public class MoeUpdateInfo
    {
        /// <summary>
        /// 取得更新信息对象
        /// </summary>
        /// <param name="upjson">更新信息JSON内容</param>
        /// <returns></returns>
        public MoeUpdateItem GetMoeUpdateInfo(string upjson)
        {
            try
            {
                return new JavaScriptSerializer().Deserialize<MoeUpdateItem>(upjson);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 更新信息对象
    /// </summary>
    public class MoeUpdateItem
    {
        /// <summary>
        /// 更新的内容说明
        /// </summary>
        private string info;
        public string Info
        {
            get { return string.IsNullOrWhiteSpace(info) ? string.Empty : Regex.Unescape(info); }
            set { info = value; }
        }
        public List<MoeUpdateFile> Files { get; set; }
    }

    /// <summary>
    /// 更新文件对象
    /// </summary>
    public class MoeUpdateFile
    {
        private string path, name, ver, state, url, md5, newpath;
        public string Path
        {
            get => string.IsNullOrWhiteSpace(path) ? string.Empty : Regex.Unescape(path);
            set => path = value;
        }
        public string Name
        {
            get => string.IsNullOrWhiteSpace(name) ? string.Empty : Regex.Unescape(name);
            set => name = value;
        }
        public string Ver
        {
            get => string.IsNullOrWhiteSpace(ver) ? string.Empty : ver;
            set => ver = value;
        }
        /// <summary>
        /// 状态 up:下载  del:删除 mov:移动
        /// </summary>
        public string State
        {
            get => string.IsNullOrWhiteSpace(state) ? string.Empty : state.ToLower();
            set => state = value;
        }
        public string Url
        {
            get => string.IsNullOrWhiteSpace(url) ? string.Empty : Regex.Unescape(url);
            set => url = value;
        }
        public string MD5
        {
            get => string.IsNullOrWhiteSpace(md5) ? string.Empty : md5.Trim();
            set => md5 = value;
        }
        /// <summary>
        /// 移动到新路径
        /// </summary>
        public string NewPath
        {
            get => string.IsNullOrWhiteSpace(newpath) ? string.Empty : Regex.Unescape(newpath);
            set => newpath = value;
        }
    }
}
