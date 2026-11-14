using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using Object=UnityEngine.Object;
namespace OldSaber
{
    public sealed class ClassicMenuEnvironment:MonoBehaviour
    {
        private GameObject root;
        private Renderer[] originals;
        private bool[] enabledStates;
        private BloomPrePassLight[] lights;
        private bool[] lightStates;
        private readonly List<Object> owned=new List<Object>();
        private ClassicMenuAssets assets;
        internal void Build(TMP_FontAsset fallback)
        {
            assets=new ClassicMenuAssets(fallback);
            JObject data;
            using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.ClassicMenuEnvironment.json"))
            using(var r=new StreamReader(s))data=JObject.Parse(r.ReadToEnd());
            originals=GetComponentsInChildren<Renderer>(true);enabledStates=originals.Select(r=>r.enabled).ToArray();
            lights=GetComponentsInChildren<BloomPrePassLight>(true);lightStates=lights.Select(l=>l.enabled).ToArray();
            var templates=Resources.FindObjectsOfTypeAll<TubeBloomPrePassLight>().Where(t=>t.gameObject.scene==gameObject.scene).ToArray();
            var tubeTemplate=templates.FirstOrDefault(t=>Fields.Get<BloomPrePassLightTypeSO>(t,"_lightType") && Fields.Get<ParametricBoxController>(t,"_parametricBoxController"));
            if(!tubeTemplate)throw new InvalidOperationException("Menu bloom tube template not ready");
            var box=Fields.Get<ParametricBoxController>(tubeTemplate,"_parametricBoxController");
            var mats=originals.SelectMany(r=>r.sharedMaterials).Where(m=>m).ToArray();
            var surface=mats.FirstOrDefault(m=>m.name.StartsWith("DarkEnvironment"))??mats.FirstOrDefault(m=>m.shader.name=="Custom/SimpleLit");
            if(!surface)throw new InvalidOperationException("Menu surface material not ready");
            var material=new Material(surface);owned.Add(material);
            if(material.HasProperty("_Color"))material.SetColor("_Color",new Color(.025f,.025f,.025f,1));
            root=new GameObject("OldSaber.MenuEnvironment.0.10.1");root.SetActive(false);root.transform.SetParent(transform,false);
            var nodes=new Dictionary<string,Transform>();var meshes=new Dictionary<string,Mesh>();
            var textNodes=assets.Data["nodes"].Where(n=>n["text"]!=null).ToDictionary(n=>(string)n["id"]);
            foreach(var n in data["nodes"])
            {
                string id=(string)n["id"];textNodes.TryGetValue(id,out var textNode);
                var go=ClassicGuiNodeFactory.Create((string)n["name"],textNode!=null,textNode!=null,false,out var text);
                int layer=LayerMask.NameToLayer("Environment");go.layer=layer<0?0:layer;
                var tr=go.transform;string parent=(string)n["parent"];tr.SetParent(parent.Length==0?root.transform:nodes[parent],false);nodes[id]=tr;
                tr.localPosition=V(n["position"]);tr.localScale=V(n["scale"]);var q=n["rotation"];tr.localRotation=new Quaternion((float)q[0],(float)q[1],(float)q[2],(float)q[3]);
                if(text)
                {
                    var d=textNode["text"];text.font=assets.Fonts.Font((string)d["font"]);text.fontSharedMaterial=text.font.material;
                    text.text=(string)d["m_text"];text.fontSize=(float)d["m_fontSize"];text.color=ClassicGuiState.ColorValue(d["m_fontColor"]);
                    text.autoSizeTextContainer=false;text.enableAutoSizing=false;text.enableWordWrapping=false;text.alignment=(TextAlignmentOptions)(int)d["m_textAlignment"];
                    ClassicMenuAssets.Layout(tr,textNode);
                }
                else if(n["beam"]!=null)
                {
                    var b=n["beam"];var tube=go.AddComponent<TubeBloomPrePassLight>();
                    Fields.Set(tube,"_lightType",Fields.Get<BloomPrePassLightTypeSO>(tubeTemplate,"_lightType"));
                    Fields.Set(tube,"_mainEffectPostProcessEnabled",Fields.Get<BoolSO>(tubeTemplate,"_mainEffectPostProcessEnabled"));
                    Fields.Set(tube,"_center",(float)b["_center"]);tube.width=(float)b["_width"];tube.length=(float)b["_length"];
                    LaserVisuals.Create(tr,tube,box,null,-1);tube.color=ClassicGuiState.ColorValue(b["_color"]);
                }
                else if(n["mesh"]!=null)
                {
                    int first=(int)n["batch"]["firstSubMesh"],count=(int)n["batch"]["subMeshCount"];string key=(string)n["mesh"]+":"+first+":"+count;
                    if(!meshes.TryGetValue(key,out var mesh)){mesh=ClassicStage.ReadMesh(data["meshes"][(string)n["mesh"]],first,count);meshes[key]=mesh;owned.Add(mesh);}
                    var visual=new GameObject("ClassicMesh");visual.layer=go.layer;string batchRoot=(string)n["batchRoot"];
                    visual.transform.SetParent(count>0 && batchRoot.Length>0?nodes[batchRoot]:tr,false);
                    visual.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterials=n["materials"].Select(_=>material).ToArray();
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;visual.SetActive((bool)n["active"]);
                }
                go.SetActive((bool)n["active"] && (string)n["name"]!="BuildTypeName");
            }
            root.SetActive(true);
            foreach(var r in originals)if(r)r.enabled=false;
            foreach(var l in lights)if(l)l.enabled=false;
            Plugin.Log.Info("Classic menu environment loaded: original architecture, neon tubes and Beon logo.");
        }
        private static Vector3 V(JToken t)=>new Vector3((float)t[0],(float)t[1],(float)t[2]);
        private void LateUpdate()
        {
            if(!root)return;
            foreach(var r in originals)if(r)r.enabled=false;
            foreach(var l in lights)if(l)l.enabled=false;
        }
        private void OnDestroy()
        {
            if(root){root.SetActive(false);Object.Destroy(root);}
            if(originals!=null)for(int i=0;i<originals.Length;i++)if(originals[i])originals[i].enabled=enabledStates[i];
            if(lights!=null)for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=lightStates[i];
            foreach(var o in owned)if(o)Object.Destroy(o);assets?.Dispose();
        }
    }
}
