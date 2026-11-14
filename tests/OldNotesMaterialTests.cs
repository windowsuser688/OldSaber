using System;
using System.Reflection;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField:Attribute{}
    public class Object {public static implicit operator bool(Object o)=>o!=null;}
    public class MonoBehaviour:Object {public bool enabled=true;}
    public class Material:Object{}
    public class MeshRenderer:Object
    {
        public Material[] sharedMaterials;
        public Material sharedMaterial {get=>sharedMaterials[0];set=>sharedMaterials=new[]{value};}
        public Rendering.ReflectionProbeUsage reflectionProbeUsage;
        public ConditionalMaterialSwitcher Switcher;
        public T GetComponent<T>() where T:class=>Switcher as T;
    }
}
namespace UnityEngine.Rendering {public enum ReflectionProbeUsage {Off,Simple}}
public class ConditionalMaterialSwitcher:UnityEngine.MonoBehaviour
{
    public UnityEngine.Material _material0,_material1;
    public UnityEngine.MeshRenderer Renderer;
    public bool Value;
    // Reproduces the actual 1.40.8 Awake, even if enabled is false.
    public void Awake(){Renderer.sharedMaterial=Value?_material1:_material0;}
}
namespace OldSaber
{
    internal static class Fields
    {
        internal static T Get<T>(object obj,string name)=>(T)obj.GetType().GetField(name).GetValue(obj);
        internal static void Set(object obj,string name,object value)=>obj.GetType().GetField(name).SetValue(obj,value);
    }
    public static class MaterialTests
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void Main()
        {
            foreach(bool quality in new[]{false,true})
            {
                var low=new UnityEngine.Material();var high=new UnityEngine.Material();var classic=new UnityEngine.Material();
                var renderer=new UnityEngine.MeshRenderer {sharedMaterials=new[]{low}};
                var switcher=new ConditionalMaterialSwitcher {_material0=low,_material1=high,Renderer=renderer,Value=quality};
                renderer.Switcher=switcher;
                var state=new OldNotesMaterialState();state.Apply(renderer,classic);
                switcher.Awake();
                Check(renderer.sharedMaterial==classic,"Native Awake replaced classic material");
                // Pool reuse / another Awake must not revert either quality branch.
                switcher.Value=!quality;switcher.Awake();
                Check(renderer.sharedMaterial==classic,"Other quality branch replaced classic material");
                state.Apply(renderer,classic);state.Restore();
                Check(renderer.sharedMaterial==low,"Original renderer material not restored");
                Check(switcher._material0==low && switcher._material1==high && switcher.enabled,"Original switcher settings not restored");
                state.Restore();
            }
            var debris=new UnityEngine.MeshRenderer {sharedMaterials=new[]{new UnityEngine.Material()}};
            var original=debris.sharedMaterial;var replacement=new UnityEngine.Material();
            debris.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Simple;
            var debrisState=new OldNotesMaterialState();debrisState.Apply(debris,replacement,false);
            Check(debris.reflectionProbeUsage==UnityEngine.Rendering.ReflectionProbeUsage.Off,"Matte material must disable reflection probes");
            Check(debris.sharedMaterial==replacement,"Debris without switcher failed");
            debrisState.Restore();Check(debris.sharedMaterial==original,"Debris restoration failed");
            Check(debris.reflectionProbeUsage==UnityEngine.Rendering.ReflectionProbeUsage.Simple,"Original reflection mode not restored");
            Console.WriteLine("PASS: actual material state survives disabled-switcher Awake in both quality branches; restores originals; debris remains supported.");
        }
    }
}
