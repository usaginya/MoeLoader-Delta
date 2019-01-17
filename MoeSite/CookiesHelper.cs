using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MoeLoaderDelta
{
    public class CookiesHelper
    {

        #region WinApi
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData,
                                                                                 int dwFlags, IntPtr lpReserved);
        #endregion

        /// <summary>
        /// Get Internet Explorer Cookies
        /// </summary>
        /// <param name="url">full url</param>
        /// <returns></returns>
        public static string GetIECookies(string url)
        {

            uint datasize = 256;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x2000, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;

                cookieData = new StringBuilder((int)datasize);
                if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x00002000, IntPtr.Zero))
                    return null;
            }
            return cookieData.ToString();
        }

    }
}
