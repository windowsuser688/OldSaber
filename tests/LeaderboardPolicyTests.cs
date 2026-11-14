using System;
using System.Globalization;
using OldSaber;
public static class LeaderboardPolicyTests
{
 static void Check(bool v,string message){if(!v)throw new Exception(message);}
 public static void Main()
 {
  CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
  Check(LeaderboardPolicy.Provider(true,false)=="BeatLeader","BeatLeader selection");
  Check(LeaderboardPolicy.Provider(false,true)=="ScoreSaber","Switch to ScoreSaber");
  Check(LeaderboardPolicy.Provider(false,false)=="Beat Saber","Native fallback");
  Check(LeaderboardPolicy.HideDefault("custom_level_ABC",false),"Custom song without provider must hide leaderboard");
  Check(!LeaderboardPolicy.HideDefault("custom_level_ABC",true),"Provider supports custom songs");
  Check(!LeaderboardPolicy.HideDefault("OST1Song",false),"Official song keeps native leaderboard");
  Check(!LeaderboardPolicy.HideDefault(null,false),"Loading state must not hide leaderboard");
  Check(LeaderboardPolicy.Header("BeatLeader",1234.56)=="BeatLeader  •  1,234.56 pp","Player total PP formatting");
  Check(LeaderboardPolicy.Header("ScoreSaber",null)=="ScoreSaber","Do not invent unknown profile PP");
  Check(LeaderboardPolicy.Header("BeatLeader",double.NaN)=="BeatLeader","Reject invalid PP");
  Check(!LeaderboardPolicy.Header("Beat Saber",0).Contains("pp"),"Native leaderboard has no performance points");
  Console.WriteLine("PASS: provider switching, official/custom fallback matrix, PP formatting and absent/invalid profile handling.");
 }
}
