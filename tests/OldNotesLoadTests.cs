using System;
using System.IO;

// Stand-ins deliberately read lazily, matching the Unity behavior in the crash log.
namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object obj)=>obj!=null;
        public static void Destroy(Object obj){}
    }
    public class Material : Object
    {
        public float Brightness=2f;
        public bool HasProperty(string name)=>true;
        public float GetFloat(string name)=>Brightness;
        public void SetFloat(string name,float value){Brightness=value;}
    }
    public class GameObject : Object { }
    public static class Resources {public static T[] FindObjectsOfTypeAll<T>()=>Array.Empty<T>();}
    public class AssetBundle : Object
    {
        public static Stream LastStream;
        public static int Loads, Unloads;
        public static bool FailBundle, FailAsset;
        public static AssetBundle LoadFromStream(Stream stream)
        {
            LastStream=stream;Loads++;
            if(!stream.CanRead)throw new Exception("Stream was closed at bundle load");
            return FailBundle?null:new AssetBundle();
        }
        public T LoadAsset<T>(string path) where T:new()
        {
            if(!LastStream.CanRead)throw new Exception("Regression: stream closed before LoadAsset");
            LastStream.Seek(0,SeekOrigin.Begin);
            if(LastStream.ReadByte()<0)throw new Exception("Empty bundle");
            if(FailAsset)throw new InvalidDataException("Simulated asset failure");
            return new T();
        }
        public void Unload(bool destroy)
        {
            if(!LastStream.CanRead)throw new Exception("Regression: stream closed before Unload");
            if(destroy)throw new Exception("Loaded assets must survive bundle unload");
            Unloads++;
        }
    }
}
namespace UnityEngine.Rendering { }
namespace ClassiNotes.DataStores
{
    public class PluginDataSO : UnityEngine.Object
    {
        public UnityEngine.Material NoteMaterial=new UnityEngine.Material(),NoteDebrisMaterial=new UnityEngine.Material();
        public UnityEngine.Object 
            ReflectionProbeContainer=new UnityEngine.Object(),NoteTrailPS=new UnityEngine.Object(),BombNoteTrailPS=new UnityEngine.Object();
    }
}
namespace OldSaber
{
    public class Logger {public int Errors;public void Info(string s){}public void Error(string s){Errors++;}}
    public static class Plugin {public static Logger Log=new Logger();}
    public class OldNotesMaterialState:UnityEngine.Object {public void Restore(){}}
    public class OldNotesTrail:UnityEngine.Object {public void Restore(){}}
    public class OldNotesProbe:UnityEngine.Object {public UnityEngine.GameObject gameObject;}
    public static class LoaderTests
    {
        static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
        public static void Main()
        {
            OldNotesAssets.Load();
            Check(OldNotesAssets.Data!=null && Plugin.Log.Errors==0,"Successful load failed");
            Check(UnityEngine.AssetBundle.Unloads==1,"Bundle not unloaded after load");
            Check(!UnityEngine.AssetBundle.LastStream.CanRead,"Stream leaked after successful load");
            OldNotesAssets.Load();
            Check(UnityEngine.AssetBundle.Loads==1,"Cached assets loaded twice");
            Check(Math.Abs(OldNotesAssets.Data.NoteMaterial.Brightness-2.5f)<0.0001f,"Note brightness boost missing or compounded");
            Check(Math.Abs(OldNotesAssets.Data.NoteDebrisMaterial.Brightness-2.5f)<0.0001f,"Debris brightness boost missing or compounded");
            OldNotesAssets.Data=null;
            UnityEngine.AssetBundle.FailAsset=true;
            OldNotesAssets.Load();
            Check(OldNotesAssets.Data==null && Plugin.Log.Errors==1,"Failed asset did not fall back");
            Check(UnityEngine.AssetBundle.Unloads==2,"Failed asset load leaked bundle");
            Check(!UnityEngine.AssetBundle.LastStream.CanRead,"Failed asset load leaked stream");
            UnityEngine.AssetBundle.FailAsset=false;UnityEngine.AssetBundle.FailBundle=true;
            OldNotesAssets.Load();
            Check(OldNotesAssets.Data==null && Plugin.Log.Errors==2,"Failed bundle did not fall back");
            Check(!UnityEngine.AssetBundle.LastStream.CanRead,"Failed bundle leaked stream");
            UnityEngine.AssetBundle.FailBundle=false;
            OldNotesAssets.Load();
            Check(OldNotesAssets.Data!=null,"Retry failed");
            Console.WriteLine("PASS: actual loader keeps stream readable through asset load/unload; success/failure disposal, cached load and retry.");
        }
    }
}
