using System;
using OldSaber;
namespace UnityEngine
{
    public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
}
public static class TrailAppearanceTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        var black=SaberTrailAppearance.ToAlphaMask(new UnityEngine.Color32(0,0,0,255));
        Check(black.a==0 && black.r==255,"Black additive background must become transparent, not darken the scene");
        var half=SaberTrailAppearance.ToAlphaMask(new UnityEngine.Color32(128,128,128,255));
        Check(half.a==128 && half.g==255,"Fade gradient must move into alpha");
        Check(SaberTrailAppearance.ToAlphaMask(new UnityEngine.Color32(255,255,255,0)).a==0,"Existing transparency must be retained");
        var custom=new UnityEngine.Color(.2f,.8f,.4f,1);
        var color=SaberTrailAppearance.TrailColor(custom,true,.25f);
        Check(color.r==custom.r && color.g==custom.g && color.b==custom.b && color.a==.65f,"Fallback trail must retain player color and visible opacity");
        Check(SaberTrailAppearance.TrailColor(custom,false,.25f).a==.25f,"Additive trail opacity changed");
        Console.WriteLine("PASS: black trail texels become transparent, gradients survive, and player color is retained with brighter fallback opacity.");
    }
}
