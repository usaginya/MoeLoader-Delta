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
        /// 取文件MD5
        /// </summary>
        /// <param name="path">文件名</param>
        /// <param name="lower">使用小写</param>
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
        /// 取文件MD5 大写字母
        /// </summary>
        /// <param name="path">文件名</param>
        /// <returns></returns>
        public static string GetMD5Hash(string path)
        {
            return GetMD5Hash(path, false);
        }

        /// <summary>
        /// 移动文件夹中的所有文件夹与文件到另一个文件夹
        /// From http://blog.csdn.net/szsbell/article/details/51800424
        /// YIU modified
        /// </summary>
        /// <param name="sourcePath">源文件夹</param>
        /// <param name="destPath">目标文件夹</param>
        /// <param name="excludeFiles">排除的文件名或者目录</param>
        /// <returns>错误信息</returns>
        public static string MoveFolder(string sourcePath, string destPath, List<string> excludeFiles)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        return "创建目标目录失败：" + ex.Message;
                    }
                }
                //获得源文件下所有文件
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
                            //覆盖模式
                            if (File.Exists(destFile))
                            {
                                File.Delete(destFile);
                            }
                            File.Move(c, destFile);
                        }
                    }
                    catch { }
                });

                //获得源文件下所有目录文件
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    bool noExcl = true;
                    string fileName = Path.GetFileName(c);
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必须要在同一个根目录下移动才有效，不能在不同卷中移动。
                    //Directory.Move(c, destDir);

                    try
                    {
                        noExcl = !excludeFiles.Contains(fileName);
                    }
                    catch { }

                    if (noExcl)
                    {
                        //采用递归的方法实现
                        MoveFolder(c, destDir, excludeFiles);
                    }
                });

                //删除空目录
                DirectoryInfo di = new DirectoryInfo(sourcePath);
                if (di.GetFiles().Length + di.GetDirectories().Length < 1)
                    Directory.Delete(sourcePath);
            }
            else
            {
                return "源目录不存在";
            }
            return "";
        }
        /// <summary>
        /// 移动文件夹中的所有文件夹与文件到另一个文件夹
        /// </summary>
        /// <param name="sourcePath">源文件夹</param>
        /// <param name="destPath">目标文件夹</param>
        public static void MoveFolder(string sourcePath, string destPath)
        {
            MoveFolder(sourcePath, destPath, null);
        }
    }
}
