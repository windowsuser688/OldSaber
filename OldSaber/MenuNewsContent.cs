using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace OldSaber
{
    internal static class MenuNewsContent
    {
        internal const string SteamUrl="https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=620980&count=10&maxlength=0&feeds=steam_community_announcements";
        internal static string Plain(string value)
        {
            value=Regex.Replace(value??"",@"<br\s*/?>|</p>|</div>|\[/?(?:h\d|list|\*)\]","\n",RegexOptions.IgnoreCase);
            value=Regex.Replace(value,@"\[img\].*?\[/img\]","",RegexOptions.Singleline|RegexOptions.IgnoreCase);
            value=Regex.Replace(value,@"<[^>]*>|\[/?(?:url|b|i|u|quote|previewyoutube)(?:=[^\]]*)?\]", "",RegexOptions.IgnoreCase);
            return WebUtility.HtmlDecode(value).Trim();
        }
        internal static string Parse(string json,bool beatLeader)
        {
            var data=JObject.Parse(json);var entries=beatLeader?data["data"]:data["appnews"]?["newsitems"];
            if(!(entries is JArray))throw new FormatException("The news feed returned an unexpected format.");
            var posts=entries.Take(10).Select(p=>
            {
                var time=(long?)(p[beatLeader?"timepost":"date"])??0;
                string date=time>0?(time>100000000000L?DateTimeOffset.FromUnixTimeMilliseconds(time):DateTimeOffset.FromUnixTimeSeconds(time)).ToString("yyyy-MM-dd"):"";
                string title=Plain((string)p[beatLeader?"owner":"title"]);
                return title+"  "+date+"\n\n"+Plain((string)p[beatLeader?"body":"contents"]);
            }).ToArray();
            return posts.Length==0?"No announcements are available.":string.Join("\n\n────────────────────\n\n",posts);
        }
    }
}
