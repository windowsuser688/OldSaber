using UnityEngine;
using UnityEngine.Rendering;

namespace OldSaber
{
    internal static class LaserVisuals
    {
        internal static void Create(Transform parent, TubeBloomPrePassLight tube,
            ParametricBoxController boxTemplate, TubeBloomPrePassLight glowTemplate, int sourceId)
        {
            // Side lasers never receive an opaque parametric box, even when a
            // glow template is unavailable. That mesh caused the dark silhouette.
            if (VisualPolicy.UsesGlowOnly(sourceId))
            {
                if (!glowTemplate) return; // Native volumetric bloom remains active.
                var template = Fields.Get<Parametric3SliceSpriteController>(glowTemplate, "_dynamic3SliceSprite");
                var glow = new GameObject("ClassicLaserGlow");
                glow.transform.SetParent(parent, false);
                glow.layer = parent.gameObject.layer;
                glow.AddComponent<MeshFilter>().sharedMesh = template.GetComponent<MeshFilter>().sharedMesh;
                var renderer = glow.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = template.GetComponent<MeshRenderer>().sharedMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var sprite = glow.AddComponent<Parametric3SliceSpriteController>();
                Fields.Set(sprite, "_widthMultiplier", Fields.Get<float>(template, "_widthMultiplier"));
                Fields.Set(tube, "_dynamic3SliceSprite", sprite);
                Fields.Set(tube, "_forceUseBakedGlow", true);
                Fields.Set(tube, "_bakedGlowWidthScale", Fields.Get<float>(glowTemplate, "_bakedGlowWidthScale"));
                glow.AddComponent<LaserCoreVisibility>().Initialize(tube, renderer);
                return;
            }

            var core = new GameObject("ClassicLaserCore");
            core.transform.SetParent(parent, false);
            core.layer = parent.gameObject.layer;
            core.AddComponent<MeshFilter>().sharedMesh = boxTemplate.GetComponent<MeshFilter>().sharedMesh;
            var coreRenderer = core.AddComponent<MeshRenderer>();
            coreRenderer.sharedMaterials = boxTemplate.GetComponent<MeshRenderer>().sharedMaterials;
            coreRenderer.shadowCastingMode = ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;
            var box = core.AddComponent<ParametricBoxController>();
            Fields.Set(box, "_meshRenderer", coreRenderer);
            Fields.Set(tube, "_parametricBoxController", box);
            core.AddComponent<LaserCoreVisibility>().Initialize(tube, coreRenderer);
        }
    }
}
