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
    internal static class ClassicObstacles
    {
        internal static JObject Profile;
        private static bool warnedDistortions;
        public static void GraphicsPostfix(ObstacleMaterialSetter __instance,bool screenDisplacementEffects)
        {
            var state=__instance.GetComponent<ClassicObstacleGraphics>();
            if(!state)state=__instance.gameObject.AddComponent<ClassicObstacleGraphics>();
            state.Distortions=screenDisplacementEffects;
            if(Plugin.Config.OldObstacles && !screenDisplacementEffects && !warnedDistortions)
            {warnedDistortions=true;Plugin.Log.Warn("Old Obstacles requires Distortions in graphics settings; using native obstacles.");}
        }
        public static void SizePostfix(StretchableObstacle __instance,float width,float height,float length)
        {
            var controller=__instance.GetComponentInParent<ObstacleController>();
            if(!controller)return;
            var setter=Fields.Get<ObstacleMaterialSetter>(controller,"_obstacleMaterialSetter");
            var graphics=setter.GetComponent<ClassicObstacleGraphics>();
            var state=__instance.GetComponent<ClassicObstacleState>();
            if(!Plugin.Active || !Plugin.Config.OldObstacles || !graphics || !graphics.Distortions)
            {if(state)state.Restore();return;}
            try
            {
                if(!state)state=__instance.gameObject.AddComponent<ClassicObstacleState>();
                if(!state.Built)state.Build(__instance,setter);
                state.Resize(width,height,length);
            }
            catch(Exception e)
            {
                if(state){state.Restore();Object.Destroy(state);}
                Plugin.Log.Error("Classic obstacles failed; native obstacle retained. "+e);
            }
        }
        internal static void Restore()
        {
            foreach(var state in Resources.FindObjectsOfTypeAll<ClassicObstacleState>())
            {state.Restore();Object.Destroy(state);}
            foreach(var state in Resources.FindObjectsOfTypeAll<ClassicObstacleGraphics>())Object.Destroy(state);
        }
    }
    public sealed class ClassicObstacleGraphics:MonoBehaviour {internal bool Distortions;}
    public sealed class ClassicObstacleState:MonoBehaviour
    {
        private GameObject root;
        private ClassicObstacleGeometry geometry;
        private Renderer source;
        private Renderer[] originals;
        private bool[] originalEnabled;
        private readonly List<Renderer> cores=new List<Renderer>(),frames=new List<Renderer>();
        private readonly List<Object> owned=new List<Object>();
        private readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        internal bool Built=>root;
        private static Vector3 V(JToken t)=>new Vector3((float)t["x"],(float)t["y"],(float)t["z"]);
        internal void Build(StretchableObstacle obstacle,ObstacleMaterialSetter setter)
        {
            if(ClassicObstacles.Profile==null)
                using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.ClassicObstacles.json"))
                using(var r=new StreamReader(s))ClassicObstacles.Profile=JObject.Parse(r.ReadToEnd());
            var data=ClassicObstacles.Profile;
            source=Fields.Get<Renderer>(setter,"_obstacleCoreRenderer");
            var coreMaterial=Fields.Get<Material>(setter,"_hwCoreMaterial");
            var frameShader=ObstacleMaterialResolver.Frame(Shader.Find("Custom/GlowingInstancedHD"),Resources.FindObjectsOfTypeAll<Material>());
            var frameController=Fields.Get<ParametricBoxFrameController>(obstacle,"_obstacleFrame");
            var nativeFrame=frameController?Fields.Get<MeshRenderer>(frameController,"_meshRenderer"):null;
            if(!source)throw new InvalidOperationException("Obstacle core renderer is missing");
            if(!coreMaterial || !coreMaterial.shader || !coreMaterial.shader.isSupported)
                throw new InvalidOperationException("Obstacle distortion material is missing or unsupported");
            if(!frameShader)throw new InvalidOperationException("No supported loaded Custom/GlowingInstancedHD shader for obstacle edges");
            // The renderer is a serialized reference, not necessarily on the controller's GameObject.
            // Its absence must not prevent the extracted frame from loading.
            int frameQueue=nativeFrame && nativeFrame.sharedMaterial?nativeFrame.sharedMaterial.renderQueue:2003;
            originals=obstacle.GetComponentsInChildren<Renderer>(true);originalEnabled=originals.Select(r=>r.enabled).ToArray();
            root=new GameObject("OldSaber.Obstacle.0.10.1");root.transform.SetParent(obstacle.transform,false);
            var nodes=new Dictionary<string,Transform>();var meshes=new Dictionary<string,Mesh>();var materials=new Dictionary<string,Material>();
            foreach(var entry in (JObject)data["textures"])
            {
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);owned.Add(texture);
                if(!ImageConversion.LoadImage(texture,Convert.FromBase64String((string)entry.Value)))throw new InvalidDataException("Obstacle texture");
                texture.wrapMode=TextureWrapMode.Repeat;
                texture.name=entry.Key;
            }
            foreach(var entry in (JObject)data["materials"])
            {
                var d=entry.Value;string name=(string)d["name"];
                if(name=="ShadowCasterOnly")continue;
                bool core=name.StartsWith("ObstacleCore");
                var material=core?new Material(coreMaterial):new Material(frameShader);owned.Add(material);materials[entry.Key]=material;
                material.name="OldSaber."+name;
                // Keep modern render-state and distortion pass settings; original shaders
                // were compiled for Unity 2017 and cannot be loaded directly in 1.40.8.
                if(core)
                {
                    foreach(var tex in (JObject)d["textures"])
                    {
                        if(!material.HasProperty(tex.Key))continue;
                        material.SetTexture(tex.Key,owned.OfType<Texture2D>().First(t=>t.name==(string)tex.Value["id"]));
                        var scale=tex.Value["scale"];material.SetTextureScale(tex.Key,new Vector2((float)scale["x"],(float)scale["y"]));
                    }
                    // ScreenDisplacementHD already shades both sides. Its original
                    // culling/render state must not be split into two overlapping passes.
                    if(material.HasProperty("_CullMode"))material.SetFloat("_CullMode",0);
                    material.DisableKeyword("SCALE_UV");
                    if(material.HasProperty("_ScaleUV"))material.SetFloat("_ScaleUV",0);
                }
                else
                {
                    // The native frame shader expects parametric vertex data; the
                    // extracted cubes need the regular mesh glow shader instead.
                    material.shader=frameShader;
                    material.shaderKeywords=new string[0];
                    material.EnableKeyword("_ENABLE_CUTOUT");
                    foreach(string p in new[]{"_BloomFactor","_CutoutTexScale","_EnableCutout"})
                        if(material.HasProperty(p) && d["floats"][p]!=null)material.SetFloat(p,(float)d["floats"][p]);
                    if(material.HasProperty("_EnableColorInstancing"))material.SetFloat("_EnableColorInstancing",1);
                    material.EnableKeyword("_ENABLE_COLOR_INSTANCING");
                    // Solid legacy edges, not additive note/sprite blending.
                    foreach(string p in new[]{"_BlendSrcFactor","_BlendSrcFactorA","_ZWrite"})if(material.HasProperty(p))material.SetFloat(p,1);
                    foreach(string p in new[]{"_BlendDstFactor","_BlendDstFactorA"})if(material.HasProperty(p))material.SetFloat(p,0);
                    material.renderQueue=frameQueue;
                }
            }
            foreach(var n in data["nodes"])
            {
                var go=new GameObject((string)n["name"]);go.layer=source.gameObject.layer;
                // Initialize procedural mesh on an active object before parenting into a pooled obstacle.
                bool cube=(bool?)n["proceduralCube"]==true;
                if(cube){go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>();go.AddComponent<ClassicObstacleCube>();}
                var tr=go.transform;tr.SetParent(n["parent"].Type==JTokenType.Null?root.transform:nodes[(string)n["parent"]],false);
                nodes[(string)n["id"]]=tr;tr.localPosition=V(n["position"]);tr.localScale=V(n["scale"]);
                var q=n["rotation"];tr.localRotation=new Quaternion((float)q["x"],(float)q["y"],(float)q["z"],(float)q["w"]);
                if(n["materials"]!=null && (string)n["name"]!="DepthWrite")
                {
                    if(!cube)
                    {
                        string key=(string)n["mesh"];
                        if(!meshes.TryGetValue(key,out var mesh)){mesh=ClassicStage.ReadMesh(data["meshes"][key],0,0);meshes[key]=mesh;owned.Add(mesh);}
                        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>();
                    }
                    var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=materials[(string)n["materials"][0]];
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                    if((string)n["name"]=="CoreInside")renderer.enabled=false;
                    else (cube?cores:frames).Add(renderer);
                }
                go.SetActive((bool)n["active"]);
            }
            geometry=root.AddComponent<ClassicObstacleGeometry>();
            foreach(var binding in (JObject)data["bindings"])
            {
                var tr=nodes[(string)binding.Value];
                Fields.Set(geometry,binding.Key,binding.Key=="_stretchableCoreOutside" || binding.Key=="_stretchableCoreInside"?(object)tr.GetComponent<ClassicObstacleCube>():tr);
            }
            Plugin.Log.Info("Classic obstacle model loaded: extracted geometry; frame="+frameShader.name+", core="+coreMaterial.shader.name);
        }
        internal void Resize(float width,float height,float length)
        {
            geometry.SetSize(width,height,length);
            foreach(var r in originals)if(r)r.enabled=false;
            LateUpdate();
        }
        private void LateUpdate()
        {
            if(!root || !source)return;
            if(!Plugin.Active || !Plugin.Config.OldObstacles){Restore();return;}
            // Copy native color, UV motion and cutout so pooled obstacles and dissolves stay synchronized.
            source.GetPropertyBlock(block);
            // The extracted StretchableCube already calculates world-scaled UVs.
            block.SetVector("_UVScale",Vector4.one);
            foreach(var r in cores)if(r)r.SetPropertyBlock(block);
            foreach(var r in frames)if(r)r.SetPropertyBlock(block);
            foreach(var r in originals)if(r)r.enabled=false;
        }
        internal void Restore()
        {
            if(root){root.SetActive(false);Object.Destroy(root);root=null;}
            if(originals!=null)for(int i=0;i<originals.Length;i++)if(originals[i])originals[i].enabled=originalEnabled[i];
            originals=null;cores.Clear();frames.Clear();
            foreach(var o in owned)if(o)Object.Destroy(o);owned.Clear();
        }
        private void OnDestroy(){Restore();}
    }
}
