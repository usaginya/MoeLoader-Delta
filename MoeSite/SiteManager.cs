﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 管理站點定義
    /// </summary>
    public class SiteManager
    {
        private static List<ImageSite> sites = new List<ImageSite>();
        private static SiteManager instance;

        private SiteManager()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\SitePacks\\";

            string[] dlls = Directory.GetFiles(path, "SitePack*.dll", SearchOption.TopDirectoryOnly);

            #region 保證有基本站點包路徑
            if (dlls.Length < 1)
            {
                List<string> dlll = new List<string>();
                string basisdll = path + "SitePack.dll";

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
                    Type type = Assembly.LoadFile(dll).GetType("SitePack.SiteProvider", true, false);
                    MethodInfo methodInfo = type.GetMethod("SiteList");
                    sites.AddRange(methodInfo.Invoke(Activator.CreateInstance(type), null) as List<ImageSite>);
                }
                catch (Exception ex)
                {
                    File.AppendAllText(path + "site_error.txt", ex.ToString() + "\r\n");
                }
            }
        }

        /// <summary>
        /// 站點定義管理者
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
        /// 站點集合
        /// </summary>
        public List<ImageSite> Sites
        {
            get
            {
                return sites;
            }
        }
    }
}
