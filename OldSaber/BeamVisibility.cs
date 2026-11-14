using System;

namespace OldSaber
{
    internal static class BeamVisibility
    {
        // Only the solid core is gated; the native bloom retains the full fade.
        internal static bool ShouldDraw(float r, float g, float b, float alpha)
        {
            float brightness = Math.Max(r, Math.Max(g, b)) * Math.Max(0, alpha);
            return !float.IsNaN(brightness) && !float.IsInfinity(brightness)
                && alpha > .01f && brightness > .02f;
        }
    }
}
