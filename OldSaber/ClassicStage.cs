using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OldSaber
{
    [DefaultExecutionOrder(10000)]
    public sealed class ClassicStage : MonoBehaviour
    {
        private readonly Dictionary<string, Transform> nodes = new Dictionary<string, Transform>();
        private readonly List<Object> owned = new List<Object>();
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<BloomPrePassLight> hiddenLights = new List<BloomPrePassLight>();
        private readonly List<LightLink> links = new List<LightLink>();
        private readonly List<LaserMotion> lasers = new List<LaserMotion>();
        private readonly List<RingMotion> rings = new List<RingMotion>();
        private readonly List<SpectrumMotion> spectra = new List<SpectrumMotion>();
        private readonly float[] spectrum = new float[1024];
        private readonly SpectrumEnvelope spectrumEnvelope = new SpectrumEnvelope();
        private AudioSource songAudio;
        private float nextAudioLookup;
        private bool loggedSpectrum;
        private readonly System.Random random = new System.Random(101);
        private BeatmapCallbacksController callbacks;
        private BeatmapDataCallbackWrapper callback;
        private LightWithIdManager lightManager;
        private float previousTime;
        private bool committed;
        private Scene scene;
        private static Vector3 V(JToken t) { return new Vector3((float)t[0], (float)t[1], (float)t[2]); }
        private static Color C(JToken t) { return new Color((float)t["r"], (float)t["g"], (float)t["b"], (float)t["a"]); }

        internal void Build(Scene target, LightSwitchEventEffect[] effects)
        {
            scene = target;
            callbacks = Fields.Get<BeatmapCallbacksController>(effects[0], "_beatmapCallbacksController");
            lightManager = Fields.Get<LightWithIdManager>(effects[0], "_lightManager");
            var ids = effects.GroupBy(e => (int)e.eventType).ToDictionary(g => g.Key, g => g.First().lightsId);
            for (int i = 0; i < 5; i++) if (!ids.ContainsKey(i)) throw new InvalidOperationException("Missing basic light group " + i);
            var tubeTemplate = Resources.FindObjectsOfTypeAll<TubeBloomPrePassLight>()
                .FirstOrDefault(t => t.gameObject.scene == scene && Fields.Get<BloomPrePassLightTypeSO>(t, "_lightType") != null
                    && Fields.Get<ParametricBoxController>(t, "_parametricBoxController") != null);
            if (!tubeTemplate) throw new InvalidOperationException("No native bloom tube/core template in " + scene.name);
            var boxTemplate = Fields.Get<ParametricBoxController>(tubeTemplate, "_parametricBoxController");
            var glowTemplate = Resources.FindObjectsOfTypeAll<TubeBloomPrePassLight>()
                .FirstOrDefault(t => t.gameObject.scene == scene
                    && Fields.Get<Parametric3SliceSpriteController>(t, "_dynamic3SliceSprite") != null
                    && Fields.Get<Parametric3SliceSpriteController>(t, "_dynamic3SliceSprite").GetComponent<MeshRenderer>() != null
                    && Fields.Get<Parametric3SliceSpriteController>(t, "_dynamic3SliceSprite").GetComponent<MeshRenderer>().sharedMaterial != null);
            if (!glowTemplate) Plugin.Log.Warn("No native glow sprite template; side lasers use volumetric bloom only. Enable main effects to see them.");
            else Plugin.Log.Info("Side lasers use glow shader " + Fields.Get<Parametric3SliceSpriteController>(glowTemplate, "_dynamic3SliceSprite").GetComponent<MeshRenderer>().sharedMaterial.shader.name);

            var nativeMaterials = Resources.FindObjectsOfTypeAll<Renderer>()
                .Where(r => r.gameObject.scene == scene).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToArray();
            if (nativeMaterials.Length == 0) throw new InvalidOperationException("No environment materials.");
            var mirrorTemplates = ClassicMirrors.FindTemplates(scene);
            // Capture before creating our own objects; preserve native particle systems (smoke).
            hiddenRenderers.AddRange(Resources.FindObjectsOfTypeAll<Renderer>().Where(r =>
                r.gameObject.scene == scene && r.enabled && !(r is ParticleSystemRenderer) && !IsGameplay(r.transform)));
            hiddenLights.AddRange(Resources.FindObjectsOfTypeAll<BloomPrePassLight>().Where(l => l.gameObject.scene == scene && l.enabled && !IsGameplay(l.transform)));
            JObject data;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SceneCatalog.ResourceFor(scene.name)))
            using (var reader = new StreamReader(stream)) data = JObject.Parse(reader.ReadToEnd());
            if ((string)data["sceneName"] != scene.name) throw new InvalidOperationException("Classic scene resource mismatch.");
            var meshData = (JObject)data["meshes"];
            var meshCache = new Dictionary<string, Mesh>();
            var materialCache = new Dictionary<string, Material>();
            foreach (var n in data["nodes"])
            {
                var go = new GameObject((string)n["name"]);
                var tr = go.transform;
                string parent = (string)n["parent"];
                tr.SetParent(parent.Length == 0 ? transform : nodes[parent], false);
                tr.localPosition = V(n["position"]);
                var q = n["rotation"];
                tr.localRotation = new Quaternion((float)q[0], (float)q[1], (float)q[2], (float)q[3]);
                tr.localScale = V(n["scale"]);
                go.layer = LayerMask.NameToLayer("Environment") >= 0 ? LayerMask.NameToLayer("Environment") : 0;
                go.SetActive((bool)n["active"]);
                nodes.Add((string)n["id"], tr);
                if (n["beam"] != null)
                {
                    var b = n["beam"];
                    var tube = go.AddComponent<TubeBloomPrePassLight>();
                    Fields.Set(tube, "_lightType", Fields.Get<BloomPrePassLightTypeSO>(tubeTemplate, "_lightType"));
                    Fields.Set(tube, "_mainEffectPostProcessEnabled", Fields.Get<BoolSO>(tubeTemplate, "_mainEffectPostProcessEnabled"));
                    Fields.Set(tube, "_center", (float)b["_center"]);
                    tube.width = (float)b["_width"];
                    tube.length = (float)b["_length"];
                    int oldId = (int)b["_ID"];
                    LaserVisuals.Create(tr, tube, boxTemplate, glowTemplate, oldId);
                    Fields.Set(tube, "_disableRenderersOnZeroAlpha", true);
                    tube.color = C(b["_color"]);
                    if (oldId >= 1 && oldId <= 5) links.Add(new LightLink(ids[oldId - 1], tube));
                    else new LightLink(-1, tube).ColorWasSet(C(b["_color"]));
                }
                else if (n["mesh"] != null)
                {
                    string mk = (string)n["mesh"];
                    var batch = n["batch"];
                    int first = (int)batch["firstSubMesh"], count = (int)batch["subMeshCount"];
                    string cacheKey = mk + ":" + first + ":" + count;
                    Mesh mesh;
                    if (!meshCache.TryGetValue(cacheKey, out mesh))
                    {
                        mesh = ReadMesh(meshData[mk], first, count); mesh.name = "Classic." + mk;
                        meshCache.Add(cacheKey, mesh); owned.Add(mesh);
                    }
                    // Unity static batching stores vertices in batch-root coordinates.
                    var visual = new GameObject("ClassicMesh");
                    string batchRoot = (string)n["batchRoot"];
                    visual.transform.SetParent(count > 0 && batchRoot.Length > 0 ? nodes[batchRoot] : tr, false);
                    visual.layer = go.layer;
                    visual.SetActive(go.activeSelf);
                    visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = visual.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = n["materials"].Select(x => MaterialFor((string)x, nativeMaterials, materialCache)).ToArray();
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    if (n["materials"].Count() == 1)
                        ClassicMirrors.Attach(renderer, (string)n["materials"][0], mirrorTemplates, owned);
                }
                if (n["rotationEvent"] != null) lasers.Add(new LaserMotion(tr, (int)n["rotationEvent"], V(n["rotationAxis"])));
            }
            foreach (var r in data["rings"]) rings.Add(new RingMotion(r, nodes));
            foreach (var s in data["spectra"]) spectra.Add(new SpectrumMotion(s, nodes));
            ResolveSongAudio();
            previousTime = callbacks.songTime;
        }

        internal void Commit()
        {
            // Register before hiding anything so a setup failure leaves a usable environment.
            foreach (var link in links) lightManager.RegisterLight(link);
            callback = callbacks.AddBeatmapCallback<BasicBeatmapEventData>(HandleEvent);
            foreach (var r in hiddenRenderers) if (r) r.enabled = false;
            foreach (var l in hiddenLights) if (l) l.enabled = false;
            committed = true;
        }
        private static bool IsGameplay(Transform t)
        {
            while (t != null)
            {
                if (t.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                t = t.parent;
            }
            return false;
        }
        internal static Mesh ReadMesh(JToken d, int first, int count)
        {
            var m = new Mesh();
            m.indexFormat = IndexFormat.UInt32;
            m.vertices = d["vertices"].Select(V).ToArray();
            var normals = d["normals"].Select(V).ToArray();
            if (normals.Length == m.vertexCount) m.normals = normals;
            var uv = d["uv"].Select(t => new Vector2((float)t[0], (float)t[1])).ToArray();
            if (uv.Length == m.vertexCount) m.uv = uv;
            var uv2 = d["uv2"].Select(t => new Vector2((float)t[0], (float)t[1])).ToArray();
            if (uv2.Length == m.vertexCount) m.uv2 = uv2;
            var tangents = d["tangents"].Select(t => new Vector4((float)t[0], (float)t[1], (float)t[2], (float)t[3])).ToArray();
            if (tangents.Length == m.vertexCount) m.tangents = tangents;
            var colors = d["colors"].Select(t => new Color((float)t[0], (float)t[1], (float)t[2], (float)t[3])).ToArray();
            m.colors = colors.Length == m.vertexCount ? colors : Enumerable.Repeat(Color.white, m.vertexCount).ToArray();
            var sub = d["triangles"];
            if (count == 0) {first = 0; count = sub.Count();}
            m.subMeshCount = count;
            for (int i = 0; i < count; i++) m.SetTriangles(sub[first+i].Select(t => (int)t).ToArray(), i);
            if (normals.Length != m.vertexCount) m.RecalculateNormals();
            if (tangents.Length != m.vertexCount && uv.Length == m.vertexCount) m.RecalculateTangents();
            m.RecalculateBounds(); return m;
        }
        private Material MaterialFor(string name, Material[] available, Dictionary<string,Material> cache)
        {
            Material result;
            if (cache.TryGetValue(name, out result)) return result;
            // A cloned mirror/decal material is not a self-contained surface: its
            // renderer normally receives textures/property blocks from controllers.
            // Use a native opaque mesh shader for ALL recovered structural surfaces.
            var source = available.FirstOrDefault(m => m.name.Replace(" (Instance)", "") == "DarkEnvironment"
                && m.shader && m.HasProperty("_Color"));
            if (!source) source = available.FirstOrDefault(m => m.shader && m.HasProperty("_Color")
                && m.name.IndexOf("dark", StringComparison.OrdinalIgnoreCase)>=0
                && m.shader.name.IndexOf("mirror", StringComparison.OrdinalIgnoreCase)<0);
            if (!source) throw new InvalidOperationException("Cannot find an opaque structural material for " + name);
            result = new Material(source) { name = "Classic." + name };
            bool trim = name == "PlayerPlace";
            bool floor = name == "PlayersPlaceMirror" || name == "TrackMirror";
            Color tint = trim ? new Color(.03f,.65f,1f,1f)
                : floor ? new Color(.075f,.10f,.14f,1f) : new Color(.035f,.045f,.065f,1f);
            Color emission = trim ? new Color(.03f,.45f,.8f,1f)
                : floor ? new Color(.025f,.035f,.055f,1f) : new Color(.008f,.012f,.02f,1f);
            result.SetColor("_Color", tint);
            if (result.HasProperty("_EmissionColor")) {result.SetColor("_EmissionColor", emission);result.EnableKeyword("_EMISSION");}
            foreach (string keyword in new[] {"_ENABLE_CUTOUT", "_CUTOUT_ON", "_ALPHATEST_ON", "_ALPHABLEND_ON", "_ALPHAPREMULTIPLY_ON"}) result.DisableKeyword(keyword);
            foreach (string property in new[] {"_Cutout", "_CutoutValue", "_EnableCutout", "_Mode"})
                if (result.HasProperty(property)) result.SetFloat(property, 0);
            if (result.HasProperty("_SrcBlend")) result.SetInt("_SrcBlend", (int)BlendMode.One);
            if (result.HasProperty("_DstBlend")) result.SetInt("_DstBlend", (int)BlendMode.Zero);
            if (result.HasProperty("_ZWrite")) result.SetInt("_ZWrite", 1);
            result.SetOverrideTag("RenderType", "Opaque");
            result.renderQueue = (int)RenderQueue.Geometry;
            Plugin.Log.Info("Surface " + name + " uses opaque shader " + result.shader.name);
            cache[name] = result; owned.Add(result); return result;
        }
        private void HandleEvent(BasicBeatmapEventData e)
        {
            if (callbacks.songTime < previousTime)
            {
                foreach (var laser in lasers) laser.Reset();
                foreach (var ring in rings) ring.Reset();
                spectrumEnvelope.Reset();
                previousTime = callbacks.songTime;
            }
            int type = (int)e.basicBeatmapEventType;
            foreach (var laser in lasers) if (laser.Event == type) laser.EventValue(e.value, random);
            foreach (var ring in rings) ring.Event(type, random);
        }
        private void Update()
        {
            if (!committed) return;
            float now = callbacks.songTime, dt = now - previousTime;
            previousTime = now;
            if (dt < 0) { spectrumEnvelope.Reset(); foreach (var l in lasers) l.Reset(); foreach (var r in rings) r.Reset(); return; }
            if (dt <= 0) return; // Song time also handles pause and practice speed.
            dt = Mathf.Min(dt, .1f);
            foreach (var l in lasers) l.Tick(dt);
            foreach (var r in rings) r.Tick(dt);
            UpdateSpectrum(dt);
        }
        private void ResolveSongAudio()
        {
            // Use only a gameplay clock's source, never a menu/preview spectrogram.
            // Injection and clip assignment can complete after the environment loads.
            var clocks = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>()
                .Where(c => c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded && c.isActiveAndEnabled)
                .OrderBy(c => Mathf.Abs(c.songTime - callbacks.songTime));
            foreach (var clock in clocks)
            {
                var source = Fields.Get<AudioSource>(clock, "_audioSource");
                if (!source || !source.clip || !source.isActiveAndEnabled) continue;
                songAudio = source;
                Plugin.Log.Info("Visualizer bound to gameplay audio: " + source.gameObject.scene.name + "/" + source.name);
                return;
            }
        }
        private void UpdateSpectrum(float dt)
        {
            if ((!songAudio || !songAudio.clip || !songAudio.isActiveAndEnabled) && Time.unscaledTime >= nextAudioLookup)
            {
                nextAudioLookup=Time.unscaledTime+1f;
                ResolveSongAudio();
            }
            Array.Clear(spectrum,0,spectrum.Length);
            if (songAudio && songAudio.isPlaying) songAudio.GetSpectrumData(spectrum,0,FFTWindow.BlackmanHarris);
            else if (!songAudio) AudioListener.GetSpectrumData(spectrum,0,FFTWindow.BlackmanHarris);
            spectrumEnvelope.Process(spectrum,AudioSettings.outputSampleRate,dt);
            if (!loggedSpectrum && spectrum.Any(x=>x>.00001f))
            {
                loggedSpectrum=true;
                Plugin.Log.Info("Visualizer received live spectrum (" + (songAudio ? "song source" : "listener fallback") + ").");
            }
            foreach (var s in spectra) s.Tick(spectrumEnvelope.Values);
        }

        private void LateUpdate()
        {
            if (!committed) return;
            // Native light scripts still run to provide colors. Their parametric
            // controllers can re-enable old renderers when a light turns on.
            foreach (var r in hiddenRenderers) if (r) r.enabled = false;
            foreach (var l in hiddenLights) if (l) l.enabled = false;
        }
        private void OnDestroy()
        {
            if (callbacks != null && callback != null) callbacks.RemoveBeatmapCallback(callback);
            if (lightManager) foreach (var l in links) lightManager.UnregisterLight(l);
            // These lists only contain objects that were enabled before installation.
            foreach (var r in hiddenRenderers) if (r) r.enabled = true;
            foreach (var l in hiddenLights) if (l) l.enabled = true;
            foreach (var asset in owned) if (asset) Object.Destroy(asset);
        }
    }

    internal sealed class LightLink : ILightWithId
    {
        public bool isRegistered {get; private set;}
        public int lightId {get; private set;}
        private readonly TubeBloomPrePassLight tube;
        internal LightLink(int id, TubeBloomPrePassLight t) {lightId=id;tube=t;}
        public void __SetIsRegistered() {isRegistered=true;}
        public void __SetIsUnRegistered() {isRegistered=false;}
        public void ColorWasSet(Color c)
        {
            if (tube) {tube.color=c;tube.Refresh();}

        }
    }
    internal sealed class LaserMotion
    {
        internal readonly int Event;
        private readonly Transform tr; private readonly Quaternion origin; private readonly Vector3 axis; private float speed;
        internal LaserMotion(Transform t,int ev,Vector3 a) {tr=t;Event=ev;axis=a;origin=t.localRotation;}
        internal void Reset() {speed=0;tr.localRotation=origin;}
        internal void EventValue(int v,System.Random rng) {Reset();if(v>0){tr.Rotate(axis,(float)rng.NextDouble()*180,Space.Self);speed=v*20*(rng.Next(2)==0?-1:1);}}
        internal void Tick(float dt) {tr.Rotate(axis,dt*speed,Space.Self);}
    }
    internal sealed class SpectrumMotion
    {
        private readonly Transform[] columns;private readonly float min,max;
        internal SpectrumMotion(JToken s,Dictionary<string,Transform> nodes) {columns=s["nodes"].Select(n=>nodes[(string)n]).ToArray();max=(float)s["max"];
            // The original baseline ends six metres below the floor. Keep the tips
            // visible even in quiet passages, while preserving the original maximum.
            min=Mathf.Min(max,Mathf.Max((float)s["min"],-columns[0].parent.position.y+.1f));
            Tick(new float[SpectrumEnvelope.Bands]);}
        internal void Tick(IList<float> samples)
        {
            for(int i=0;i<64;i++)
            {
                float amplitude=Mathf.Clamp01(samples[i]);
                float height=Mathf.Lerp(min,max,amplitude);
                var forward=columns[i].localScale;forward.y=height;columns[i].localScale=forward;
                var backward=columns[i+64].localScale;backward.y=height;columns[i+64].localScale=backward;
            }
        }
    }
    internal sealed class RingMotion
    {
        private sealed class Wave {internal float Progress,Angle,Step,Speed,Flex;}
        private readonly Transform[] tr;private readonly float[] angles,targets,flex;
        private readonly JToken settings;private readonly List<Wave> waves=new List<Wave>();
        private float spacing,moveSpeed;private bool alternate;
        internal RingMotion(JToken s,Dictionary<string,Transform> nodes)
        {settings=s;tr=s["nodes"].Select(n=>nodes[(string)n]).ToArray();angles=new float[tr.Length];targets=new float[tr.Length];flex=new float[tr.Length];Reset();}
        internal void Reset()
        {
            waves.Clear();alternate=false;spacing=(float)settings["spacing"];moveSpeed=0;
            for(int i=0;i<tr.Length;i++){angles[i]=targets[i]=flex[i]=0;tr[i].localRotation=Quaternion.identity;tr[i].localPosition=new Vector3(0,0,i*spacing);}
            var s=settings["startup"];
            if(s!=null) waves.Add(new Wave {Angle=(float)s["_startupRotationAngle"],Step=(float)s["_startupRotationStep"],Speed=(float)s["_startupRotationPropagationSpeed"],Flex=(float)s["_startupRotationFlexySpeed"]});
        }
        internal void Event(int type,System.Random rng)
        {
            var spin=settings["spin"];
            if(spin!=null && type==(int)spin["_songEventType"])
            {if(waves.Count>=64)waves.RemoveAt(0);waves.Add(new Wave {Angle=targets[0]+90*(rng.Next(2)==0?-1:1),Step=(float)rng.NextDouble()*(float)spin["_rotationStep"],Speed=(float)spin["_rotationPropagationSpeed"],Flex=(float)spin["_rotationFlexySpeed"]});}
            var move=settings["move"];
            if(move!=null && type==(int)move["_songEventType"]){spacing=(float)move[alternate?"_maxPositionStep":"_minPositionStep"];alternate=!alternate;moveSpeed=(float)move["_moveSpeed"];}
        }
        internal void Tick(float dt)
        {
            for(int w=0;w<waves.Count;w++)
            {var wave=waves[w];float before=wave.Progress;wave.Progress+=dt*wave.Speed;
             for(int i=0;i<tr.Length;i++){float p=(float)i/tr.Length;if(p>=before&&p<wave.Progress){targets[i]=wave.Angle+i*wave.Step;flex[i]=wave.Flex;}}}
            waves.RemoveAll(w=>w.Progress>1);
            for(int i=0;i<tr.Length;i++){angles[i]=Mathf.Lerp(angles[i],targets[i],dt*flex[i]);tr[i].localRotation=Quaternion.Euler(0,0,angles[i]);var p=tr[i].localPosition;p.z=Mathf.Lerp(p.z,i*spacing,dt*moveSpeed);tr[i].localPosition=p;}
        }
    }
}
