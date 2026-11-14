using System;
namespace OldSaber
{
    internal static class ClassicGuiSizing
    {
        internal static float TextHeight(float fontSize,float pointSize,float faceScale,float capHeight,float worldScale,bool orthographic)
        {
            if(pointSize<=0)return 0;
            return Math.Abs(fontSize/pointSize*faceScale*capHeight*worldScale*(orthographic?1f:.1f));
        }
        internal static float Ratio(float desired,float current)
        {
            if(desired<=0 || current<=0 || float.IsNaN(desired) || float.IsNaN(current) || float.IsInfinity(desired) || float.IsInfinity(current))return 1;
            return Math.Max(.01f,Math.Min(100f,desired/current));
        }
    }
}
