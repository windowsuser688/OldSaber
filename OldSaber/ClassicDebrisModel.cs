using System;

namespace OldSaber
{
    internal struct DebrisVector
    {
        internal float X,Y,Z;
        internal DebrisVector(float x,float y,float z) {X=x;Y=y;Z=z;}
        public static DebrisVector operator +(DebrisVector a,DebrisVector b) {return new DebrisVector(a.X+b.X,a.Y+b.Y,a.Z+b.Z);}
        public static DebrisVector operator -(DebrisVector a,DebrisVector b) {return new DebrisVector(a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
        public static DebrisVector operator *(DebrisVector a,float b) {return new DebrisVector(a.X*b,a.Y*b,a.Z*b);}
        public static DebrisVector operator /(DebrisVector a,float b) {return a*(1f/b);}
        internal static float Dot(DebrisVector a,DebrisVector b) {return a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
    }
    internal static class ClassicDebrisModel
    {
        internal static float SeparationSpeed(float normalY) {return (normalY+1f)*.5f*7f+1f;}
        internal static DebrisVector Velocity(DebrisVector fragmentNormal,DebrisVector unitSphereSample)
        {return (fragmentNormal+unitSphereSample*.2f)*SeparationSpeed(fragmentNormal.Y);}
        internal static DebrisVector Spin(DebrisVector insideSphereSample) {return insideSphereSample*4.5f;}
        internal static DebrisVector OnSphere(System.Random rng)
        {
            double y=rng.NextDouble()*2-1, angle=rng.NextDouble()*Math.PI*2, r=Math.Sqrt(1-y*y);
            return new DebrisVector((float)(r*Math.Cos(angle)),(float)y,(float)(r*Math.Sin(angle)));
        }
        internal static DebrisVector InsideSphere(System.Random rng) {return OnSphere(rng)*(float)Math.Pow(rng.NextDouble(),1.0/3.0);}
        internal static DebrisVector Centroid(DebrisVector[] vertices,DebrisVector point,DebrisVector normal)
        {
            float length=(float)Math.Sqrt(DebrisVector.Dot(normal,normal));
            if(length<1e-6f)return new DebrisVector();
            var sum=new DebrisVector();int count=0;
            float offset=-DebrisVector.Dot(normal,point);
            foreach(var v in vertices)
            {
                float distance=DebrisVector.Dot(normal,v)+offset;
                if(distance<0)continue;
                var projected=v-normal*(distance/length);
                sum=sum+(projected+v)*.5f;count++;
            }
            return count>0?sum/count:new DebrisVector();
        }
    }
}
