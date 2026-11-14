using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Object=UnityEngine.Object;
namespace OldSaber
{
    // Providers own their rows, paging, filters, login and detailed score/profile
    // screens. Do not clone/reparent them or replace their requests with API snapshots.
    public sealed class ClassicLeaderboardHost:MonoBehaviour
    {
        private float nextScan;
        private void Update()
        {
            if(!ClassicMenuSession.Enabled || !Plugin.Active || Time.unscaledTime<nextScan)return;
            nextScan=Time.unscaledTime+.5f;
            foreach(var view in Resources.FindObjectsOfTypeAll<PlatformLeaderboardViewController>())
                if(view.gameObject.scene.IsValid() && view.isActiveAndEnabled && !view.GetComponent<ClassicLeaderboard>())
                    view.gameObject.AddComponent<ClassicLeaderboard>();
        }
        private void OnDestroy()
        {foreach(var state in Resources.FindObjectsOfTypeAll<ClassicLeaderboard>())Object.Destroy(state);}
    }
    public sealed class ClassicLeaderboard:MonoBehaviour
    {
        private PlatformLeaderboardViewController view;
        private TMP_Text heading;
        private GameObject header;
        private ClassicMenuAssets assets;
        private CanvasGroup visibility;
        private bool ownsVisibility,hidden;
        private float savedAlpha;
        private bool savedInteractable,savedRaycasts;
        private float nextUpdate;
        private bool warned;
        private void Update()
        {
            if(warned || Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+.25f;
            try
            {
                if(!view)view=GetComponent<PlatformLeaderboardViewController>();
                if(!view)return;
                // Core hides inactive providers either by deactivation or by moving
                // their containers to (-999,-999,-999). ActiveSelf alone is insufficient.
                bool bl=view.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c &&
                    c.GetType().FullName=="BeatLeader.ViewControllers.LeaderboardView" && Visible(c.transform));
                var ssPanel=Find("ScoreSaberPanelScreen");var ssRows=Find("ScoreSaberLeaderboardElements");
                bool ss=Visible(ssRows);
                string provider=LeaderboardPolicy.Provider(bl,ss);
                bool installed=bl || ss || HasProviderPlugin();
                var key=Fields.Get<BeatmapKey>(view,"_beatmapKey");
                bool hide=LeaderboardPolicy.HideDefault(key.levelId,installed);
                SetHidden(hide);
                if(hide){if(header)header.SetActive(false);return;}
                if(!heading)
                {
                    var sample=view.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.font);
                    if(!sample)return;
                    assets=new ClassicMenuAssets(sample.font);
                    var rect=assets.Panel(view.transform,"OldSaber.LeaderboardHeader");header=rect.gameObject;
                    // Keep the provider's own header, profile panel and navigation unobstructed.
                    ClassicMenuAssets.Rect(rect,new Vector2(0,1),new Vector2(1,1),new Vector2(0,7),new Vector2(0,7));
                    heading=assets.Text(rect,"",3.5f);heading.enableAutoSizing=true;heading.fontSizeMin=2.5f;heading.fontSizeMax=3.5f;
                }
                header.SetActive(true);
                PlaceHeader(ssPanel);
                if(bl)heading.text=LeaderboardPolicy.Header(provider,BeatLeaderPP());
                else if(ss)
                {
                    // ScoreSaber's native profile panel is authoritative. Read only
                    // its PP label, never the per-score PP values in leaderboard rows.
                    string pp=ScoreSaberPP(ssPanel);
                    heading.text=string.IsNullOrEmpty(pp)?provider:provider+"  •  "+pp;
                }
                else heading.text=LeaderboardPolicy.Header(provider,null);
            }
            catch(Exception e)
            {
                SetHidden(false);if(header)header.SetActive(false);
                if(!warned){warned=true;Plugin.Log.Warn("Classic leaderboard integration unavailable; provider UI left intact. "+e.Message);}
            }
        }
        private void PlaceHeader(Transform scoreSaberPanel)
        {
            var parent=view.transform as RectTransform;
            float top=parent?parent.rect.yMax:40;
            var corners=new Vector3[4];
            foreach(var panel in new[]{Find("LeaderboardNavigationButtonsPanel"),Find("CustomLeaderboardPanel"),scoreSaberPanel})
            {
                if(!Visible(panel) || !(panel is RectTransform rect))continue;
                rect.GetWorldCorners(corners);
                foreach(var corner in corners)top=Mathf.Max(top,view.transform.InverseTransformPoint(corner).y);
            }
            ClassicMenuAssets.Rect((RectTransform)header.transform,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,top+5),new Vector2(0,7));
        }
        private Transform Find(string name)=>view.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        private static bool Visible(Transform t)
        {
            if(!t || !t.gameObject.activeInHierarchy)return false;
            for(var p=t;p;p=p.parent)if(p.localPosition.sqrMagnitude>250000)return false;
            return true;
        }
        private static bool HasProviderPlugin()
        {
            return IPA.Loader.PluginManager.EnabledPlugins.Any(p=>
                string.Equals(p.Id,"BeatLeader",StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Id,"ScoreSaber",StringComparison.OrdinalIgnoreCase)) ||
                // Native ScoreSaber builds do not always register an IPA plugin.
                AppDomain.CurrentDomain.GetAssemblies().Any(a=>string.Equals(a.GetName().Name,"ScoreSaber",StringComparison.OrdinalIgnoreCase));
        }
        private static double? BeatLeaderPP()
        {
            var assembly=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="BeatLeader");
            var type=assembly?.GetType("BeatLeader.DataManager.ProfileManager");
            if(type==null || !(bool)(type.GetField("HasProfile",BindingFlags.Static|BindingFlags.Public)?.GetValue(null)??false))return null;
            var player=type.GetProperty("Profile",BindingFlags.Static|BindingFlags.Public)?.GetValue(null);
            var value=player?.GetType().GetProperty("pp")?.GetValue(player);
            return value==null?(double?)null:Convert.ToDouble(value,CultureInfo.InvariantCulture);
        }
        private static string ScoreSaberPP(Transform panel)
        {
            if(!Visible(panel))return null;
            foreach(var text in panel.GetComponentsInChildren<TMP_Text>(false).OrderByDescending(t=>t.name.IndexOf("pp",StringComparison.OrdinalIgnoreCase)>=0))
            {
                string plain=Regex.Replace(text.text??"","<[^>]+>","");
                var match=Regex.Match(plain,@"\b\d[\d,.\u00a0 ]*\s*pp\b",RegexOptions.IgnoreCase);
                if(match.Success)return match.Value.Trim();
            }
            return null;
        }
        private void SetHidden(bool value)
        {
            if(value==hidden)return;
            if(value)
            {
                visibility=GetComponent<CanvasGroup>();ownsVisibility=!visibility;
                if(!visibility)visibility=gameObject.AddComponent<CanvasGroup>();
                savedAlpha=visibility.alpha;savedInteractable=visibility.interactable;savedRaycasts=visibility.blocksRaycasts;
                visibility.alpha=0;visibility.interactable=false;visibility.blocksRaycasts=false;hidden=true;
            }
            else
            {
                if(visibility){visibility.alpha=savedAlpha;visibility.interactable=savedInteractable;visibility.blocksRaycasts=savedRaycasts;if(ownsVisibility)Object.Destroy(visibility);}
                visibility=null;hidden=false;
            }
        }
        private void OnDisable(){SetHidden(false);if(header)header.SetActive(false);}
        private void OnDestroy(){SetHidden(false);if(header)Object.Destroy(header);assets?.Dispose();}
    }
}
