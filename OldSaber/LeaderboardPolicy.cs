using System;
using System.Globalization;
namespace OldSaber
{
    internal static class LeaderboardPolicy
    {
        internal static string Provider(bool beatLeaderVisible,bool scoreSaberVisible)=>beatLeaderVisible?"BeatLeader":scoreSaberVisible?"ScoreSaber":"Beat Saber";
        internal static bool HideDefault(string levelId,bool hasLeaderboardProvider)=>!hasLeaderboardProvider && !string.IsNullOrEmpty(levelId) && levelId.StartsWith("custom_level_",StringComparison.OrdinalIgnoreCase);
        internal static string Header(string provider,double? pp)
        {
            if(provider=="Beat Saber")return "BEAT SABER LEADERBOARD";
            if(!pp.HasValue || double.IsNaN(pp.Value) || double.IsInfinity(pp.Value) || pp.Value<0)return provider;
            return provider+"  •  "+pp.Value.ToString("N2",CultureInfo.CurrentCulture)+" pp";
        }
    }
}
