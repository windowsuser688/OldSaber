using UnityEngine;
namespace OldSaber
{
    internal static class SaberTrailAppearance
    {
        internal static bool IsAlphaBlended(string shaderName)=>shaderName=="Legacy Shaders/Particles/Alpha Blended";
        internal static Color32 ToAlphaMask(Color32 pixel)
        {
            int brightness=System.Math.Max(pixel.r,System.Math.Max(pixel.g,pixel.b));
            return new Color32(255,255,255,(byte)(brightness*pixel.a/255));
        }
        internal static Color TrailColor(Color playerColor,bool alphaBlended,float originalAlpha)
        {
            playerColor.a=alphaBlended?.65f:originalAlpha;
            return playerColor;
        }
    }
}
