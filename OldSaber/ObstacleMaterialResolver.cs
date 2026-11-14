using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace OldSaber
{
    internal static class ObstacleMaterialResolver
    {
        // Asset bundles can contain a usable shader even when Shader.Find resolves
        // a missing or unsupported copy with the same name.
        internal static Shader Frame(Shader found,IEnumerable<Material> loaded)
        {
            var material=loaded.FirstOrDefault(m=>m && m.shader && m.shader.isSupported &&
                m.shader.name=="Custom/GlowingInstancedHD");
            if(material)return material.shader;
            return found && found.isSupported && found.name=="Custom/GlowingInstancedHD"?found:null;
        }
    }
}
