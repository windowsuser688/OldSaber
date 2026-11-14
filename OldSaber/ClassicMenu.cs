using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using UImage = UnityEngine.UI.Image;

namespace OldSaber
{
    internal static class ClassicMenuSession
    {
        internal static bool Enabled;
        internal static Action OpenModSettings;
        internal static Func<IEnumerable<BeatSaberMarkupLanguage.MenuButtons.MenuButton>> ModButtons;
    }
    public sealed class ClassicMenuHost:MonoBehaviour
    {
        private float nextScan;
        private readonly HashSet<int> failed=new HashSet<int>();
        private readonly Dictionary<int,int> environmentAttempts=new Dictionary<int,int>();
        private void Update()
        {
            if(!ClassicMenuSession.Enabled || !Plugin.Active || Time.unscaledTime<nextScan)return;
            nextScan=Time.unscaledTime+.25f;
            foreach(var view in Resources.FindObjectsOfTypeAll<MainMenuViewController>())
                if(view.gameObject.scene.IsValid() && view.isActiveAndEnabled && !view.GetComponent<ClassicMainMenu>() && !failed.Contains(view.GetInstanceID()))
                {
                    var state=view.gameObject.AddComponent<ClassicMainMenu>();
                    try{state.Build(view);}catch(Exception e){failed.Add(view.GetInstanceID());Object.Destroy(state);Plugin.Log.Error("Classic main menu failed; native menu restored. "+e);}
                }
            var activeMain=Resources.FindObjectsOfTypeAll<MainMenuViewController>().FirstOrDefault(v=>v.isActiveAndEnabled && v.gameObject.scene.IsValid());
            if(activeMain)
            {
                var flow=Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().FirstOrDefault(f=>f.gameObject.scene.IsValid());
                var right=flow?flow.rightScreenViewController:null;
                if(right && right.isActiveAndEnabled && !right.GetComponent<ClassicReleaseInfo>() && !failed.Contains(right.GetInstanceID()))
                {
                    var state=right.gameObject.AddComponent<ClassicReleaseInfo>();
                    try{state.Build(activeMain,activeMain.GetComponentInChildren<TMP_Text>(true).font);}catch(Exception e){failed.Add(right.GetInstanceID());Object.Destroy(state);Plugin.Log.Error("Classic release panel failed; native panel retained. "+e);}
                }
            }
            foreach(var manager in Resources.FindObjectsOfTypeAll<MenuEnvironmentManager>())
            {
                if(!manager.gameObject.scene.IsValid() || !manager.isActiveAndEnabled)continue;
                var sample=Resources.FindObjectsOfTypeAll<MainMenuViewController>().FirstOrDefault(v=>v.isActiveAndEnabled)?.GetComponentInChildren<TMP_Text>(true);
                if(!sample)continue;
                foreach(var entry in Fields.Get<Array>(manager,"_data"))
                {
                    if(Fields.Get<MenuEnvironmentManager.MenuEnvironmentType>(entry,"_menuEnvironmentType")!=MenuEnvironmentManager.MenuEnvironmentType.Default)continue;
                    var wrapper=Fields.Get<GameObject>(entry,"_wrapper");
                    if(!wrapper || !wrapper.activeInHierarchy || wrapper.GetComponent<ClassicMenuEnvironment>() || failed.Contains(wrapper.GetInstanceID()))continue;
                    var state=wrapper.AddComponent<ClassicMenuEnvironment>();
                    try{state.Build(sample.font);environmentAttempts.Remove(wrapper.GetInstanceID());}
                    catch(Exception e)
                    {
                        int id=wrapper.GetInstanceID();environmentAttempts.TryGetValue(id,out int attempts);environmentAttempts[id]=attempts+1;
                        Object.Destroy(state);
                        if(attempts>=19){failed.Add(id);Plugin.Log.Error("Classic menu environment failed after waiting for native templates; native environment restored. "+e);}
                    }
                }
            }
            foreach(var view in Resources.FindObjectsOfTypeAll<LevelFilteringNavigationController>())
                if(view.gameObject.scene.IsValid() && view.isActiveAndEnabled && !view.GetComponent<ClassicLibraryTabs>() && !failed.Contains(view.GetInstanceID()))
                {
                    var state=view.gameObject.AddComponent<ClassicLibraryTabs>();
                    try{state.Build(view);}catch(Exception e){failed.Add(view.GetInstanceID());Object.Destroy(state);Plugin.Log.Error("Classic library failed; native browser retained. "+e);}
                }
            foreach(var view in Resources.FindObjectsOfTypeAll<LevelCollectionViewController>())
                if(view.gameObject.scene.IsValid() && view.isActiveAndEnabled && !view.GetComponent<ClassicSongList>() && !failed.Contains(view.GetInstanceID()))
                {
                    var state=view.gameObject.AddComponent<ClassicSongList>();
                    try{state.Build(view);}catch(Exception e){failed.Add(view.GetInstanceID());Object.Destroy(state);Plugin.Log.Error("Classic song list failed; native list retained. "+e);}
                }
            foreach(var view in Resources.FindObjectsOfTypeAll<StandardLevelDetailView>())
                if(view.gameObject.scene.IsValid() && view.isActiveAndEnabled && !view.GetComponent<ClassicSongMenu>() && !failed.Contains(view.GetInstanceID()))
                {
                    var state=view.gameObject.AddComponent<ClassicSongMenu>();
                    try{state.Build(view);}catch(Exception e){failed.Add(view.GetInstanceID());Object.Destroy(state);Plugin.Log.Error("Classic song menu failed; native song view restored. "+e);}
                }
        }
        private void OnDestroy()
        {
            foreach(var s in Resources.FindObjectsOfTypeAll<ClassicMainMenu>())Object.Destroy(s);
            foreach(var s in Resources.FindObjectsOfTypeAll<ClassicSongMenu>())Object.Destroy(s);
            foreach(var s in Resources.FindObjectsOfTypeAll<ClassicSongList>())Object.Destroy(s);
            foreach(var s in Resources.FindObjectsOfTypeAll<ClassicLibraryTabs>())Object.Destroy(s);
            foreach(var s in Resources.FindObjectsOfTypeAll<ClassicReleaseInfo>())Object.Destroy(s);
            foreach(var s in Resources.FindObjectsOfTypeAll<ClassicMenuEnvironment>())Object.Destroy(s);
        }
    }
    internal sealed class ClassicMenuAssets:IDisposable
    {
        internal readonly JObject Data;
        internal readonly ClassicGuiFonts Fonts;
        internal readonly TMP_FontAsset Font;
        private readonly List<Object> owned=new List<Object>();
        private readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        internal ClassicMenuAssets(TMP_FontAsset fallback)
        {
            using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.ClassicMenu.json"))
            using(var r=new StreamReader(s))Data=JObject.Parse(r.ReadToEnd());
            Fonts=new ClassicGuiFonts(Data,fallback,owned);
            Font=Fonts.Font(((JObject)Data["fonts"]).Properties().First().Name);
        }
        internal Sprite Sprite(string key)
        {
            if(sprites.TryGetValue(key,out var result))return result;
            var t=Fonts.Texture(key);var metadata=Data["sprites"]?[key];var b=metadata?["border"];
            var border=b==null?Vector4.zero:new Vector4((float)b["x"],(float)b["y"],(float)b["z"],(float)b["w"]);
            result=UnityEngine.Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),metadata==null?100:(float)metadata["pixelsPerUnit"],0,SpriteMeshType.FullRect,border);
            owned.Add(result);sprites[key]=result;return result;
        }
        internal RectTransform Panel(Transform parent,string name)
        {var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;var rt=(RectTransform)go.transform;rt.SetParent(parent,false);return rt;}
        internal static void Rect(RectTransform r,Vector2 min,Vector2 max,Vector2 pos,Vector2 size)
        {r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;}
        internal TMP_Text Text(Transform parent,string value,float size)
        {
            var go=new GameObject("Text",typeof(RectTransform));go.layer=parent.gameObject.layer;
            var t=go.AddComponent<TextMeshProUGUI>();go.transform.SetParent(parent,false);
            t.font=Font;t.fontSharedMaterial=Font.material;t.text=value;t.fontSize=size;t.color=Color.white;
            t.alignment=TextAlignmentOptions.Center;t.enableWordWrapping=false;t.raycastTarget=false;
            Rect(t.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);return t;
        }
        internal Button Button(Transform parent,string label,Vector2 pos,Vector2 size,Action action)
        {
            var rt=Panel(parent,label);Rect(rt,new Vector2(.5f,.5f),new Vector2(.5f,.5f),pos,size);
            // Keep a full rectangular hit area, but render the original hollow stroke.
            // Sprite-less filled Images wash out the white labels in the game renderer.
            var hit=rt.gameObject.AddComponent<UImage>();hit.color=Color.clear;
            var outline=Panel(rt,"Outline");Rect(outline,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var bg=outline.gameObject.AddComponent<UImage>();bg.raycastTarget=false;
            var stroke=Data["nodes"].First(n=>(string)n["name"]=="Stroke" && n["image"]?["texture"]!=null);
            bg.sprite=Sprite((string)stroke["image"]["texture"]);bg.type=UImage.Type.Sliced;bg.color=Color.white;
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=bg;
            SetSelected(button,false);
            var text=Text(rt,label,4);text.autoSizeTextContainer=false;text.enableAutoSizing=false;
            button.onClick.AddListener(()=>action());return button;
        }
        internal static void SetSelected(Button button,bool selected)
        {
            var colors=button.colors;
            colors.normalColor=selected?new Color(.15f,.8f,1):new Color(.65f,.75f,.8f);
            colors.highlightedColor=new Color(.25f,.9f,1);colors.selectedColor=colors.normalColor;
            colors.pressedColor=new Color(.1f,.5f,.7f);colors.disabledColor=new Color(.3f,.35f,.4f,.5f);
            colors.colorMultiplier=1;colors.fadeDuration=.08f;button.colors=colors;
        }
        internal static void Layout(Transform tr,JToken n)
        {
            var t=n["transform"];var rt=tr as RectTransform;
            if(rt)ClassicGuiLayout.Rect(rt,V2(t["m_AnchorMin"]),V2(t["m_AnchorMax"]),V2(t["m_Pivot"]),V2(t["m_SizeDelta"]),V2(t["m_AnchoredPosition"]),(float)t["m_LocalPosition"]["z"]);
            else tr.localPosition=V(t["m_LocalPosition"]);
            tr.localScale=V(t["m_LocalScale"]);var q=t["m_LocalRotation"];tr.localRotation=new Quaternion((float)q["x"],(float)q["y"],(float)q["z"],(float)q["w"]);
        }
        private static Vector2 V2(JToken t)=>new Vector2((float)t["x"],(float)t["y"]);
        private static Vector3 V(JToken t)=>new Vector3((float)t["x"],(float)t["y"],(float)t["z"]);
        public void Dispose(){foreach(var o in owned)if(o)Object.Destroy(o);owned.Clear();}
    }
    public sealed class ClassicMainMenu:MonoBehaviour
    {
        private ClassicMenuAssets assets;
        private GameObject root,play,mods,more,news;
        private GameObject[] originals;private bool[] active;
        private bool committed;
        private readonly List<KeyValuePair<Button,Button>> links=new List<KeyValuePair<Button,Button>>();
        private int page;
        private readonly List<KeyValuePair<Behaviour,bool>> layouts=new List<KeyValuePair<Behaviour,bool>>();
        internal void Build(MainMenuViewController view)
        {
            assets=new ClassicMenuAssets(view.GetComponentInChildren<TMP_Text>(true).font);
            originals=Enumerable.Range(0,view.transform.childCount).Select(i=>view.transform.GetChild(i).gameObject).ToArray();active=originals.Select(o=>o.activeSelf).ToArray();
            foreach(var layout in view.GetComponents<LayoutGroup>()){layouts.Add(new KeyValuePair<Behaviour,bool>(layout,layout.enabled));layout.enabled=false;}
            root=assets.Panel(view.transform,"OldSaber.ClassicMenu").gameObject;
            ClassicMenuAssets.Rect((RectTransform)root.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(ClassicMenuLayout.Width,ClassicMenuLayout.Height));
            var nodes=new Dictionary<string,Transform>();var main=(string)assets.Data["screens"]["main"];
            var buttons=new Dictionary<string,Button>();
            foreach(var n in assets.Data["nodes"])
            {
                string id=(string)n["id"],parent=(string)n["parent"];
                if(id!=main && (parent==null || !nodes.ContainsKey(parent)))continue;
                var go=ClassicGuiNodeFactory.Create((string)n["name"],(bool)n["rect"],n["text"]!=null,true,out var text);go.layer=view.gameObject.layer;
                go.transform.SetParent(parent==null?root.transform:nodes[parent],false);nodes[id]=go.transform;
                if(parent==null)play=go;
                if(text)
                {
                    var d=n["text"];text.font=assets.Fonts.Font((string)d["font"]);text.fontSharedMaterial=text.font.material;
                    text.text=(string)d["m_text"];text.fontSize=(float)d["m_fontSize"];text.alignment=(TextAlignmentOptions)(int)d["m_textAlignment"];
                    text.color=Color.white;text.raycastTarget=false;text.autoSizeTextContainer=false;text.enableAutoSizing=false;text.enableWordWrapping=false;
                }
                if(n["image"]!=null)
                {
                    var d=n["image"];var image=go.AddComponent<UImage>();image.raycastTarget=false;image.color=ClassicGuiState.ColorValue(d["m_Color"]);
                    if(d["texture"]!=null)image.sprite=assets.Sprite((string)d["texture"]);
                    image.type=(UImage.Type)((int?)d["m_Type"]??0);
                    image.preserveAspect=(string)n["name"]=="Icon" || (bool?)d["m_PreserveAspect"]==true;
                }
                if(n["button"]!=null)
                {
                    var hit=go.AddComponent<UImage>();hit.color=Color.clear;hit.raycastTarget=true;
                    var b=go.AddComponent<Button>();b.targetGraphic=hit;buttons[(string)n["name"]]=b;
                }
                ClassicMenuAssets.Layout(go.transform,n);go.SetActive(parent==null || (bool)n["active"]);
            }
            foreach(var pair in new[]{new[]{"SoloButton","_soloButton"},new[]{"PartyButton","_partyButton"},new[]{"TutorialButton","_howToPlayButton"},new[]{"SettingsButton","_optionsButton"},new[]{"QuitButton","_quitButton"}})
            {
                var original=Fields.Get<Button>(view,pair[1]);var button=buttons[pair[0]];button.onClick.AddListener(()=>original.onClick.Invoke());links.Add(new KeyValuePair<Button,Button>(button,original));
                // Keep the extracted outline but tint it on hover/press.
                var stroke=button.GetComponentsInChildren<UImage>().FirstOrDefault(i=>i.name=="Stroke");if(stroke)button.targetGraphic=stroke;
            }
            mods=assets.Panel(root.transform,"Mods").gameObject;more=assets.Panel(root.transform,"More").gameObject;
            foreach(var panel in new[]{mods,more})ClassicMenuAssets.Rect((RectTransform)panel.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            assets.Button(root.transform,"PLAY",new Vector2(-45,38),new Vector2(27,7),()=>Show(play));
            assets.Button(root.transform,"MODS",new Vector2(-15,38),new Vector2(27,7),()=>{page=0;RefreshMods();Show(mods);});
            assets.Button(root.transform,"MORE",new Vector2(15,38),new Vector2(27,7),()=>Show(more));
            news=assets.Panel(root.transform,"ReleaseInfo").gameObject;
            ClassicMenuAssets.Rect((RectTransform)news.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            news.SetActive(false);news.AddComponent<ClassicMenuNews>().Initialize(assets);
            assets.Button(root.transform,"NEWS",new Vector2(45,38),new Vector2(27,7),()=>Show(news));
            int row=0;
            foreach(var pair in new[]{new[]{"MULTIPLAYER","_multiplayerButton"},new[]{"CAMPAIGN","_campaignButton"},new[]{"BEATMAP EDITOR","_beatmapEditorButton"}})
            {
                var original=Fields.Get<Button>(view,pair[1]);var b=assets.Button(more.transform,pair[0],new Vector2(0,16-row++*12),new Vector2(60,10),()=>original.onClick.Invoke());links.Add(new KeyValuePair<Button,Button>(b,original));
            }
            foreach(Transform child in root.transform)
                if(child.name=="PLAY" || child.name=="MODS" || child.name=="MORE" || child.name=="NEWS")
                {var tab=(RectTransform)child;tab.anchorMin=tab.anchorMax=new Vector2(.5f,1);tab.anchoredPosition=new Vector2(tab.anchoredPosition.x,-5);}
            Canvas.ForceUpdateCanvases();
            foreach(var n in assets.Data["nodes"])if(nodes.TryGetValue((string)n["id"],out var tr))ClassicMenuAssets.Layout(tr,n);
            Show(play);committed=true;foreach(var go in originals)go.SetActive(false);
            Plugin.Log.Info("Classic menu enabled for this game session: extracted 0.10.1 layout, Mods and modern modes.");
        }
        private void RefreshMods()
        {
            foreach(Transform child in mods.transform){child.gameObject.SetActive(false);Object.Destroy(child.gameObject);}
            assets.Button(mods.transform,"MOD SETTINGS",new Vector2(0,24),new Vector2(65,9),()=>ClassicMenuSession.OpenModSettings?.Invoke());
            var entries=(ClassicMenuSession.ModButtons?.Invoke()??Enumerable.Empty<BeatSaberMarkupLanguage.MenuButtons.MenuButton>()).ToArray();
            page=Math.Max(0,Math.Min(page,Math.Max(0,(entries.Length-1)/4)));
            int row=0;foreach(var entry in entries.Skip(page*4).Take(4))
            {var b=assets.Button(mods.transform,entry.Text,new Vector2(0,12-row++*10),new Vector2(65,9),()=>{if(entry.Interactable)entry.ButtonClicked();});b.interactable=entry.Interactable;}
            if(entries.Length>4){assets.Button(mods.transform,"<",new Vector2(-20,-32),new Vector2(16,7),()=>{page--;RefreshMods();});assets.Button(mods.transform,">",new Vector2(20,-32),new Vector2(16,7),()=>{page++;RefreshMods();});}
        }
        private void Show(GameObject selected){play.SetActive(selected==play);mods.SetActive(selected==mods);more.SetActive(selected==more);news.SetActive(selected==news);}
        private void LateUpdate(){if(!committed)return;
            var parent=transform as RectTransform;
            if(parent && root)root.transform.localScale=Vector3.one*ClassicMenuLayout.Fit(parent.rect.width,parent.rect.height);
            foreach(var o in originals)if(o)o.SetActive(false);foreach(var p in links)if(p.Key && p.Value)p.Key.interactable=p.Value.interactable;}
        private void OnDestroy()
        {
            if(root){root.SetActive(false);Object.Destroy(root);}
            if(committed)for(int i=0;i<originals.Length;i++)if(originals[i])originals[i].SetActive(active[i]);
            foreach(var pair in layouts)if(pair.Key)pair.Key.enabled=pair.Value;
            assets?.Dispose();
        }
    }
}
