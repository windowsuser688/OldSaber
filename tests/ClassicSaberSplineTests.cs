using System;
using OldSaber.LegacyTrail;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    }
    public static class Mathf {public static float Clamp01(float f)=>Math.Clamp(f,0f,1f);}
}
public static class SaberSplineTests
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Main()
    {
        var spline=new Spline();
        for(int i=0;i<20;i++)spline.AddControlPoint(new UnityEngine.Vector3(0,0,0),new UnityEngine.Vector3(0,0,1));
        spline.RefreshSpline();
        for(int i=0;i<60;i++)
        {
            var p=spline.InterpolateByLen(i/60f);var n=spline.InterpolateNormalByLen(i/60f);
            Check(float.IsFinite(p.x) && p.magnitude==0,"Stationary saber generated invalid trail");
            Check(Math.Abs(n.z-1)<1e-6,"Stationary trail width changed");
        }
        spline.Clear();
        for(int i=0;i<20;i++)spline.AddControlPoint(new UnityEngine.Vector3(i,0,0),new UnityEngine.Vector3(0,0,1));
        spline.RefreshSpline();float previous=-1;
        for(int i=0;i<60;i++)
        {
            var p=spline.InterpolateByLen(i/60f);
            Check(float.IsFinite(p.x) && p.x>=previous,"Straight swing trail reversed");previous=p.x;
            Check(Math.Abs(p.y)+Math.Abs(p.z)<1e-6,"Straight swing drifted off axis");
        }
        // Reset after a tracking discontinuity must not retain old positions.
        spline.Clear();
        for(int i=0;i<20;i++)spline.AddControlPoint(new UnityEngine.Vector3(100,0,0),new UnityEngine.Vector3(0,0,1));
        spline.RefreshSpline();Check(spline.InterpolateByLen(.5f).x==100,"Reset retained previous swing");
        Console.WriteLine("PASS: recovered saber spline handles stationary blades, straight swings, widths and reset without nonfinite vertices.");
    }
}
