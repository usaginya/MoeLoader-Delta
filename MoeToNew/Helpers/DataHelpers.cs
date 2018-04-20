using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MoeLoaderDelta.Helpers
{
    class DataHelpers
    {
        /// <summary>
        /// 取檔案MD5
        /// </summary>
        /// <param name="path">檔案名</param>
        /// <param name="lower">使用小寫</param>
        /// <returns></returns>
        public static string GetMD5Hash(string path, bool lower)
        {
            MD5 md5 = MD5.Create();
            if (!File.Exists(path))
                return "";

            FileStream stream = File.OpenRead(path);
            byte[] data = md5.ComputeHash(stream);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }
            return lower ? sb.ToString() : sb.ToString().ToUpper();
        }
        /// <summary>
        /// 取檔案MD5 大寫字母
        /// </summary>
        /// <param name="path">檔案名</param>
        /// <returns></returns>
        public static string GetMD5Hash(string path)
        {
            return GetMD5Hash(path, false);
        }

        /// <summary>
        /// 移動資料夾中的所有資料夾與檔案到另一個資料夾
        /// From http://blog.csdn.net/szsbell/article/details/51800424
        /// YIU modified
        /// </summary>
        /// <param name="sourcePath">源資料夾</param>
        /// <param name="destPath">目標資料夾</param>
        /// <param name="excludeFiles">排除的檔案名或者目錄</param>
        /// <returns>錯誤訊息</returns>
        public static string MoveFolder(string sourcePath, string destPath, List<string> excludeFiles)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目標目錄不存在則創建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        return "創建目標目錄失敗：" + ex.Message;
                    }
                }
                //獲得源檔案下所有檔案
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    bool noExcl = true;
                    string fileName = Path.GetFileName(c);

                    try
                    {
                        noExcl = !excludeFiles.Contains(fileName);
                    }
                    catch { }

                    try
                    {
                        if (noExcl)
                        {
                            string destFile = Path.Combine(new string[] { destPath, fileName });
                            //覆蓋模式
                            if (File.Exists(destFile))
                            {
                                File.Delete(destFile);
                            }
                            File.Move(c, destFile);
                        }
                    }
                    catch { }
                });

                //獲得源檔案下所有目錄檔案
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    bool noExcl = true;
                    string fileName = Path.GetFileName(c);
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必須要在同一個根目錄下移動才有效，不能在不同卷中移動。
                    //Directory.Move(c, destDir);

                    try
                    {
                        noExcl = !excludeFiles.Contains(fileName);
                    }
                    catch { }

                    if (noExcl)
                    {
                        //採用遞迴的方法實現
                        MoveFolder(c, destDir, excludeFiles);
                    }
                });

                //刪除空目錄
                DirectoryInfo di = new DirectoryInfo(sourcePath);
                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                    Directory.Delete(sourcePath);
            }
            else
            {
                return "源目錄不存在";
            }
            return "";
        }
        /// <summary>
        /// 移動資料夾中的所有資料夾與檔案到另一個資料夾
        /// </summary>
        /// <param name="sourcePath">源資料夾</param>
        /// <param name="destPath">目標資料夾</param>
        public static void MoveFolder(string sourcePath, string destPath)
        {
            MoveFolder(sourcePath, destPath, null);
        }
    }
}
