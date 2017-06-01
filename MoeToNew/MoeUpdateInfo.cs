using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace MoeLoader
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
                return (new JavaScriptSerializer()).Deserialize<MoeUpdateItem>(upjson);
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
            get { return string.IsNullOrWhiteSpace(info) ? "" : Regex.Unescape(info); }
            set { info = value; }
        }
        public List<MoeUpdateFile> files { get; set; }
    }

    /// <summary>
    /// 更新文件对象
    /// </summary>
    public class MoeUpdateFile
    {
        private string path, name, ver, state, url, md5;
        public string Path
        {
            get { return string.IsNullOrWhiteSpace(path) ? "" : Regex.Unescape(path); }
            set { path = value; }
        }
        public string Name
        {
            get { return string.IsNullOrWhiteSpace(name) ? "" : Regex.Unescape(name); }
            set { name = value; }
        }
        public string Ver
        {
            get { return string.IsNullOrWhiteSpace(ver) ? "" : ver; }
            set { ver = value; }
        }
        /// <summary>
        /// 状态 up:下载  del:删除
        /// </summary>
        public string State
        {
            get { return string.IsNullOrWhiteSpace(state) ? "" : state.ToLower(); }
            set { state = value; }
        }
        public string Url
        {
            get { return string.IsNullOrWhiteSpace(url) ? "" : Regex.Unescape(url); }
            set { url = value; }
        }
        public string MD5
        {
            get { return string.IsNullOrWhiteSpace(md5) ? "" : md5.Trim(); }
            set { md5 = value; }
        }
    }
}
