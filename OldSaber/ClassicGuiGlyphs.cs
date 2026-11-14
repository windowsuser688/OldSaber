using UnityEngine.TextCore;
namespace OldSaber
{
    internal static class ClassicGuiGlyphs
    {
        internal static GlyphRect Rect(uint code,float x,float y,float width,float height,int atlasHeight)
        {
            // Legacy fonts place whitespace outside the atlas. Preserve its
            // advance metrics, but never give it an out-of-bounds render rect.
            if(code==32 || width<=0 || height<=0)return GlyphRect.zero;
            return new GlyphRect((int)x,atlasHeight-(int)(y+height+.5f),(int)(width+.5f),(int)(height+.5f));
        }
    }
}
