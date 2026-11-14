using System;
using System.Linq;
using System.Reflection;
using OldSaber;
using UnityEngine;
namespace UnityEngine
{
 public class SerializeField:Attribute{}
 public class Object {public static implicit operator bool(Object o)=>o!=null;public static void Destroy(Object o){}}
 public class MonoBehaviour:Object {public Transform transform=new Transform();public T GetComponent<T>() where T:new()=>new T();}
 public class Transform {public Vector3 localScale=new Vector3(1,1,1),localPosition;public Vector3 lossyScale=>localScale;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 back=>new Vector3(0,0,-1);public static Vector3 left=>new Vector3(-1,0,0);public static Vector3 right=>new Vector3(1,0,0);}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}}
 public class Mesh:Object {public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;public void RecalculateBounds(){}}
 public class MeshRenderer {public Bounds bounds;}
 public class MeshFilter {public Mesh mesh;}
}
public class ClassicObstacleGeometryTests
{
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static void Near(float a,float b){Check(Math.Abs(a-b)<.00001f,"Expected "+b+", got "+a);}
 static FieldInfo Field(Type t,string s)=>t.GetField(s,BindingFlags.Instance|BindingFlags.NonPublic);
 static Transform T(ClassicObstacleGeometry g,string s)=>(Transform)Field(g.GetType(),s).GetValue(g);
 public static void Main()
 {
  var g=new ClassicObstacleGeometry();
  foreach(var f in g.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic))
  {
   if(f.FieldType==typeof(Transform))f.SetValue(g,new Transform{localScale=new Vector3(.02f,.02f,.02f)});
   if(f.FieldType==typeof(ClassicObstacleCube))
   {var cube=new ClassicObstacleCube();cube.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cube,null);f.SetValue(g,cube);}
  }
  foreach(var size in new[]{new Vector3(2,3,8),new Vector3(1,.6f,.2f),new Vector3(4,5,20)})
  {
   g.SetSize(size.x,size.y,size.z);
   Near(g.bounds.size.x,size.x);Near(g.bounds.center.y,size.y/2);Near(g.bounds.center.z,size.z/2);
   Near(T(g,"_stretchableCore").localScale.z,size.z);
   Near(T(g,"_stretchableFrontTop").localScale.x,size.x);Near(T(g,"_stretchableFrontTop").localScale.y,.02f);
   Near(T(g,"_stretchableMiddleTopLeft").localScale.z,size.z);
   Near(T(g,"_cornerBackTopRight").localPosition.x,size.x/2);Near(T(g,"_cornerBackTopRight").localPosition.y,size.y);Near(T(g,"_cornerBackTopRight").localPosition.z,size.z);
  }
  var c=new ClassicObstacleCube();c.transform.localScale=new Vector3(2,3,8);
  c.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
  var mesh=(Mesh)Field(c.GetType(),"_mesh").GetValue(c);
  Check(mesh.vertices.Length==24 && mesh.triangles.Length==36,"Cube topology");
  Check(mesh.triangles.All(i=>i>=0 && i<24),"Invalid triangle index");
  Near(mesh.uv[0].x,2);Near(mesh.uv[0].y,8);Near(mesh.uv[4].x,8);Near(mesh.uv[4].y,3);
  c.transform.localScale=new Vector3(1,2,4);c.RefreshUVs();Near(mesh.uv[0].x,1);Near(mesh.uv[0].y,4);
  Console.WriteLine("PASS: original obstacle dimensions, short/long wall resizing, fixed edge thickness, corner placement, procedural topology and world-scaled UVs.");
 }
}
