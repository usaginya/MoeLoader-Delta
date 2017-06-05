using System;
using System.Collections.Generic;
using System.Reflection;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 管理站点定义
    /// </summary>
    public class SiteManager
    {
        private List<ImageSite> sites = new List<ImageSite>();
        private static SiteManager instance;

        private SiteManager()
        {
            string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (System.IO.File.Exists(path + "\\SitePacks\\SitePack.dll.new"))
            {
                //update site pack
                try
                {
                    System.IO.File.Delete(path + "\\SitePacks\\SitePack.dll");
                    System.IO.File.Move(path + "\\SitePack.dll.new", path + "\\SitePacks\\SitePack.dll");
                }
                catch { }
            }

            string[] dlls = System.IO.Directory.GetFiles(path + "\\SitePacks", "SitePack*.dll", System.IO.SearchOption.TopDirectoryOnly);
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
                    System.IO.File.AppendAllText(path + "\\site_error.txt", ex.ToString() + "\r\n");
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
        public List<ImageSite> Sites
        {
            get
            {
                return sites;
            }
        }
    }
}
