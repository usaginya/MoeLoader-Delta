namespace SitePack
{
    public class SiteLargeBooru : SiteBooru
    {
        public SiteLargeBooru(
            string siteUrl,
            string Url,
            string tagUrl,
            string siteName,
            string shortName,
            string referer,
            bool needMinus,
            MoeLoaderDelta.BooruProcessor.SourceType srcType
            ) : base(
                  siteUrl,
                  Url,
                  tagUrl,
                  siteName,
                  shortName,
                  referer,
                  needMinus,
                  srcType
                  )
        { }
    }
}
