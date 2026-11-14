using System;
using System.Reflection;
using OldSaber;
// Minimal lifecycle stand-ins: test the actual guard's callback ordering without
// claiming to run Unity's renderer. The shipped build uses the real game DLLs.
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object o) { return o != null; } }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform; public T GetComponent<T>() where T:Component { return gameObject.GetComponent<T>(); } }
    public class MonoBehaviour : Component { public bool isActiveAndEnabled = true; }
    public class MeshRenderer : Component { public bool enabled = true; public Material[] sharedMaterials; public Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows; }
    public class MeshFilter : Component { public Mesh sharedMesh; }
    public class Mesh : Object {}
    public class Material : Object {}
    public class GameObject : Object
    {
        public int layer; public Transform transform;
        private readonly System.Collections.Generic.Dictionary<Type,Component> components=new System.Collections.Generic.Dictionary<Type,Component>();
        public GameObject(string name) {transform=new Transform {gameObject=this};components[typeof(Transform)]=transform;}
        public T AddComponent<T>() where T:Component,new() {var c=new T {gameObject=this};components[typeof(T)]=c;return c;}
        public T GetComponent<T>() where T:Component {Component c;return components.TryGetValue(typeof(T),out c)?(T)c:null;}
    }
    public class Transform : Component { public readonly System.Collections.Generic.List<Transform> children=new System.Collections.Generic.List<Transform>();public void SetParent(Transform parent,bool worldPositionStays){parent.children.Add(this);} }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order){} }
}
namespace UnityEngine.Rendering { public enum ShadowCastingMode { Off } }
namespace OldSaber
{
    internal static class Fields
    {
        internal static T Get<T>(object target,string name) {return (T)target.GetType().GetField(name).GetValue(target);}
        internal static void Set(object target,string name,object value) {target.GetType().GetField(name).SetValue(target,value);}
    }
}
public class ParametricBoxController : UnityEngine.MonoBehaviour {public UnityEngine.MeshRenderer _meshRenderer;}
public class Parametric3SliceSpriteController : UnityEngine.MonoBehaviour {public float _widthMultiplier=1;}
public class TubeBloomPrePassLight : UnityEngine.MonoBehaviour
{
    public Parametric3SliceSpriteController _dynamic3SliceSprite;
    public ParametricBoxController _parametricBoxController;
    public bool _forceUseBakedGlow;
    public float _bakedGlowWidthScale=1;
    public UnityEngine.Color color;
    public event Action didRefreshEvent;
    public void Refresh() { didRefreshEvent?.Invoke(); }
}
internal static class LaserVisibilityTests
{
    static void Check(bool value,string why) { if(!value)throw new Exception(why); }
    static void Lifecycle(object target,string name) { target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null); }
    static void Main()
    {
        Check(VisualPolicy.UsesMirror("PlayersPlaceMirror"),"player standing mirror must remain");
        Check(VisualPolicy.UsesMirror("TrackMirror"),"runway mirror must be restored");
        Check(!VisualPolicy.UsesMirror("DarkEnvironment"),"structural surfaces must remain opaque");
        var templateObject=new UnityEngine.GameObject("template");
        templateObject.AddComponent<UnityEngine.MeshFilter>().sharedMesh=new UnityEngine.Mesh();
        templateObject.AddComponent<UnityEngine.MeshRenderer>().sharedMaterials=new[]{new UnityEngine.Material()};
        var box=templateObject.AddComponent<ParametricBoxController>();
        var sprite=templateObject.AddComponent<Parametric3SliceSpriteController>();
        var glow=new TubeBloomPrePassLight {_dynamic3SliceSprite=sprite};
        foreach(int id in new[]{3,4})
        {
            var parent=new UnityEngine.GameObject("side");var light=new TubeBloomPrePassLight();
            LaserVisuals.Create(parent.transform,light,box,glow,id);
            Check(light._parametricBoxController==null,"side laser must never contain opaque box core");
            Check(light._dynamic3SliceSprite!=null && light._forceUseBakedGlow,"side laser must use native glow");
            Check(parent.transform.children.Count==1,"side laser must have exactly one glow visual");
            var fallback=new UnityEngine.GameObject("fallback");var fallbackLight=new TubeBloomPrePassLight();
            LaserVisuals.Create(fallback.transform,fallbackLight,box,null,id);
            Check(fallback.transform.children.Count==0 && fallbackLight._parametricBoxController==null,"missing glow must never restore black-line box");
        }
        var other=new UnityEngine.GameObject("other");var otherLight=new TubeBloomPrePassLight();
        LaserVisuals.Create(other.transform,otherLight,box,glow,1);
        Check(otherLight._parametricBoxController!=null,"non-side lights retain their rendering path");
        Console.WriteLine("PASS: actual visual factory creates glow-only left/right lasers, safe fallback, and player and runway mirror policy.");
        var tube=new TubeBloomPrePassLight();var renderer=new UnityEngine.MeshRenderer();
        var guard=new LaserCoreVisibility();guard.Initialize(tube,renderer);
        tube.color=new UnityEngine.Color(1,0,0,1);Lifecycle(guard,"OnEnable");
        Check(renderer.enabled,"bright red must remain visible");
        tube.color=new UnityEngine.Color(.005f,0,0,1);tube.Refresh();
        Check(!renderer.enabled,"RGB fade must not leave an opaque dark core");
        renderer.enabled=true;Lifecycle(guard,"LateUpdate");
        Check(!renderer.enabled,"late native renderer re-enable must be corrected before rendering");
        tube.color=new UnityEngine.Color(0,0,1,.015f);tube.Refresh();
        Check(!renderer.enabled,"low-alpha blue tail must hide solid core");
        tube.color=new UnityEngine.Color(0,0,1,1);tube.Refresh();
        Check(renderer.enabled,"next blue flash must reappear immediately");
        tube.color=new UnityEngine.Color(0,0,0,1);tube.Refresh();
        Check(!renderer.enabled,"black with opaque alpha must be invisible");
        tube.color=new UnityEngine.Color(1,1,1,0);tube.Refresh();
        Check(!renderer.enabled,"off must be invisible");
        tube.color=new UnityEngine.Color(1,1,1,1);tube.isActiveAndEnabled=false;Lifecycle(guard,"LateUpdate");
        Check(!renderer.enabled,"disabled tube must not leave a core");
        tube.isActiveAndEnabled=true;Lifecycle(guard,"OnDisable");tube.Refresh();
        Check(!renderer.enabled,"disabled guard must unsubscribe from refresh");
        Lifecycle(guard,"OnEnable");Check(renderer.enabled,"re-enabled guard must resume");
        Check(!BeamVisibility.ShouldDraw(float.NaN,0,0,1),"invalid color must hide");
        Console.WriteLine("PASS: RGB/alpha fades, dark-core suppression, refresh/late-update ordering, flash recovery and unsubscribe/re-enable.");
    }
}
