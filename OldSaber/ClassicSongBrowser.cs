using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace OldSaber
{
    public sealed class ClassicLibraryTabs:MonoBehaviour
    {
        private ClassicMenuAssets assets;private NativeMenuMask mask;private RectTransform root;
        private LevelFilteringNavigationController view;private BeatmapLevelsModel model;
        private bool custom,selected;private float retry;private string signature;
        private TMP_Text status;
        internal void Build(LevelFilteringNavigationController owner)
        {
            view=owner;model=Fields.Get<BeatmapLevelsModel>(view,"_beatmapLevelsModel");
            var sample=view.GetComponentInChildren<TMP_Text>(true);if(!sample)throw new InvalidOperationException("Library text template not ready");
            assets=new ClassicMenuAssets(sample.font);mask=new NativeMenuMask(transform);
            root=assets.Panel(transform,"OldSaber.Library");ClassicMenuAssets.Rect(root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            assets.Button(root,"OST 1",new Vector2(-25,0),new Vector2(45,9),()=>Choose(false));
            var customs=assets.Button(root,"CUSTOM SONGS",new Vector2(25,0),new Vector2(45,9),()=>Choose(true));
            customs.interactable=Fields.Get<bool>(view,"_enableCustomLevels");
            status=assets.Text(root,"",3);ClassicMenuAssets.Rect(status.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-9),new Vector2(110,6));
            mask.Hide();Choose(false);
        }
        private void Choose(bool value)
        {
            custom=value;selected=false;signature=null;retry=0;
            if(custom && !model.GetAllPacks().Any(p=>p.AllBeatmapLevels().Any(l=>l.levelID.StartsWith("custom_level_",StringComparison.OrdinalIgnoreCase))))typeof(LevelFilteringNavigationController).GetMethod("UpdateCustomSongs",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null);
            Refresh();
        }
        private void Refresh()
        {
            var packs=model.GetAllPacks().ToArray();
            BeatmapLevelPack pack;
            if(custom)
            {
                var levels=packs.SelectMany(p=>p.AllBeatmapLevels()).Where(l=>l.levelID.StartsWith("custom_level_",StringComparison.OrdinalIgnoreCase)).GroupBy(l=>l.levelID).Select(g=>g.First()).OrderBy(l=>l.songName,StringComparer.CurrentCultureIgnoreCase).ToArray();
                string sig=string.Join("|",levels.Select(l=>l.levelID));
                if(selected && sig==signature && CurrentMatches(levels))return;
                signature=sig;pack=BeatmapLevelPack.CreateLevelPackForFiltering(levels);status.text=levels.Length==0?"No custom songs loaded.":levels.Length+" custom songs";
            }
            else
            {
                pack=packs.FirstOrDefault(p=>string.Equals(p.packID,"OstVol1",StringComparison.OrdinalIgnoreCase) || p.packName=="Original Soundtrack Vol. 1" || p.packName=="OST Vol. 1");
                if(pack==null){status.text="Loading OST 1…";return;}
                if(selected && CurrentMatches(pack.AllBeatmapLevels()))return;status.text="Original Soundtrack";
            }
            typeof(LevelFilteringNavigationController).GetMethod("HandleLevelSearchViewControllerDidFilterBeatmapLevelCollection",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{pack,new LevelSelectionOptions{isFiltered=true,sortAlphabetically=false}});
            selected=true;
            var list=Resources.FindObjectsOfTypeAll<LevelCollectionViewController>().FirstOrDefault(v=>v.gameObject.scene==view.gameObject.scene && v.isActiveAndEnabled);
            var first=pack.AllBeatmapLevels().FirstOrDefault();if(list && first!=null)list.SelectLevel(first);
        }
        private bool CurrentMatches(IEnumerable<BeatmapLevel> expected)
        {
            var list=Resources.FindObjectsOfTypeAll<LevelCollectionViewController>().FirstOrDefault(v=>v.gameObject.scene==view.gameObject.scene && v.isActiveAndEnabled);
            if(!list)return selected;
            var table=Fields.Get<LevelCollectionTableView>(list,"_levelCollectionTableView");
            if(Fields.Get<bool>(table,"_showLevelPackHeader"))return false;
            var actual=Fields.Get<IReadOnlyList<BeatmapLevel>>(table,"_beatmapLevels");
            return actual!=null && new HashSet<string>(expected.Select(l=>l.levelID)).SetEquals(actual.Select(l=>l.levelID));
        }
        private void LateUpdate(){if(root){mask.Hide();if(Time.unscaledTime>=retry){retry=Time.unscaledTime+1;Refresh();}}}
        private void OnDisable(){mask?.Restore();}
        private void OnDestroy(){mask?.Dispose();if(root)Object.Destroy(root.gameObject);assets?.Dispose();}
    }
    public sealed class ClassicSongList:MonoBehaviour
    {
        private ClassicMenuAssets assets;private NativeMenuMask mask;private RectTransform root;
        private LevelCollectionViewController view;private LevelCollectionTableView native;
        private readonly List<Button> rows=new List<Button>();private readonly List<TMP_Text> labels=new List<TMP_Text>();
        private IReadOnlyList<BeatmapLevel> levels;private int page;private TMP_Text status;private BeatmapLevel selected;
        internal void Build(LevelCollectionViewController owner)
        {
            view=owner;native=Fields.Get<LevelCollectionTableView>(view,"_levelCollectionTableView");
            assets=new ClassicMenuAssets(view.GetComponentInChildren<TMP_Text>(true).font);mask=new NativeMenuMask(transform);
            root=assets.Panel(transform,"OldSaber.SongList");ClassicMenuAssets.Rect(root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(60,80));
            assets.Button(root,"UP",new Vector2(0,34),new Vector2(56,8),()=>{page=Math.Max(0,page-1);RefreshRows();});
            for(int i=0;i<6;i++)
            {
                int row=i;var b=assets.Button(root,"",new Vector2(0,23-i*9),new Vector2(56,8),()=>Select(row));rows.Add(b);
                var t=b.GetComponentInChildren<TMP_Text>();t.alignment=TextAlignmentOptions.Left;t.enableWordWrapping=false;t.overflowMode=TextOverflowModes.Ellipsis;t.fontSize=3.4f;labels.Add(t);
                t.rectTransform.offsetMin=new Vector2(2,0);t.rectTransform.offsetMax=new Vector2(-2,0);
            }
            assets.Button(root,"DOWN",new Vector2(0,-34),new Vector2(56,8),()=>{if(levels!=null && (page+1)*6<levels.Count)page++;RefreshRows();});
            status=assets.Text(root,"",2.7f);ClassicMenuAssets.Rect(status.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-26),new Vector2(56,6));
            LateUpdate();
        }
        private void Select(int row)
        {
            int index=page*6+row;if(levels==null || index>=levels.Count)return;selected=levels[index];
            native.SelectLevel(selected);
            // SelectLevel already invokes the native row callback (including preview and details).
            RefreshRows();
        }
        private void RefreshRows()
        {
            for(int i=0;i<rows.Count;i++)
            {
                int index=page*6+i;bool exists=levels!=null && index<levels.Count;rows[i].gameObject.SetActive(exists);
                if(exists){var l=levels[index];labels[i].text=l.songName+"\n<size=70%>"+l.songAuthorName+"</size>";ClassicMenuAssets.SetSelected(rows[i],l==selected);}
            }
            status.text=levels==null || levels.Count==0?"No songs available":(page+1)+" / "+Math.Max(1,(levels.Count+5)/6);
        }
        private void LateUpdate()
        {
            if(!root)return;mask.Hide();var parent=(RectTransform)transform;float scale=Mathf.Min(parent.rect.width/60,parent.rect.height/80);root.localScale=Vector3.one*scale;
            root.anchoredPosition=new Vector2(Mathf.Max(0,(parent.rect.width-60*scale)/2)-2,0);
            var current=Fields.Get<IReadOnlyList<BeatmapLevel>>(native,"_beatmapLevels");
            if(!ReferenceEquals(levels,current)){levels=current;page=0;RefreshRows();}
        }
        private void OnDisable(){mask?.Restore();}
        private void OnDestroy(){mask?.Dispose();if(root)Object.Destroy(root.gameObject);assets?.Dispose();}
    }
}
