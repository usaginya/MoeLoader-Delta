using MoeLoaderDelta;
using System.Collections.Generic;
using System.Net;

namespace SitePack
{
    class SiteProvider
    {
        public List<IMageSite> SiteList(IWebProxy proxy)
        {
            List<IMageSite> sites = new List<IMageSite>();

            bool ecchimode = System.IO.File.Exists(
                $"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace("file:\\", string.Empty)}\\SitePacks\\18x.txt"
                );

            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.Author, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.Tag, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.TagFull, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.Day, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.Week, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.Month, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.Pid, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.PidPlus, proxy));
            sites.Add(new SitePixiv(SitePixiv.PixivSrcType.ExtStteing_0, proxy));

            sites.Add(new SiteLargeBooru(
                "https://yande.re",
                "https://yande.re/post.xml?page={0}&limit={1}&tags={2}", //XML
                "https://yande.re/tag.xml?limit={0}&order=count&name={1}",
                "yande.re", "yande", "https://yande.re/", false, BooruProcessor.SourceType.XMLYD));

            sites.Add(new SiteLargeBooru(
                "https://konachan.com",
                "https://konachan.com/post.json?page={0}&limit={1}&tags={2}",
                "https://konachan.com/tag.xml?limit={0}&order=count&name={1}",
                "konachan.com", "konachan", null, false, BooruProcessor.SourceType.JSONKnc));

            sites.Add(new SiteBooru(
                    "https://danbooru.donmai.us",
                    "https://danbooru.donmai.us/posts.json?page={0}&limit={1}&tags={2}",
                    "https://danbooru.donmai.us/tags/autocomplete.json?search%5Bname_matches%5D={0}",
                    "danbooru.donmai.us", "donmai", "https://danbooru.donmai.us/", false, BooruProcessor.SourceType.JSON,
                    "https://danbooru.donmai.us/login"));

            sites.Add(new SiteBooru(
                "http://behoimi.org",
                "http://behoimi.org/post/index.xml?page={0}&limit={1}&tags={2}",
                "http://behoimi.org/tag/index.xml?limit={0}&order=count&name={1}",
                "behoimi.org", "behoimi", "http://behoimi.org/", false, BooruProcessor.SourceType.XML));

            //sites.Add(new SiteBooru(
            //    "http://wakku.to/post/index.xml?page={0}&limit={1}&tags={2}",
            //    "http://wakku.to/tag/index.xml?limit={0}&order=count&name={1}",
            //    "wakku.to", "wakku", null, false, BooruProcessor.SourceType.XML));

            sites.Add(new SiteSankaku("chan"));
            if (ecchimode)
                sites.Add(new SiteSankaku("idol"));

            sites.Add(new SiteBooru(
            "https://safebooru.org",
            "https://safebooru.org/index.php?page=dapi&s=post&q=index&pid={0}&limit={1}&tags={2}",
            "https://safebooru.org/index.php?page=dapi&s=tag&q=index&order=name&limit={0}&name={1}",
            "safebooru.org", "safebooru", null, true, BooruProcessor.SourceType.XML));

            //sites.Add(new SiteBooru(
            //    "https://gelbooru.com/index.php?page=dapi&s=post&q=index&pid={0}&limit={1}&tags={2}",
            //    "https://gelbooru.com/index.php?page=dapi&s=tag&q=index&order=name&limit={0}&name={1}",
            //    "gelbooru.com", "gelbooru", null, true, BooruProcessor.SourceType.XML));
            sites.Add(new SiteGelbooru());

            sites.Add(new SiteEshuu(1)); //tag
            sites.Add(new SiteEshuu(3)); //artist
            sites.Add(new SiteEshuu(2)); //source
            sites.Add(new SiteEshuu(4)); //chara

            sites.Add(new SiteZeroChan());

            sites.Add(new SiteMjvArt());

            sites.Add(new SiteWCosplay());

            sites.Add(new SiteMiniTokyo(1));
            sites.Add(new SiteMiniTokyo(2));

            sites.Add(new SiteYuriimg());

            if (ecchimode)
            {
                sites.Add(new SiteLargeBooru(
                    "https://lolibooru.moe",
                    "https://lolibooru.moe/post.xml?page={0}&limit={1}&tags={2}", //XML
                    "https://lolibooru.moe/tag.xml?limit={0}&order=count&name={1}",
                    "lolibooru.moe", "lolibooru", "https://lolibooru.moe/", false, BooruProcessor.SourceType.XMLYDNV));

                sites.Add(new SiteATFBooru());

                sites.Add(new SiteRule34(SiteRule34.Rule34srcType.Filter));
                sites.Add(new SiteRule34(SiteRule34.Rule34srcType.Full));
            }

            sites.Add(new SiteKawaiinyan(SiteKawaiinyan.KawaiiSrcType.TagPxO));
            sites.Add(new SiteKawaiinyan(SiteKawaiinyan.KawaiiSrcType.TagPxP));
            sites.Add(new SiteKawaiinyan(SiteKawaiinyan.KawaiiSrcType.TagPxL));
            return sites;
        }
    }
}