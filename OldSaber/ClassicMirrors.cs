using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OldSaber
{
    internal static class ClassicMirrors
    {
        // The runway's combined mesh has top vertices at y=0..0.00004.
        // A mirror at zero competes for the same depth pixels and flickers.
        // Keep both mirrors above coplanar geometry and on the same reflection plane.
        internal const float SurfaceHeight = .002f;
        internal static bool IsReflectiveSurface(string materialName)
        {
            return VisualPolicy.UsesMirror(materialName);
        }

        internal static Mirror[] FindTemplates(Scene scene)
        {
            // Include inactive components: mirror quality Off must not prevent us
            // from setting up surfaces that will work when High is selected later.
            return Resources.FindObjectsOfTypeAll<Mirror>()
                .Where(m => m.gameObject.scene == scene && m.mirrorMaterial
                    && m.mirrorMaterial.shader && m.mirrorMaterial.HasProperty("_ReflectionTex")
                    && Fields.Get<MirrorRendererSO>(m, "_mirrorRenderer"))
                .ToArray();
        }

        internal static bool Attach(MeshRenderer surface, string sourceName,
            Mirror[] templates, List<Object> owned)
        {
            if (!IsReflectiveSurface(sourceName)) return false;
            if (templates.Length == 0)
            {
                Plugin.Log.Warn("No native mirror template for " + sourceName + "; keeping opaque floor.");
                return false;
            }
            if (surface.sharedMaterials.Length != 1 || Vector3.Dot(surface.transform.up, Vector3.up) < .999f)
            {
                Plugin.Log.Warn("Mirror surface is not a single horizontal plane: " + sourceName);
                return false;
            }
            // Share one native renderer and plane between the player and runway mirrors.
            var template = templates.FirstOrDefault(m =>
                m.mirrorMaterial.name.Replace(" (Instance)", "") == sourceName) ?? templates[0];
            var renderer = Fields.Get<MirrorRendererSO>(templates[0], "_mirrorRenderer");
            var reflection = new Material(template.mirrorMaterial) { name = "Classic." + sourceName + ".Reflection" };
            // The reflection texture belongs to the renderer and is supplied in
            // OnWillRenderObject. Never retain or destroy the template's texture.
            reflection.SetTexture("_ReflectionTex", null);
            owned.Add(reflection);
            var fallback = surface.sharedMaterial;
            Vector3 position = surface.transform.position;
            surface.transform.position = new Vector3(position.x, SurfaceHeight, position.z);
            // MirrorRendererSO excludes layer 4 (Water) to prevent recursion.
            surface.gameObject.layer = 4;
            // Nice serialized its standalone runway plane inactive. Enable it
            // only after successfully finding a compatible mirror template.
            if(sourceName=="TrackMirror")surface.gameObject.SetActive(true);
            var mirror = surface.gameObject.AddComponent<Mirror>();
            Fields.Set(mirror, "_mirrorRenderer", renderer);
            Fields.Set(mirror, "_renderer", surface);
            Fields.Set(mirror, "_mirrorMaterial", reflection);
            Fields.Set(mirror, "_noMirrorMaterial", fallback);
            Plugin.Log.Info("Mirror connected: " + sourceName + "; follows native Mirrors quality.");
            return true;
        }
    }
}
