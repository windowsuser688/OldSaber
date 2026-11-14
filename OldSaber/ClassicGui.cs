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
    public sealed class ClassicGuiHost : MonoBehaviour
    {
        private float nextScan;
        private readonly HashSet<int> failed=new HashSet<int>();
        private void Update()
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.5f;
            if(!Plugin.Active || !Plugin.Config.OldGUI){RestoreAll();failed.Clear();return;}
            foreach(var hud in Resources.FindObjectsOfTypeAll<CoreGameHUDController>())
            {
                if(!hud || !hud.gameObject.scene.IsValid() || !hud.gameObject.activeInHierarchy || hud.GetComponent<ClassicGuiState>() || failed.Contains(hud.GetInstanceID()))continue;
                var state=hud.gameObject.AddComponent<ClassicGuiState>();
                try{state.Build(hud);}
                catch(Exception e){state.Restore();Object.Destroy(state);failed.Add(hud.GetInstanceID());Plugin.Log.Error("Classic HUD failed; native HUD retained. "+e);}
            }
        }
        private static void RestoreAll()
        {
            foreach(var state in Resources.FindObjectsOfTypeAll<ClassicGuiState>()){state.Restore();Object.Destroy(state);}
        }
        private void OnDestroy(){RestoreAll();}
    }
    public sealed class ClassicGuiState : MonoBehaviour
    {
        private GameObject root,energyPanel;
        private Graphic[] nativeGraphics;
        private bool[] nativeEnabled;
        private Renderer[] nativeRenderers;
        private bool[] rendererEnabled;
        private TMP_Text scoreSource,comboSource,multiplierSource;
        private UImage energySource,progressSource;
        private GameEnergyUIPanel nativeEnergyPanel;
        private IGameEnergyCounter energyCounter;
        private readonly List<Object> owned=new List<Object>();
        private readonly List<KeyValuePair<TMP_Text,TMP_Text>> texts=new List<KeyValuePair<TMP_Text,TMP_Text>>();
        private UImage energyTarget,progressTarget;
        private bool committed;
        private readonly List<ClassicGuiAnchor> anchors=new List<ClassicGuiAnchor>();
        private static Vector3 V(JToken t)=>new Vector3((float)t["x"],(float)t["y"],(float)t["z"]);
        private static Vector2 V2(JToken t)=>new Vector2((float)t["x"],(float)t["y"]);
        internal static Color ColorValue(JToken t)=>new Color((float)t["r"],(float)t["g"],(float)t["b"],(float)t["a"]);
        internal void Build(CoreGameHUDController hud)
        {
            var score=hud.GetComponentInChildren<ScoreUIController>(true);
            var combo=hud.GetComponentInChildren<ComboUIController>(true);
            var multiplier=hud.GetComponentInChildren<ScoreMultiplierUIController>(true);
            nativeEnergyPanel=hud.GetComponentInChildren<GameEnergyUIPanel>(true);
            if(!score || !combo || !multiplier || !nativeEnergyPanel)throw new InvalidOperationException("HUD counters unavailable");
            scoreSource=Fields.Get<TextMeshProUGUI>(score,"_scoreText");comboSource=Fields.Get<TextMeshProUGUI>(combo,"_comboText");
            multiplierSource=Fields.Get<TextMeshProUGUI[]>(multiplier,"_multiplierTexts").First();
            energySource=Fields.Get<UImage>(nativeEnergyPanel,"_energyBar");progressSource=Fields.Get<UImage>(multiplier,"_multiplierProgressImage");
            energyCounter=Fields.Get<IGameEnergyCounter>(nativeEnergyPanel,"_gameEnergyCounter");
            nativeGraphics=hud.GetComponentsInChildren<Graphic>(true);nativeEnabled=nativeGraphics.Select(x=>x.enabled).ToArray();
            nativeRenderers=hud.GetComponentsInChildren<Renderer>(true);rendererEnabled=nativeRenderers.Select(x=>x.enabled).ToArray();
            JObject data;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.ClassicGUI.json"))
            using(var reader=new StreamReader(stream))data=JObject.Parse(reader.ReadToEnd());
            var fonts=new ClassicGuiFonts(data,scoreSource.font,owned);
            root=new GameObject("OldSaber.ClassicHUD");root.SetActive(false);root.transform.SetParent(hud.transform,false);
            var nodes=new Dictionary<string,Transform>();var counters=new Dictionary<string,TMP_Text>();var images=new Dictionary<string,UImage>();
            foreach(var n in data["nodes"])
            {
                var go=ClassicGuiNodeFactory.Create((string)n["name"],(bool)n["rect"],n["text"]!=null,
                    n["text"]!=null && (bool)n["text"]["ui"],out var preparedText);
                go.layer=scoreSource.gameObject.layer;
                var tr=go.transform;tr.SetParent(n["parent"].Type==JTokenType.Null?root.transform:nodes[(string)n["parent"]],false);nodes[(string)n["id"]]=tr;
                if(n["canvas"]!=null)
                {
                    var canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
                    canvas.sortingOrder=(int)n["canvas"]["m_SortingOrder"];
                    canvas.additionalShaderChannels=(AdditionalCanvasShaderChannels)(int)n["canvas"]["m_AdditionalShaderChannelsFlag"];
                }
                if(n["text"]!=null)
                {
                    var d=n["text"];TMP_Text text=preparedText;
                    text.font=fonts.Font((string)d["font"]);text.fontSharedMaterial=text.font.material;
                    text.text=(string)d["m_text"];text.fontSize=(float)d["m_fontSize"];text.enableAutoSizing=false;
                    text.color=Color.white;text.enableVertexGradient=false;text.fontStyle=(FontStyles)(int)d["m_fontStyle"];text.alignment=(TextAlignmentOptions)(int)d["m_textAlignment"];
                    var margin=d["m_margin"];text.margin=new Vector4((float)margin["x"],(float)margin["y"],(float)margin["z"],(float)margin["w"]);
                    text.autoSizeTextContainer=false;text.raycastTarget=false;text.enableWordWrapping=false;counters[(string)d["id"]]=text;
                }
                if(n["image"]!=null)
                {
                    var d=n["image"];var image=go.AddComponent<UImage>();image.raycastTarget=false;image.color=ColorValue(d["m_Color"]);
                    if(d["texture"]!=null)
                    {
                        var texture=fonts.Texture((string)d["texture"]);var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);owned.Add(sprite);image.sprite=sprite;
                    }
                    image.type=(UImage.Type)(d["m_Type"]==null?0:(int)d["m_Type"]);
                    if(d["m_FillMethod"]!=null){image.fillMethod=(UImage.FillMethod)(int)d["m_FillMethod"];image.fillOrigin=(int)d["m_FillOrigin"];image.fillClockwise=(bool)d["m_FillClockwise"];image.fillAmount=(float)d["m_FillAmount"];}
                    images[(string)d["id"]]=image;
                }
                ApplyLayout(tr,n);
                go.SetActive(ClassicGuiLayout.StaticVisible((string)n["name"],(bool)n["active"]));
            }
            var bindings=data["bindings"];
            Bind(bindings["score"],scoreSource,counters);Bind(bindings["combo"],comboSource,counters);Bind(bindings["multiplier"],multiplierSource,counters);
            energyTarget=images[(string)bindings["energy"]];progressTarget=images[(string)bindings["progress"]];energyPanel=nodes[(string)bindings["energyPanel"]].gameObject;
            committed=true;
            bool battery=energyCounter!=null && energyCounter.energyType==GameplayModifiers.EnergyType.Battery;
            foreach(var graphic in nativeGraphics)
                if(!battery || !graphic.transform.IsChildOf(nativeEnergyPanel.transform))graphic.enabled=false;
            foreach(var renderer in nativeRenderers)
                if(!battery || !renderer.transform.IsChildOf(nativeEnergyPanel.transform))renderer.enabled=false;
            root.SetActive(true);
            Canvas.ForceUpdateCanvases();
            // Canvas/TMP initialization can resize a RectTransform. Restore the
            // serialized layout after activation, in parent-before-child order.
            foreach(var n in data["nodes"])ApplyLayout(nodes[(string)n["id"]],n);
            Canvas.ForceUpdateCanvases();
            // Keep FrontPanel at its extracted floor position and original size.
            // The score belongs on the standing platform, not at the modern score anchor.
            // Only combo and multiplier follow current counter anchors.
            AddTextAnchor("ComboPanel",bindings["combo"].First(),comboSource,data,nodes,counters);
            AddTextAnchor("MultiplierPanel",bindings["multiplier"].Last(),multiplierSource,data,nodes,counters);
            // EnergyPanel keeps its extracted position, rotation and size.
            // Following the modern energy anchor moves the classic bar out of place.
            LateUpdate();
            Plugin.Log.Info("Classic gameplay HUD enabled: extracted Beon/Teko fonts, score, combo, multiplier and energy.");
        }
        private static void ApplyLayout(Transform tr,JToken node)
        {
            var t=node["transform"];
            if(tr is RectTransform rt)
                ClassicGuiLayout.Rect(rt,V2(t["m_AnchorMin"]),V2(t["m_AnchorMax"]),V2(t["m_Pivot"]),V2(t["m_SizeDelta"]),V2(t["m_AnchoredPosition"]),(float)t["m_LocalPosition"]["z"]);
            else tr.localPosition=V(t["m_LocalPosition"]);
            tr.localScale=V(t["m_LocalScale"]);
            var q=t["m_LocalRotation"];tr.localRotation=new Quaternion((float)q["x"],(float)q["y"],(float)q["z"],(float)q["w"]);
        }
        private void AddTextAnchor(string panel,JToken id,TMP_Text source,JObject data,Dictionary<string,Transform> nodes,Dictionary<string,TMP_Text> counters)
        {
            var node=data["nodes"].First(n=>(string)n["name"]==panel);
            var text=counters[(string)id];
            anchors.Add(new ClassicGuiAnchor(nodes[(string)node["id"]],text.rectTransform,source.rectTransform,text,source));
        }
        private void Bind(JToken ids,TMP_Text source,Dictionary<string,TMP_Text> targets)
        {foreach(var id in ids)texts.Add(new KeyValuePair<TMP_Text,TMP_Text>(targets[(string)id],source));}
        private void LateUpdate()
        {
            if(!committed)return;
            // Native combo animations can re-enable the full-combo indicator after Build.
            // Keep native visuals hidden while still letting their controllers update values.
            bool battery=energyCounter!=null && energyCounter.energyType==GameplayModifiers.EnergyType.Battery;
            foreach(var graphic in nativeGraphics)if(graphic && (!battery || !graphic.transform.IsChildOf(nativeEnergyPanel.transform)))graphic.enabled=false;
            foreach(var renderer in nativeRenderers)if(renderer && (!battery || !renderer.transform.IsChildOf(nativeEnergyPanel.transform)))renderer.enabled=false;
            foreach(var anchor in anchors)anchor.Update();
            foreach(var pair in texts)if(pair.Key && pair.Value && pair.Key.text!=pair.Value.text)pair.Key.text=pair.Value.text;
            if(progressSource && progressTarget)progressTarget.fillAmount=progressSource.fillAmount;
            if(energyTarget && energyCounter!=null)energyTarget.fillAmount=Mathf.Clamp01(energyCounter.energy);
            // Preserve Hide HUD, No Fail and battery-energy visibility from native controllers.
            if(energyPanel && nativeEnergyPanel)energyPanel.SetActive(nativeEnergyPanel.gameObject.activeInHierarchy && (energyCounter==null || energyCounter.energyType!=GameplayModifiers.EnergyType.Battery));
        }
        internal void Restore()
        {
            if(root){root.SetActive(false);Object.Destroy(root);root=null;}
            if(committed)
            {
                for(int i=0;i<nativeGraphics.Length;i++)if(nativeGraphics[i])nativeGraphics[i].enabled=nativeEnabled[i];
                for(int i=0;i<nativeRenderers.Length;i++)if(nativeRenderers[i])nativeRenderers[i].enabled=rendererEnabled[i];
                committed=false;
            }
            foreach(var item in owned)if(item)Object.Destroy(item);owned.Clear();texts.Clear();anchors.Clear();
        }
        private void OnDestroy(){Restore();}
    }
}
