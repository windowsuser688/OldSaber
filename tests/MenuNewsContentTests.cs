using System;
using OldSaber;
public static class MenuNewsContentTests
{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 public static void Main()
 {
  var bl=MenuNewsContent.Parse("{\"data\":[{\"owner\":\"BeatLeader\",\"timepost\":1700000000,\"body\":\"<p>New &amp; improved</p>\"}]}",true);
  Check(bl.Contains("BeatLeader") && bl.Contains("New & improved") && !bl.Contains("<p>"),"BeatLeader feed parsing");
  var steam=MenuNewsContent.Parse("{\"appnews\":{\"newsitems\":[{\"title\":\"Update\",\"date\":1700000000,\"contents\":\"[b]Fix[/b][img]https://example.com/a.png[/img]\"}]}}",false);
  Check(steam.Contains("Update") && steam.Contains("Fix") && !steam.Contains("example.com"),"Steam markup parsing");
  Check(MenuNewsContent.Parse("{\"data\":[]}",true).Contains("No announcements"),"Empty feed");
  Check(MenuNewsContent.Parse("{\"data\":[{\"timepost\":1700000000000}]}",true).Contains("2023-11-14"),"Milliseconds timestamp");
  bool rejected=false;try{MenuNewsContent.Parse("{}",false);}catch(FormatException){rejected=true;}Check(rejected,"Malformed feed must not masquerade as empty news");
  Check(MenuNewsContent.SteamUrl.Contains("appid=620980") && MenuNewsContent.SteamUrl.Contains("steam_community_announcements"),"Official Beat Saber announcements only");
  Console.WriteLine("PASS: BeatLeader/Steam schemas, HTML/BBCode cleanup, timestamp units, empty/malformed feed handling and Steam app/feed selection.");
 }
}
