using System;
namespace OldSaber
{
    internal static class ClassicMenuLayout
    {
        internal const float Width=160,Height=80;
        internal static float Fit(float width,float height)
        {
            if(width<=0 || height<=0 || float.IsNaN(width) || float.IsNaN(height) || float.IsInfinity(width) || float.IsInfinity(height))return 1;
            return Math.Min(width/Width,height/Height);
        }
    }
}
