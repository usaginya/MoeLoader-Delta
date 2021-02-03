using IniParser;
using IniParser.Model;
using IniParser.Parser;
using LZ4;
using MoeLoaderDelta.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 管理站点定义
    /// Last 20210120
    /// </summary>
    public class SiteManager
    {
        /// <summary>
        /// 站点登录类型 用于判断调用登录方式 登录参数保存到LoginSiteArgs
        /// FillIn      弹出账号填写窗口
        /// Cookie 登录
        /// </summary>
        public enum SiteLoginType { FillIn, Cookie }

        /// <summary>
        /// 站点保存方式类型
        /// Change 修改配置
        /// Save      保存配置
        /// </summary>
        public enum SiteConfigType { Read, Change, Save }

        #region 事件代理
        /// <summary>
        /// 显示Toast消息
        /// </summary>
        public static ShowToastMsgDelegate showToastMsgDelegate;
        public delegate void ShowToastMsgDelegate(string msg, MsgType msgType);
        #endregion

        /// <summary>
        /// Toast消息类型
        /// </summary>
        public enum MsgType { Info, Success, Warning, Error }

        /// <summary>
        /// 参数共享传递
        /// </summary>
        public static IWebProxy MainProxy { get; set; }
        public static string RunPath { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string SitePacksPath { get; set; } = $"{RunPath}\\SitePacks\\";

        /// <summary>
        /// 初始化aesKey
        /// </summary>
        private static readonly string aesKey = Convert.ToBase64String
            (Encoding.UTF8.GetBytes($"{MachineInfo.GetBLOSSerialNumber()}${MachineInfo.GetCPUSerialNumber()}"));

        /// <summary>
        /// ini配置文件解析
        /// </summary>
        private static FileIniDataParser iniParser;
        /// <summary>
        /// ini配置数据库 SiteShortName, IniData
        /// </summary>
        private static Dictionary<string, IniData> inis = new Dictionary<string, IniData>();

        /// <summary>
        /// 站点集合
        /// </summary>
        public List<IMageSite> Sites { get; } = new List<IMageSite>();

        /// <summary>
        /// 站点定义管理者
        /// </summary>
        public static SiteManager Instance { get; } = new SiteManager();

        private SiteManager() { }

        /// <summary>
        /// 初始化加载站点
        /// </summary>
        public void Initialize()
        {
            //设置加载站点配置解析
            IniDataParser iniDataParser = new IniDataParser();
            iniDataParser.Configuration.AllowCreateSectionsOnFly
                = iniDataParser.Configuration.AllowDuplicateKeys
                = iniDataParser.Configuration.AllowDuplicateSections
                = iniDataParser.Configuration.SkipInvalidLines
                = true;
            iniParser = new FileIniDataParser(iniDataParser);

            string[] dlls = { };
            try { dlls = Directory.GetFiles(SitePacksPath, "SitePack*.dll", SearchOption.AllDirectories); } catch { }
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
                    Sites.AddRange(methodInfo.Invoke(Activator.CreateInstance(type), new object[] { MainProxy }) as List<IMageSite>);
                }
                catch (Exception ex)
                {
                    EchoErrLog("站点载入过程", ex);
                }
            }
        }

        /// <summary>
        /// 调用主窗口显示Toast消息
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="msgType">类型</param>
        public static void ShowToastMsg(string msg, MsgType msgType = MsgType.Info)
        {
            if (showToastMsgDelegate == null) { return; }
            showToastMsgDelegate(msg, msgType);
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
            int maxlog = 8192;
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
        /// 载入站点配置
        /// </summary>
        /// <param name="siteShortName">站点短名</param>
        public static void LoadSiteConfig(string siteShortName)
        {
            if (string.IsNullOrWhiteSpace(siteShortName)) { return; }

            string content = string.Empty, iv = content, siteini = $"{SitePacksPath}{siteShortName}.ini";
            if (string.IsNullOrWhiteSpace(siteShortName) || inis.Any(d => d.Key == siteShortName)) { return; }

            if (File.Exists(siteini))
            {
                //读入并解压
                content = File.ReadAllText(siteini);
                if (string.IsNullOrWhiteSpace(content) || content.Length < 7) { return; }
                content = Encoding.UTF8.GetString(LZ4Codec.Unwrap(Convert.FromBase64String(content)));
                iv = content.Substring(0, 6);
                content = AESHelper.AesDecrypt(content.Substring(6), aesKey, iv);
                //解析并入库
                IniData iniData = iniParser.Parser.Parse(content);
                inis.Add(siteShortName, iniData);
                return;
            }
            inis.Add(siteShortName, new IniData());
        }

        /// <summary>
        /// 保存站点配置
        /// </summary>
        /// <param name="siteShortName">站点短名</param>
        private static void SaveSiteConfig(string siteShortName)
        {
            if (!inis.ContainsKey(siteShortName)) { return; }
            IniData iniData = inis[siteShortName];
            if (!iniData.Sections.Any()) { return; }
            string content = string.Empty, iv = content, siteini = $"{SitePacksPath}{siteShortName}.ini";

            //写出指定站点配置并压缩
            iniParser.WriteFile(siteini, iniData);
            content = File.ReadAllText(siteini);
            iv = RandomRNG(100000, 999999).ToString();
            content = $"{iv}{AESHelper.AesEncrypt(content, aesKey, iv)}";
            content = Convert.ToBase64String(LZ4Codec.Wrap(Encoding.UTF8.GetBytes(content)));
            File.WriteAllText(siteini, content);
        }

        /// <summary>
        /// 读取或更改站点设置
        /// </summary>
        /// <param name="siteShortName">站点短名</param>
        /// <param name="siteConfig">设置参数表</param>
        /// <param name="configType">设置方法</param>
        public static string SiteConfig(string siteShortName, SiteConfigArgs siteConfig, SiteConfigType configType = SiteConfigType.Read)
        {
            if (string.IsNullOrWhiteSpace(siteShortName)) { return string.Empty; }

            IniData iniData = new IniData();
            if (inis.ContainsKey(siteShortName)) { iniData = inis[siteShortName]; }

            switch (configType)
            {
                case SiteConfigType.Change:
                    if (string.IsNullOrWhiteSpace(siteConfig.Section) || string.IsNullOrWhiteSpace(siteConfig.Key)) { return string.Empty; }
                    iniData[siteConfig.Section][siteConfig.Key] = siteConfig.Value;
                    inis[siteShortName] = iniData;
                    break;

                case SiteConfigType.Save:
                    SaveSiteConfig(siteShortName);
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(siteConfig.Section) || string.IsNullOrWhiteSpace(siteConfig.Key)) { return string.Empty; }
                    if (string.IsNullOrWhiteSpace(iniData.GetKey(siteConfig.Section)))
                    {
                        try
                        {
                            LoadSiteConfig(siteShortName);
                            iniData = inis[siteShortName];
                        }
                        catch { }
                    }
                    return iniData[siteConfig.Section][siteConfig.Key] ?? string.Empty;
            }
            return string.Empty;
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

    /// <summary>
    /// 站点设置参数
    /// </summary>
    public class SiteConfigArgs
    {
        /// <summary>
        /// 项名
        /// </summary>
        public string Section { get; set; }
        /// <summary>
        /// 键名
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; }
    }

}
