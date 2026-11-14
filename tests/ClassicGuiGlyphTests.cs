using System;
using OldSaber;
namespace UnityEngine.TextCore
{
    public struct GlyphRect
    {
        public int x,y,width,height;
        public GlyphRect(int x,int y,int width,int height){this.x=x;this.y=y;this.width=width;this.height=height;}
        public static GlyphRect zero=>new GlyphRect();
    }
}
public static class ClassicGuiGlyphTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        var r=ClassicGuiGlyphs.Rect(48,10,20,30,40,512);
        Check(r.x==10 && r.y==452 && r.width==30 && r.height==40,"Legacy top-origin conversion failed");
        var space=ClassicGuiGlyphs.Rect(32,6,517,14.25f,75,512);
        Check(space.width==0 && space.height==0 && space.y==0,"Legacy space would sample outside atlas");
        var nbsp=ClassicGuiGlyphs.Rect(160,6,517,0,0,512);
        Check(nbsp.y==0 && nbsp.height==0,"Empty nonbreaking space has invalid rect");
        var rounded=ClassicGuiGlyphs.Rect(65,2,3,5.6f,7.7f,32);
        Check(rounded.width==6 && rounded.height==8 && rounded.y==21,"TMP upgrade rounding changed");
        Console.WriteLine("PASS: classic font atlas orientation, TMP rounding, space and empty-glyph handling.");
    }
}
