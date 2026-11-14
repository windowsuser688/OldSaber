using System;
using OldSaber;
namespace UnityEngine
{
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public class RectTransform {public Vector2 anchorMin,anchorMax,pivot,sizeDelta;public Vector3 anchoredPosition3D;}
}
public static class ClassicGuiLayoutTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        var rect=new UnityEngine.RectTransform();
        ClassicGuiLayout.Rect(rect,new UnityEngine.Vector2(0,1),new UnityEngine.Vector2(1,1),new UnityEngine.Vector2(.5f,1),new UnityEngine.Vector2(0,6),new UnityEngine.Vector2(0,-2),-.01f);
        Check(rect.anchoredPosition3D.y==-2 && rect.anchoredPosition3D.z==-.01f,"Anchored offset/depth must be preserved instead of stale local Y");
        Check(rect.anchorMax.x==1 && rect.sizeDelta.y==6 && rect.pivot.y==1,"Rect sizing lost");
        // Simulate Canvas changing its dimensions, then apply the final layout.
        rect.sizeDelta=new UnityEngine.Vector2(1920,1080);
        ClassicGuiLayout.Rect(rect,new UnityEngine.Vector2(),new UnityEngine.Vector2(),new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(100,100),new UnityEngine.Vector2(0,1.2f),0);
        Check(rect.sizeDelta.x==100 && rect.sizeDelta.y==100,"Canvas initialization size must be corrected");
        Check(!ClassicGuiLayout.StaticVisible("TextOverlayPanelOutline0",true) && !ClassicGuiLayout.StaticVisible("AnimationCircle",true),"Uncontrolled animation overlays must be hidden");
        Check(ClassicGuiLayout.StaticVisible("TextPanel",true) && !ClassicGuiLayout.StaticVisible("FeverModeCanvas",false),"Main labels and source visibility must remain correct");
        Console.WriteLine("PASS: HUD anchored offsets, dimensions after canvas initialization, and animation-only layer visibility.");
    }
}
