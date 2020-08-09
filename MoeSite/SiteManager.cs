using LZ4;
using MoeLoaderDelta.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 管理站点定义
    /// Last 20200809
    /// </summary>
    public class SiteManager
    {
        /// <summary>
        /// 站点登录类型 用于判断调用登录方式 登录参数保存到LoginSiteArgs
        /// FillIn 弹出账号填写窗口
        /// Cookie 登录
        /// </summary>
        public enum SiteLoginType { FillIn, Cookie }

        /// <summary>
        /// 参数共享传递
        /// </summary>
        public static IWebProxy Mainproxy { get; set; }
        public static string RunPath { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string SitePacksPath { get; set; } = $"{RunPath}\\SitePacks\\";

        /// <summary>
        /// 初始化aesKey
        /// </summary>
        private static readonly string aesKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MachineInfo.GetBLOSSerialNumber()}${MachineInfo.GetCPUSerialNumber()}"));

        /// <summary>
        /// 站点集合
        /// </summary>
        public List<IMageSite> Sites { get; } = new List<IMageSite>();

        /// <summary>
        /// 站点定义管理者
        /// </summary>
        public static SiteManager Instance { get; } = new SiteManager();

        private SiteManager()
        {
            string[] dlls = { };
            try { dlls = Directory.GetFiles(SitePacksPath, "SitePack*.dll", SearchOption.AllDirectories); }
            catch { }

            #region 保证有基本站点包路径
            if (dlls.Length < 1)
            {
                string basisdll = SitePacksPath + "SitePack.dll";
                if (File.Exists(basisdll))
                {
                    List<string> dlll = new List<string> { basisdll };
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
                    Sites.AddRange(methodInfo.Invoke(Activator.CreateInstance(type), new object[] { Mainproxy }) as List<IMageSite>);
                }
                catch (Exception ex)
                {
                    EchoErrLog("站点载入过程", ex);
                }
            }
        }

        /// <summary>
        /// 调用站点登录
        /// </summary>
        /// <param name="site">站点</param>
        /// <param name="loginArgs">登录参数</param>
        public static void LoginSiteCall(IMageSite site, LoginSiteArgs loginArgs)
        {
            try { site.LoginCall(loginArgs); } catch { }
        }

        /// <summary>
        /// 提供站点错误的输出
        /// </summary>
        /// <param name="siteName">站点短名</param>
        /// <param name="ex">错误信息</param>
        /// <param name="extra_info">附加错误信息</param>
        /// <param name="noShow">不显示信息</param>
        /// <param name="noLog">不记录Log</param>
        public static void EchoErrLog(string siteName, Exception ex = null, string extra_info = null, bool noShow = false, bool noLog = false)
        {
            int maxlog = 4096;
            bool exisnull = ex == null;
            string logPath = SitePacksPath + "site_error.log",
                wstr = "[异常站点]: " + siteName + "\r\n";
            wstr += "[异常时间]: " + DateTime.Now.ToString() + "\r\n";
            wstr += "[异常信息]: " + extra_info + (exisnull ? "\r\n" : string.Empty);
            if (!exisnull)
            {
                wstr += (string.IsNullOrWhiteSpace(extra_info) ? string.Empty : " | ") + ex.Message + "\r\n";
                wstr += "[异常对象]: " + ex.Source + "\r\n";
                wstr += "[调用堆栈]: " + ex.StackTrace.Trim() + "\r\n";
                wstr += "[触发方法]: " + ex.TargetSite + "\r\n";
            }
            if (!noLog)
            {
                File.AppendAllText(logPath, wstr + "\r\n");
            }
            if (!noShow)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(extra_info) ? ex.Message : extra_info, $"{siteName} 错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        /// <param name="siteName">站点短名</param>
        /// <param name="extra_info">附加错误信息</param>
        public static void EchoErrLog(string siteName, string extra_info, bool noShow = false, bool noLog = false)
        {
            EchoErrLog(siteName, null, extra_info, noShow, noLog);
        }

        #region 站点配置文件处理方法
        /// <summary>
        /// 读INI配置文件 API
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">项</param>
        /// <param name="def">缺省值</param>
        /// <param name="retval">lpReturnedString取得的内容</param>
        /// <param name="size">lpReturnedString缓冲区的最大字符数</param>
        /// <param name="filePath">配置文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);

        /// <summary>
        /// 写INI配置文件 API
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">项</param>
        /// <param name="val">值</param>
        /// <param name="filepath">配置文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern long WritePrivateProfileString(string section, string key, string val, string filepath);

        /// <summary>
        /// 读INI配置文件
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">项</param>
        /// <param name="filePath">配置文件路径</param>
        /// <param name="def">缺省值</param>
        /// <returns></returns>
        public static string GetPrivateProfileString(string section, string key, string filePath, string def = null)
        {
            StringBuilder sb = new StringBuilder(short.MaxValue);
            try { GetPrivateProfileString(section, key, def ?? string.Empty, sb, sb.Capacity, filePath); }
            catch (Exception) { }
            return sb.ToString();
        }

        /// <summary>
        /// 读取或保存站点设置
        /// </summary>
        /// <param name="siteShortName">站点短名</param>
        /// <param name="section">项名</param>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        /// <param name="save">False读取,True保存</param>
        /// <returns></returns>
        public static string SiteConfig(string siteShortName, string section, string key, string value = null, bool save = false)
        {
            string siteini = $"{SitePacksPath}{siteShortName}.ini";
            string akey = string.Empty, iv = string.Empty;
            if (save)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    akey = aesKey;
                    iv = RandomRNG(100000, 999999).ToString();
                    value = $"{iv}{AESHelper.AesEncrypt(value, akey, iv)}";
                    value = Convert.ToBase64String(LZ4Codec.Wrap(Encoding.UTF8.GetBytes(value)));
                }
                return WritePrivateProfileString(section, key, value, siteini).ToString();
            }
            else
            {
                if (!File.Exists(siteini)) { return string.Empty; }
                string getVal = GetPrivateProfileString(section, key, siteini);
                if (string.IsNullOrWhiteSpace(getVal) || getVal.Length < 7) { return string.Empty; }
                getVal = Encoding.UTF8.GetString(LZ4Codec.Unwrap(Convert.FromBase64String(getVal)));
                akey = aesKey;
                iv = getVal.Substring(0, 6);
                return AESHelper.AesDecrypt(getVal.Substring(6), akey, iv);
            }
        }
        #endregion

        /// <summary>
        /// 取不重复随机整数
        /// </summary>
        /// <param name="minValue">最小整数</param>
        /// <param name="maxValue">最大整数</param>
        public static int RandomRNG(int minValue, int maxValue)
        {
            if (minValue > maxValue) { maxValue = minValue + 1; }
            Random rand = new Random(new Func<int>(() =>
            {
                byte[] bytes = new byte[4];
                new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(bytes);
                return BitConverter.ToInt32(bytes, 0);
            })());
            return rand.Next(minValue, maxValue);
        }
    }

    /// <summary>
    /// 站点扩展设置类
    /// </summary>
    public class SiteExtendedSetting
    {
        /// <summary>
        /// 在菜单中显示的标题
        /// </summary>
        public string Title { get; set; } = "扩展";
        /// <summary>
        /// 是否是启用的图标
        /// </summary>
        public bool Enable { get; set; } = false;
        /// <summary>
        /// 点击菜单时执行的委托方法
        /// </summary>
        public Delegate SettingAction { get; set; }
    }

    /// <summary>
    /// 站点登录参数
    /// </summary>
    public class LoginSiteArgs
    {
        /// <summary>
        /// 登录账号
        /// </summary>
        public string User { get; set; } = string.Empty;
        /// <summary>
        /// 登录密码
        /// </summary>
        public string Pwd { get; set; } = string.Empty;
        /// <summary>
        /// 登录Cookie
        /// </summary>
        public string Cookie { get; set; } = string.Empty;
    }

}
