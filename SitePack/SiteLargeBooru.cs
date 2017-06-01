using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SitePack
{
    public class SiteLargeBooru : SiteBooru
    {
        private const int LWIDTH = 300;
        private const int LHEIGHT = 300;

        public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(LWIDTH, LHEIGHT); } }

        public SiteLargeBooru(string siteUrl, string tagUrl, string siteName, string shortName, string referer, bool needMinus, MoeLoader.BooruProcessor.SourceType srcType)
            : base(siteUrl, tagUrl, siteName, shortName, referer, needMinus, srcType)
        {
        }
    }
}
