using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 管理站点定义
    /// Last 20190918
    /// </summary>
    public class SiteManager
    {
        /// <summary>
        /// 站点登录类型 用于LoginURL
        /// FillIn 弹出账号填写窗口、填写内容被填充到站点属性LoginUser 和 LoginPwd
        /// </summary>
        public enum SiteLoginType { FillIn }

        private static List<ImageSite> sites = new List<ImageSite>();
        private static SiteManager instance;

        /// <summary>
        /// 参数共享传递
        /// </summary>
        public static IWebProxy Mainproxy { get; set; }
        public static string RunPath { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string SitePacksPath { get; set; } = $"{RunPath}\\SitePacks\\";

        private SiteManager()
        {

            string[] dlls = Directory.GetFiles(SitePacksPath, "SitePack*.dll", SearchOption.AllDirectories);

            #region 保证有基本站点包路径
            if (dlls.Length < 1)
            {
                List<string> dlll = new List<string>();
                string basisdll = SitePacksPath + "SitePack.dll";

                if (File.Exists(basisdll))
                {
                    dlll.Add(basisdll);
                    dlls = dlll.ToArray();
                }
            }
            #endregion

            foreach (string dll in dlls)
            {
                try
                {
                    byte[] assemblyBuffer = File.ReadAllBytes(dll);
                    Type type = Assembly.Load(assemblyBuffer).GetType("SitePack.SiteProvider", true, false);
                    MethodInfo methodInfo = type.GetMethod("SiteList");
                    sites.AddRange(methodInfo.Invoke(Activator.CreateInstance(type), new object[] { Mainproxy }) as List<ImageSite>);
                }
                catch (Exception ex)
                {
                    EchoErrLog("站点载入过程", ex);
                }
            }
        }

        /// <summary>
        /// 站点定义管理者
        /// </summary>
        public static SiteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SiteManager();
                }
                return instance;
            }
        }

        /// <summary>
        /// 站点集合
        /// </summary>
        public List<ImageSite> Sites => sites;

        /// <summary>
        /// 站点登录处理 通过IE
        /// </summary>
        /// <param name="imageSite">站点</param>
        /// <param name="cookie">站点内部cookie, 将返回登录后的cookie, 登录失败为string.Empty</param>
        /// <param name="LoggedFlags">登录成功页面验证字符, 多个字符用|分隔; 无需验证请置null</param>
        /// <param name="Sweb">站点内部SessionClient</param>
        /// <param name="shc">站点内部SessionHeadersCollection</param>
        /// <param name="PageString">返回验证登录时的页面HTML</param>
        /// <returns></returns>
        public static bool LoginSite(ImageSite imageSite, ref string cookie, string LoggedFlags, ref SessionClient Sweb, ref SessionHeadersCollection shc, ref string pageString)
        {
            string tmp_cookie = CookiesHelper.GetIECookies(imageSite.SiteUrl);
            bool result = !string.IsNullOrWhiteSpace(tmp_cookie) && tmp_cookie.Length > 3;

            if (result)
            {
                shc.Timeout = shc.Timeout * 2;
                shc.Set("Cookie", tmp_cookie);
                try
                {
                    pageString = Sweb.Get(imageSite.SiteUrl, Mainproxy, shc);
                    result = !string.IsNullOrWhiteSpace(pageString);

                    if (result && LoggedFlags != null)
                    {
                        string[] LFlagsArray = LoggedFlags.Split('|');
                        foreach (string Flag in LFlagsArray)
                        {
                            result &= pageString.Contains(Flag);
                        }
                    }

                    cookie = result ? tmp_cookie : cookie;
                }
                catch
                {
                    //有cookie访问时发生超时之类的错误 还是作为用户登录状态 返回true
                    cookie = string.Empty;
                }
            }

            return result;
        }

        /// <summary>
        /// 站点登录处理 通过IE
        /// </summary>
        /// <param name="imageSite">站点</param>
        /// <param name="cookie">站点内部cookie, 将返回登录后的cookie, 登录失败为string.Empty</param>
        /// <param name="LoggedFlags">登录成功页面验证字符, 多个字符用|分隔; 无需验证请置null</param>
        /// <param name="Sweb">站点内部SessionClient</param>
        /// <param name="shc">站点内部SessionHeadersCollection</param>
        /// <returns></returns>
        public static bool LoginSite(ImageSite imageSite, ref string cookie, string LoggedFlags, ref SessionClient Sweb, ref SessionHeadersCollection shc)
        {
            string NullPageString = string.Empty;
            return LoginSite(imageSite, ref cookie, LoggedFlags, ref Sweb, ref shc, ref NullPageString);
        }

        /// <summary>
        /// 提供站点错误的输出
        /// </summary>
        /// <param name="SiteShortName">站点短名</param>
        /// <param name="ex">错误信息</param>
        /// <param name="extra_info">附加错误信息</param>
        /// <param name="NoShow">不显示信息</param>
        /// <param name="NoLog">不记录Log</param>
        public static void EchoErrLog(string SiteShortName, Exception ex = null, string extra_info = null, bool NoShow = false, bool NoLog = false)
        {
            int maxlog = 4096;
            bool exisnull = ex == null;
            string logPath = SitePacksPath + "site_error.log",
                wstr = "[异常站点]: " + SiteShortName + "\r\n";
            wstr += "[异常时间]: " + DateTime.Now.ToString() + "\r\n";
            wstr += "[异常信息]: " + extra_info + (exisnull ? "\r\n" : string.Empty);
            if (!exisnull)
            {
                wstr += (string.IsNullOrWhiteSpace(extra_info) ? string.Empty : " | ") + ex.Message + "\r\n";
                wstr += "[异常对象]: " + ex.Source + "\r\n";
                wstr += "[调用堆栈]: " + ex.StackTrace.Trim() + "\r\n";
                wstr += "[触发方法]: " + ex.TargetSite + "\r\n";
            }
            if (!NoLog)
            {
                File.AppendAllText(logPath, wstr + "\r\n");
            }
            if (!NoShow)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(extra_info) ? ex.Message : extra_info, $"{SiteShortName} 错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //压缩记录
            long sourceLength = new FileInfo(logPath).Length;
            if (sourceLength > maxlog)
            {
                byte[] buffer = new byte[maxlog];
                using (FileStream fs = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    int newleng = (int)sourceLength - maxlog;
                    newleng = newleng > maxlog ? maxlog : newleng;
                    fs.Seek(newleng, SeekOrigin.Begin);
                    fs.Read(buffer, 0, maxlog);
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.SetLength(0);
                    fs.Write(buffer, 0, maxlog);
                }
            }

        }
        /// <summary>
        /// 提供站点错误的输出
        /// </summary>
        /// <param name="SiteShortName">站点短名</param>
        /// <param name="extra_info">附加错误信息</param>
        public static void EchoErrLog(string SiteShortName, string extra_info, bool NoShow = false, bool NoLog = false)
        {
            EchoErrLog(SiteShortName, null, extra_info, NoShow, NoLog);
        }
    }
}
