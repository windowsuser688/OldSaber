using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace OldSaber
{
    internal static class ClassicSabers
    {
        private static JObject data;
        internal static JObject Data
        {
            get
            {
                if(data==null)
                    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.ClassicSabers.json"))
                    using(var reader=new StreamReader(stream))data=JObject.Parse(reader.ReadToEnd());
                return data;
            }
        }
        public static void Postfix(SaberModelController __instance,Saber saber)
        {
            if(!Plugin.Active || !Plugin.Config.OldSabers)return;
            if(__instance.GetComponent<ClassicSaberState>())return;
            var state=__instance.gameObject.AddComponent<ClassicSaberState>();
            try {state.Build(__instance,saber);}
            catch(Exception e){state.Restore();Object.Destroy(state);Plugin.Log.Error("Classic sabers failed; native model retained. "+e);}
        }
        internal static void Restore()
        {
            foreach(var state in Resources.FindObjectsOfTypeAll<ClassicSaberState>())
            {state.Restore();Object.Destroy(state);}
        }
    }
    public sealed class ClassicSaberState : MonoBehaviour
    {
        private GameObject root;
        private Renderer[] originals;
        private bool[] originalEnabled;
        private SaberTrail nativeTrail;
        private bool nativeTrailEnabled;
        private TubeBloomPrePassLight nativeLight;
        private bool nativeLightEnabled;
        private readonly List<Object> owned=new List<Object>();
        private bool committed;
        private Color saberColor;
        private static Vector3 V(JToken t)=>new Vector3((float)t[0],(float)t[1],(float)t[2]);
        private static Color C(JToken t)=>new Color((float)t["r"],(float)t["g"],(float)t["b"],(float)t["a"]);
        internal void Build(SaberModelController model,Saber saber)
        {
            var data=ClassicSabers.Data;
            saberColor=Fields.Get<ColorManager>(model,"_colorManager").ColorForSaberType(saber.saberType);
            var source=data["sabers"].First(s=>(int)s["type"]==(int)saber.saberType);
            originals=model.GetComponentsInChildren<Renderer>(true);
            originalEnabled=originals.Select(r=>r.enabled).ToArray();
            nativeTrail=Fields.Get<SaberTrail>(model,"_saberTrail");
            nativeTrailEnabled=nativeTrail && nativeTrail.enabled;
            nativeLight=Fields.Get<TubeBloomPrePassLight>(model,"_saberLight");
            nativeLightEnabled=nativeLight && nativeLight.enabled;
            root=new GameObject("OldSaber.ClassicSaber");root.SetActive(false);
            root.transform.SetParent(model.transform,false);root.layer=model.gameObject.layer;
            var nodes=new Dictionary<string,Transform>();
            var meshes=new Dictionary<string,Mesh>();
            var materials=new Dictionary<string,Material>();
            foreach(var n in source["nodes"])
            {
                var go=new GameObject((string)n["name"]);go.layer=root.layer;
                var tr=go.transform;tr.SetParent(n["parent"].Type==JTokenType.Null?root.transform:nodes[(string)n["parent"]],false);
                tr.localPosition=V(n["position"]);tr.localScale=V(n["scale"]);
                var q=n["rotation"];tr.localRotation=new Quaternion((float)q[0],(float)q[1],(float)q[2],(float)q[3]);
                nodes[(string)n["id"]]=tr;
                if(n["mesh"]==null || !SaberMaterialResolver.RendersPart((string)n["name"]))continue;
                string key=(string)n["mesh"];
                if(!meshes.TryGetValue(key,out var mesh)){mesh=ClassicStage.ReadMesh(data["meshes"][key],0,0);owned.Add(mesh);meshes[key]=mesh;}
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();
                if(SaberMaterialResolver.UsesHandleMaterial((string)n["name"]))
                {
                    string handleKey=((JObject)data["materials"]).Properties().First(p=>(string)p.Value["name"]=="Handle").Name;
                    renderer.sharedMaterial=GetMaterial(handleKey,data,materials,false);
                }
                else renderer.sharedMaterials=n["materials"].Select(m=>GetMaterial((string)m,data,materials,false)).ToArray();
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
            var trail=source["trail"];
            var trailMaterial=GetMaterial((string)trail["rendererMaterial"],data,materials,true);
            // Keep 65% of the archived motion history: slightly longer than the half-length trail.
            int trailFrames=Math.Max(4,(int)Math.Round((int)trail["MaxFrame"]*.65));
            root.AddComponent<LegacyTrail.XWeaponTrail>().Initialize(nodes[(string)trail["start"]],nodes[(string)trail["end"]],
                trailFrames,(int)trail["Granularity"],(int)trail["_skipFirstFrames"],SaberTrailAppearance.TrailColor(saberColor,
                    SaberTrailAppearance.IsAlphaBlended(trailMaterial.shader.name),(float)trail["MyColor"]["a"]),trailMaterial);
            // Only hide native visuals once every replacement asset has been prepared.
            committed=true;
            foreach(var renderer in originals)renderer.enabled=false;
            if(nativeTrail)nativeTrail.enabled=false;
            if(nativeLight)nativeLight.enabled=false;
            root.SetActive(true);
            Plugin.Log.Info("Classic saber and extracted trail enabled for "+saber.saberType);
        }
        private Material GetMaterial(string key,JObject data,Dictionary<string,Material> cache,bool trail)
        {
            if(cache.TryGetValue(key,out var existing))return existing;
            var profile=data["materials"][key];string shaderName=(string)profile["shader"];
            var shader=Shader.Find(shaderName);
            if(!shader && trail)shader=Shader.Find("Legacy Shaders/Particles/Additive");
            if(!shader && trail)shader=Shader.Find("Mobile/Particles/Additive");
            if(!shader && trail)shader=Shader.Find("Particles/Standard Unlit");
            if(!shader && trail)shader=Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            Material template=null;
            bool isHandle=(string)profile["name"]=="Handle";
            // Use a plain, known color material instead of inheriting a native
            // handle's tinted texture or the blade's instanced glow settings.
            if(isHandle && OldNotesAssets.Data){template=OldNotesAssets.Data.NoteMaterial;shader=null;}
            if(!shader && !trail && !template)
            {
                bool handle=(string)profile["name"]=="Handle";
                template=SaberMaterialResolver.Select(originals.Where(r=>r).SelectMany(r=>r.sharedMaterials).ToArray(),handle);
                if(!template && OldNotesAssets.Data)template=OldNotesAssets.Data.NoteMaterial;
                if(!template)shader=Shader.Find("Custom/SimpleLit");
            }
            if(!shader && !template)throw new InvalidOperationException("No compatible shader or loaded material for "+shaderName);
            var material=template?new Material(template):new Material(shader);
            material.name="OldSaber."+(string)profile["name"];owned.Add(material);cache[key]=material;
            Plugin.Log.Info("Classic saber material "+(string)profile["name"]+" uses "+material.shader.name+(template?" (loaded model fallback)":""));
            foreach(var p in ((JObject)profile["floats"]).Properties())if(material.HasProperty(p.Name))material.SetFloat(p.Name,(float)p.Value);
            foreach(var p in ((JObject)profile["colors"]).Properties())if(material.HasProperty(p.Name))material.SetColor(p.Name,C(p.Value));
            foreach(var p in ((JObject)profile["textures"]).Properties())
            {
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                owned.Add(texture);
                if(!ImageConversion.LoadImage(texture,Convert.FromBase64String((string)data["textures"][(string)p.Value])))
                    throw new InvalidDataException("Cannot decode classic trail texture");
                // The original RGB texture stores fade in brightness, not alpha.
                // Alpha blending must turn its black pixels transparent.
                if(trail && SaberTrailAppearance.IsAlphaBlended(material.shader.name))
                {
                    var pixels=texture.GetPixels32();
                    for(int i=0;i<pixels.Length;i++)pixels[i]=SaberTrailAppearance.ToAlphaMask(pixels[i]);
                    texture.SetPixels32(pixels);texture.Apply();
                }
                texture.wrapMode=TextureWrapMode.Clamp;
                if(material.HasProperty(p.Name))material.SetTexture(p.Name,texture);
            }
            if(!trail && material.HasProperty("_Color"))
            {
                var color=isHandle?material.GetColor("_Color"):saberColor;
                color.a=1;material.SetColor("_Color",color);
                if(!isHandle)
                    foreach(string property in new[]{"_TintColor","_EmissionColor"})
                        if(material.HasProperty(property))material.SetColor(property,color);
            }
            if(isHandle)
            {
                material.SetColor("_Color",new Color(.035f,.035f,.035f,1f));
                if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",Color.black);
                foreach(string property in new[]{"_Metallic","_Glossiness","_ColorInstancing","_EnableRimDim","_EnablePlaneCut","_EnableCutout"})
                    if(material.HasProperty(property))material.SetFloat(property,0);
                foreach(string keyword in new[]{"_EMISSION","_ENABLE_RIM_DIM","_ENABLE_PLANE_CUT","_ENABLE_CUTOUT"})material.DisableKeyword(keyword);
            }
            if(trail)
            {
                material.renderQueue=3000;
                if(material.HasProperty("_Mode"))material.SetFloat("_Mode",4);
                if(material.HasProperty("_SrcBlend"))material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if(material.HasProperty("_DstBlend"))material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.One);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_FADING_ON");
                if(material.HasProperty("_ZWrite"))material.SetInt("_ZWrite",0);
                if(material.HasProperty("_Cull"))material.SetInt("_Cull",0);
            }
            return material;
        }
        internal void Restore()
        {
            if(root){root.SetActive(false);Object.Destroy(root);root=null;}
            if(committed)
            {
                for(int i=0;i<originals.Length;i++)if(originals[i])originals[i].enabled=originalEnabled[i];
                if(nativeTrail)nativeTrail.enabled=nativeTrailEnabled;
                if(nativeLight)nativeLight.enabled=nativeLightEnabled;
                committed=false;
            }
            foreach(var asset in owned)if(asset)Object.Destroy(asset);
            owned.Clear();
        }
        private void OnDestroy(){Restore();}
    }
}
