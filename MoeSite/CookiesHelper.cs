using System.Runtime.InteropServices;

namespace MoeLoaderDelta
{
    public class CookiesHelper
    {
        /// <summary>
        /// by YIU
        /// Last 20180423
        /// </summary>

        [DllImport("lib\\Moekai.DLL.CookieReader.dll")]
        public static extern string  SearchChromeHost(string host);

        [DllImport("lib\\Moekai.DLL.CookieReader.dll")]
        public static extern string  GetChromeCookie(string host);

        [DllImport("lib\\Moekai.DLL.CookieReader.dll")]
        public static extern string  GetIECookie(string host);

    }
}
