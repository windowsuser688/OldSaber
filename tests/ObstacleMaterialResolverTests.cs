using System;
using OldSaber;
namespace UnityEngine
{
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public class Shader:Object {public string name;public bool isSupported=true;}
 public class Material:Object {public Shader shader;}
}
public static class ResolverTests
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Main()
 {
  var good=new UnityEngine.Shader{name="Custom/GlowingInstancedHD"};
  var bad=new UnityEngine.Shader{name=good.name,isSupported=false};
  var wrong=new UnityEngine.Shader{name="Custom/ParametricBoxFrameHD"};
  var loaded=new[]{null,new UnityEngine.Material{shader=bad},new UnityEngine.Material{shader=wrong},new UnityEngine.Material{shader=good}};
  Check(ObstacleMaterialResolver.Frame(null,loaded)==good,"Shader.Find miss must resolve loaded shader");
  Check(ObstacleMaterialResolver.Frame(bad,loaded)==good,"Unsupported same-name shader must not mask loaded shader");
  Check(ObstacleMaterialResolver.Frame(good,new UnityEngine.Material[0])==good,"Valid direct fallback");
  Check(ObstacleMaterialResolver.Frame(bad,new UnityEngine.Material[0])==null,"Reject unsupported shader");
  Check(ObstacleMaterialResolver.Frame(wrong,new UnityEngine.Material[0])==null,"Reject parametric shader for extracted meshes");
  Console.WriteLine("PASS: obstacle shader resolution handles missing/unsupported Shader.Find results and rejects incompatible shaders.");
 }
}
