using MoeLoaderDelta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SitePack
{
    class SiteRule34 : AbstractImageSite
    {
        private string filterTag;
        private SiteBooru booru;
        private SessionClient Sweb = new SessionClient();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        public override string ShortName { get { return "rule34"; } }
        private Rule34srcType srcType = Rule34srcType.Filter;
        public enum Rule34srcType { Filter, Full }

        public override string SiteUrl { get { return "https://rule34.xxx"; } }
        public override string SiteName
        {
            get
            {
                if (srcType == Rule34srcType.Filter)
                    return "rule34.xxx [Filter]";
                else
                    return "rule34.xxx [All]";
            }
        }
        public override string ToolTip
        {
            get
            {
                if (srcType == Rule34srcType.Filter)
                    return "过滤排除部分欧美风格等作品";
                else
                    return "可获得完整的搜索结果";
            }
        }
        public override string ShortType
        {
            get
            {
                if (srcType == Rule34srcType.Filter)
                    return "[FL]";
                else
                    return "[ALL]";
            }
        }


        public SiteRule34(Rule34srcType srcType)
        {
            this.srcType = srcType;
            booru = new SiteBooru(
                SiteUrl,
                SiteUrl + "/index.php?page=dapi&s=post&q=index&pid={0}&limit={1}&tags={2}",
                SiteUrl + "/autocomplete.php?q={0}",
                 SiteName, ShortName, Referer, true, BooruProcessor.SourceType.XML);
            StringBuilder sb = new StringBuilder();
            sb.Append("-anthro -lisa_simpson -animal -lapis_lazuli_(steven_universe) -lapis_lazuli_(jewel_pet) -abs -yaoi -yamatopawa");
            sb.Append("-starit -horizontal_cloaca -animal_genitalia -princess_zelda -legoman -soul_calibur -soulcalibur -spiderman");
            sb.Append("-spiderman_(series) -gardevoir -dragon_ball -dragon_ball_z -buttplug -labor -zwijgen -my_little_pony -army");
            sb.Append("-nintendo -family_guy -alien -butt_grab -halo_(series) -justmegabenewell -sangheili -mammal -madeline_fenton");
            sb.Append("-onigrift -widowmaker -mastodon -mmjsoh -iontoon -zootopia -torbj?rn -noill -canine -dragon -sonic_(series) -blood");
            sb.Append("-phillipthe2 -sims_4 -dota -bull_(noill) -gats -adventure_time -undertale -xmen -disney -alyssa_bbw -pernalonga -bones");
            sb.Append("-mark -dexter's_laboratory -camp_lazlo -male_only -steven_universe -bara -princess_peach -super_mario_bros. -athorment");
            sb.Append("-male_focus -autofellatio -llortor -super_saiyan -aka6 -resident_evil -street_fighter -avian -dc -haydee -world_of_warcraft");
            sb.Append("-scalie -male_pov -animal_humanoid -kirby_(series) -mcarson -huge_cock -dickgirl -rasmustheowl -velma_dinkley -irispoplar");
            sb.Append("-the_legend_of_zelda -cuphead_(game) -male -mukucookie -exitation -don't_starve -kid -batmetal -barbara_gordon");
            filterTag = sb.ToString();
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(keyWord);
            if (srcType == Rule34srcType.Filter)
            {
                sb.Append(" ");
                sb.Append(filterTag);
            }
            return booru.GetPageString(page, count, sb.ToString(), proxy);
        }


        public override List<TagItem> GetTags(string word, IWebProxy proxy)
        {
            List<TagItem> re = new List<TagItem>();
            try
            {
                string url = string.Format(booru.tagUrl, word);
                shc.Accept = SessionHeadersValue.AcceptAppJson;
                url = Sweb.Get(url, proxy, shc);

                JArray jobj = (JArray)JsonConvert.DeserializeObject(url);
                string tmpname;

                foreach (JObject jo in jobj)
                {
                    tmpname = jo["value"].ToString();
                    if (srcType == Rule34srcType.Filter && !filterTag.Contains(tmpname) || srcType == Rule34srcType.Full)
                    {
                        re.Add(new TagItem()
                        {
                            Name = tmpname,
                            Count = new Regex(@".*\(([^)]*)\)").Match(jo["label"].ToString()).Groups[1].Value
                        });
                    }
                }
            }
            catch { }

            return re.Count > 0 ? re : booru.GetTags(word, proxy);
        }


        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            return booru.GetImages(pageString, proxy);
        }
    }
}
