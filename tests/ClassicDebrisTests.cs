using System;
using OldSaber;
internal static class ClassicDebrisTests
{
    static void Near(float a,float b,string why) {if(Math.Abs(a-b)>1e-5)throw new Exception(why+": "+a+" != "+b);}
    static void Main()
    {
        Near(ClassicDebrisModel.SeparationSpeed(1),8,"upwards half");
        Near(ClassicDebrisModel.SeparationSpeed(-1),1,"downwards half");
        Near(ClassicDebrisModel.SeparationSpeed(0),4.5f,"sideways half");
        var normal=new DebrisVector(1,0,0);
        var force=ClassicDebrisModel.Velocity(normal,new DebrisVector(0,1,0));
        Near(force.X,4.5f,"outward velocity");Near(force.Y,.9f,"20% random separation jitter");
        var spin=ClassicDebrisModel.Spin(new DebrisVector(0,0,1));Near(spin.Z,4.5f,"spin limit");
        var vertices=new DebrisVector[8];int index=0;
        foreach(float x in new[]{-.25f,.25f})foreach(float y in new[]{-.25f,.25f})foreach(float z in new[]{-.25f,.25f})vertices[index++]=new DebrisVector(x,y,z);
        var half=ClassicDebrisModel.Centroid(vertices,new DebrisVector(),normal);
        Near(half.X,.125f,"center-cut half pivot");Near(half.Y,0,"symmetric y pivot");Near(half.Z,0,"symmetric z pivot");
        var offCenter=ClassicDebrisModel.Centroid(vertices,new DebrisVector(.1f,0,0),normal);
        Near(offCenter.X,.175f,"off-center cut pivot");
        var opposite=ClassicDebrisModel.Centroid(vertices,new DebrisVector(),new DebrisVector(-1,0,0));Near(opposite.X,-.125f,"opposite half");
        Near(ClassicDebrisModel.Centroid(vertices,new DebrisVector(),new DebrisVector()).X,0,"degenerate normal");
        var rng=new Random(12);
        for(int i=0;i<1000;i++)
        {
            var unit=ClassicDebrisModel.OnSphere(rng);Near(DebrisVector.Dot(unit,unit),1,"unit sphere");
            var inside=ClassicDebrisModel.InsideSphere(rng);
            if(DebrisVector.Dot(inside,inside)>1.00001)throw new Exception("spin sample outside sphere");
        }
        Console.WriteLine("PASS: classic upward/downward impulses, jitter, spin bounds, cut-half pivots and sample distribution bounds.");
    }
}
