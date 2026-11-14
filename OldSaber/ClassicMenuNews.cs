using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
namespace OldSaber
{
    public sealed class ClassicMenuNews:MonoBehaviour
    {
        private TMP_Text title,body,counter;
        private UnityWebRequest request;
        private bool ready,sidePanel;
        private string archive;
        private static string cache,cacheSource;
        private static float cacheTime;
        internal void Initialize(ClassicMenuAssets assets,bool sidePanel=false)
        {
            this.sidePanel=sidePanel;
            var release=(string)assets.Data["screens"]["changelog"];
            var ids=new System.Collections.Generic.HashSet<string>{release};
            foreach(var n in assets.Data["nodes"])if(ids.Contains((string)n["parent"]??""))ids.Add((string)n["id"]);
            var source=assets.Data["nodes"].Where(n=>ids.Contains((string)n["id"]) && n["text"]!=null).ToArray();
            archive=(string)source.First(n=>(string)n["name"]=="Text")["text"]["m_text"];
            title=assets.Text(transform,"RELEASE INFO",sidePanel?8:6);
            title.color=ClassicGuiState.ColorValue(source.First(n=>(string)n["name"]=="Title")["text"]["m_fontColor"]);
            ClassicMenuAssets.Rect(title.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,sidePanel?30:23),new Vector2(sidePanel?90:140,sidePanel?12:9));
            body=assets.Text(transform,"",4);body.richText=false;body.enableWordWrapping=true;body.alignment=TextAlignmentOptions.TopLeft;body.overflowMode=TextOverflowModes.Page;
            ClassicMenuAssets.Rect(body.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,sidePanel?-1:-2),new Vector2(sidePanel?88:138,sidePanel?50:40));
            assets.Button(transform,"<",new Vector2(sidePanel?-43:-61,-34),new Vector2(sidePanel?10:12,7),()=>Page(-1));
            assets.Button(transform,">",new Vector2(sidePanel?43:61,-34),new Vector2(sidePanel?10:12,7),()=>Page(1));
            assets.Button(transform,"REFRESH",new Vector2(sidePanel?-22:-30,-34),new Vector2(30,7),()=>{Cancel();StartCoroutine(Load(true));});
            assets.Button(transform,"0.10.1 INFO",new Vector2(sidePanel?20:23,-34),new Vector2(34,7),()=>{Cancel();title.text="RELEASE INFO — 0.10.1";SetBody(archive);});
            counter=assets.Text(transform,"",3);ClassicMenuAssets.Rect(counter.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(sidePanel?-2:0,-34),new Vector2(sidePanel?10:15,7));
            ready=true;
        }
        private void OnEnable(){if(ready)StartCoroutine(Load(false));}
        private IEnumerator Load(bool force)
        {
            bool bl=IPA.Loader.PluginManager.EnabledPlugins.Any(p=>string.Equals(p.Id,"BeatLeader",StringComparison.OrdinalIgnoreCase));
            string source=bl?"BeatLeader":"Beat Saber • Steam";
            title.text=sidePanel?"RELEASE INFO\n<size=40%>"+source+"</size>":"RELEASE INFO — "+source;
            if(!force && cacheSource==source && cache!=null && Time.realtimeSinceStartup-cacheTime<300){SetBody(cache);yield break;}
            SetBody("Loading announcements…");
            string url=MenuNewsContent.SteamUrl;
            if(bl)
            {
                var assembly=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="BeatLeader");
                var property=assembly?.GetType("BeatLeader.Utils.BLConstants")?.GetProperty("BEATLEADER_API_URL",BindingFlags.Public|BindingFlags.Static);
                string server=null;
                try{server=property?.GetValue(null) as string;}catch(Exception e){Plugin.Log.Warn("BeatLeader news endpoint unavailable: "+e.Message);}
                if(string.IsNullOrEmpty(server)){SetBody("BeatLeader news is unavailable. Use Refresh to retry, or 0.10.1 Info for the original release notes.");yield break;}
                url=server.TrimEnd('/')+"/mod/news";
            }
            request=UnityWebRequest.Get(url);request.timeout=15;
            yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success)SetBody("Could not load "+source+" announcements. Use Refresh to retry.\n\n"+(cacheSource==source?cache:"The original release notes are available under 0.10.1 Info."));
            else
            {
                try{cache=MenuNewsContent.Parse(request.downloadHandler.text,bl);cacheSource=source;cacheTime=Time.realtimeSinceStartup;SetBody(cache);}
                catch(Exception e){SetBody("Could not read the news feed. Use Refresh to retry.");Plugin.Log.Warn("Classic menu news: "+e.Message);}
            }
            request.Dispose();request=null;
        }
        private void SetBody(string value){body.text=value;body.pageToDisplay=1;body.ForceMeshUpdate();UpdateCounter();}
        private void Page(int step){body.ForceMeshUpdate();body.pageToDisplay=Mathf.Clamp(body.pageToDisplay+step,1,Mathf.Max(1,body.textInfo.pageCount));UpdateCounter();}
        private void UpdateCounter(){if(counter)counter.text=body.pageToDisplay+"/"+Mathf.Max(1,body.textInfo.pageCount);}
        private void Cancel(){StopAllCoroutines();if(request!=null){request.Abort();request.Dispose();request=null;}}
        private void OnDisable(){Cancel();}
        private void OnDestroy(){Cancel();}
    }
}
