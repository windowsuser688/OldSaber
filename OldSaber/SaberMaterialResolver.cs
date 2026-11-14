using System;
using System.Linq;
using UnityEngine;
namespace OldSaber
{
    internal static class SaberMaterialResolver
    {
        internal static bool RendersPart(string name)=>name!="Cylinder";
        internal static bool UsesHandleMaterial(string name)=>name=="Handle";
        internal static Material Select(Material[] materials,bool handle)
        {
            // A shader already used by the current model is available even when
            // the legacy shader name is stripped or renamed in this game version.
            var candidates=materials.Where(m=>m && m.shader && m.shader.isSupported &&
                m.HasProperty("_Color") && !m.HasProperty("_ReflectionTex") &&
                m.shader.name.IndexOf("Fake",StringComparison.OrdinalIgnoreCase)<0 &&
                m.shader.name.IndexOf("Sprite",StringComparison.OrdinalIgnoreCase)<0).ToArray();
            return candidates.OrderByDescending(m=>
                handle ? (m.name.IndexOf("Handle",StringComparison.OrdinalIgnoreCase)>=0 ? 2:0)
                       : (m.name.IndexOf("Blade",StringComparison.OrdinalIgnoreCase)>=0 ? 2:
                          m.shader.name.IndexOf("Glow",StringComparison.OrdinalIgnoreCase)>=0 ? 1:0)).FirstOrDefault();
        }
    }
}
