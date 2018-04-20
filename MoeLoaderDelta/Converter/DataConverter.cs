using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MoeLoaderDelta
{
    public class DataConverter
    {
        /// <summary>
        /// 本地Stream轉一段位元組數組
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length">指定轉換長度</param>
        /// <returns></returns>
        public static byte[] LocalStreamToByte(Stream stream, long length)
        {
            byte[] bytes = new byte[length < 1 ? 1 : length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        /// <summary>
        /// 十六進位制字串轉位元組數組
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] strHexToByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

    }
}
