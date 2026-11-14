using System;
using OldSaber;
public static class ClassicGuiSizingTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Near(float a,float b,string message){Check(Math.Abs(a-b)<.00001f,message);}
    public static void Main()
    {
        float ui=ClassicGuiSizing.TextHeight(40,80,1,60,.01f,true);
        float world=ClassicGuiSizing.TextHeight(1,80,1,60,1,false);
        Near(ui,.3f,"Canvas font world height incorrect");Near(world,.075f,"3D TMP scale factor omitted");
        float ratio=ClassicGuiSizing.Ratio(ui,world);Near(ratio,4,"3D score must fit native UI size");
        Near(ClassicGuiSizing.Ratio(ui,world*ratio),1,"Aligned text must not keep growing");
        Near(ClassicGuiSizing.Ratio(0,world),1,"Hidden/zero-size anchor must not collapse panel");
        Near(ClassicGuiSizing.Ratio(1,float.NaN),1,"Nonfinite anchor must not corrupt transforms");
        Console.WriteLine("PASS: native HUD size matching for 3D and UI text, stable repeated sizing, and invalid-size handling.");
    }
}
