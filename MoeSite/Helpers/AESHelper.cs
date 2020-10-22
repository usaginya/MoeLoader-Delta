using System;
using System.Security.Cryptography;
using System.Text;

namespace MoeLoaderDelta.Helpers
{
    public static class AESHelper
    {
        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="decryptStr">要解密的串</param>
        /// <param name="aesKey">密钥</param>
        /// <param name="aesIV">IV</param>
        public static string AesDecrypt(string decryptStr, string aesKey, string aesIV)
        {
            try
            {
                byte[] byteKEY = GetSHA256(aesKey);
                byte[] byteIV = GetSHA1(aesIV);

                byte[] byteDecrypt = Convert.FromBase64String(decryptStr);

                var _aes = new RijndaelManaged
                {
                    Padding = PaddingMode.PKCS7,
                    Mode = CipherMode.CBC,
                    Key = byteKEY,
                    IV = byteIV
                };

                var _crypto = _aes.CreateDecryptor(byteKEY, byteIV);
                byte[] decrypted = _crypto.TransformFinalBlock(
                    byteDecrypt, 0, byteDecrypt.Length);

                _crypto.Dispose();

                return Encoding.UTF8.GetString(decrypted);
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="aesKey">密钥</param>
        /// <param name="aesIV">IV</param>
        public static string AesEncrypt(string content, string aesKey, string aesIV)
        {
            try
            {
                byte[] byteKEY = GetSHA256(aesKey);
                byte[] byteIV = GetSHA1(aesIV);
                byte[] byteContnet = Encoding.UTF8.GetBytes(content);

                var _aes = new RijndaelManaged
                {
                    Padding = PaddingMode.PKCS7,
                    Mode = CipherMode.CBC,
                    Key = byteKEY,
                    IV = byteIV
                };

                var _crypto = _aes.CreateEncryptor(byteKEY, byteIV);
                byte[] decrypted = _crypto.TransformFinalBlock(
                    byteContnet, 0, byteContnet.Length);

                _crypto.Dispose();

                return Convert.ToBase64String(decrypted);
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// SHA256
        /// </summary>
        public static byte[] GetSHA256(string strData)
        {
            byte[] bytValue = Encoding.UTF8.GetBytes(strData);
            try
            {
                return new SHA256CryptoServiceProvider().ComputeHash(bytValue);
            }
            catch { return new byte[0]; }
        }

        /// <summary>
        /// SHA1
        /// </summary>
        public static byte[] GetSHA1(string strData)
        {
            byte[] bytValue = Encoding.UTF8.GetBytes(strData);
            try
            {
                bytValue = new SHA1CryptoServiceProvider().ComputeHash(bytValue);
                Array.Resize(ref bytValue, 16);
                return bytValue;
            }
            catch { return new byte[0]; }
        }

    }
}
