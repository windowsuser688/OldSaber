using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace OldSaber
{
    internal static class ClassicBomb
    {
        private static Mesh mesh;
        private static Material material;
        private static JObject profile;
        internal static void Apply(BombNoteController bomb)
        {
            try
            {
                if(!mesh)
                {
                    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.ClassicBomb.json"))
                    using(var reader=new StreamReader(stream))profile=JObject.Parse(reader.ReadToEnd());
                    mesh=ClassicStage.ReadMesh(profile,0,0);
                    mesh.name="OldSaber.Bomb.0.10.1";
                    mesh.hideFlags=HideFlags.DontUnloadUnusedAsset;
                }
                // Choose the volumetric bomb body, excluding particle/glow quads.
                var filters=bomb.GetComponentsInChildren<MeshFilter>(true).Where(f=>
                    f.sharedMesh && f.GetComponent<MeshRenderer>() &&
                    Mathf.Min(f.sharedMesh.bounds.size.x,Mathf.Min(f.sharedMesh.bounds.size.y,f.sharedMesh.bounds.size.z))>0.01f).ToArray();
                if(filters.Length!=1)
                {
                    Plugin.Log.Warn("Classic bomb: expected one body mesh on "+bomb.name+", found "+filters.Length+"; native mesh retained.");
                    return;
                }
                var state=filters[0].GetComponent<ClassicBombState>();
                if(!state)state=filters[0].gameObject.AddComponent<ClassicBombState>();
                state.Apply(filters[0],mesh);
                if(OldNotesAssets.Data)
                {
                    if(!material)
                    {
                        material=new Material(OldNotesAssets.Data.NoteMaterial);
                        material.name="OldSaber.ClassicBomb";
                        material.hideFlags=HideFlags.DontUnloadUnusedAsset;
                        // Preserve the ported shader's render state; transfer only supported
                        // appearance properties from the 0.10.1 bomb material.
                        foreach(string property in new[]{"_Glossiness","_Metallic","_FinalColorMul","_RimScale","_RimOffset","_EnableRimDim","_CutoutTexScale"})
                            if(material.HasProperty(property))material.SetFloat(property,(float)profile["material"]["floats"][property]);
                        foreach(string property in new[]{"_Color","_RimColor"})
                        {
                            var c=profile["material"]["colors"][property];
                            if(material.HasProperty(property))material.SetColor(property,new Color((float)c["r"],(float)c["g"],(float)c["b"],1f));
                        }
                        // The legacy black base depended on a red rim-light pass that
                        // the ported note shader lacks. Use a slightly lifted neutral black so the body remains visible.
                        material.SetColor("_Color",new Color(.14f,.14f,.14f,1f));
                        foreach(string property in new[]{"_Glossiness","_Metallic","_GlossyReflections","_SpecularHighlights"})
                            if(material.HasProperty(property))material.SetFloat(property,0);
                        material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                        material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                        material.DisableKeyword("_ENABLE_RIM_DIM");
                        if(material.HasProperty("_EnableRimDim"))material.SetFloat("_EnableRimDim",0);
                        material.DisableKeyword("_ENABLE_PLANE_CUT");
                        if(material.HasProperty("_EnablePlaneCut"))material.SetFloat("_EnablePlaneCut",0);
                    }
                    var renderer=filters[0].GetComponent<MeshRenderer>();
                    var appearance=renderer.GetComponent<OldNotesMaterialState>();
                    if(!appearance)appearance=renderer.gameObject.AddComponent<OldNotesMaterialState>();
                    appearance.Apply(renderer,material,false);
                }
                Plugin.Log.Info("Classic 0.10.1 bomb mesh applied: 214 vertices, 100 triangles.");
            }
            catch(Exception e){Plugin.Log.Error("Classic bomb mesh failed; native mesh retained. "+e);}
        }
        internal static void Restore()
        {
            foreach(var state in Resources.FindObjectsOfTypeAll<ClassicBombState>())
            {state.Restore();Object.Destroy(state);}
            if(material)
            {
                foreach(var state in Resources.FindObjectsOfTypeAll<OldNotesMaterialState>())state.Restore();
                Object.Destroy(material);material=null;
            }
            if(mesh)Object.Destroy(mesh);
            mesh=null;
        }
    }
    public sealed class ClassicBombState : MonoBehaviour
    {
        [SerializeField] private MeshFilter target;
        [SerializeField] private Mesh original,replacement;
        internal void Apply(MeshFilter filter,Mesh mesh)
        {
            if(!target){target=filter;original=filter.sharedMesh;}
            replacement=mesh;target.sharedMesh=mesh;
        }
        internal void Restore()
        {
            if(target && target.sharedMesh==replacement)target.sharedMesh=original;
            target=null;
        }
        private void OnDestroy(){Restore();}
    }
}
