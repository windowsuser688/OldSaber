using System;
using System.IO;
using System.Reflection;
using ClassiNotes.DataStores;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace OldSaber
{
    internal static class OldNotesAssets
    {
        internal static PluginDataSO Data;
        internal static void Load()
        {
            if(Data)return;
            AssetBundle bundle=null;
            Stream stream=null;
            try
            {
                // Unity reads this stream lazily during LoadAsset and Unload.
                // Keep it readable until the bundle has been unloaded.
                stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.OldNotes.bundle");
                if(stream==null)throw new InvalidOperationException("Embedded OldNotes bundle is missing.");
                bundle=AssetBundle.LoadFromStream(stream);
                if(!bundle)throw new InvalidOperationException("OldNotes asset bundle could not be loaded.");
                Data=bundle.LoadAsset<PluginDataSO>("assets/plugindataso.asset");
                if(!Data || !Data.NoteMaterial || !Data.NoteDebrisMaterial || !Data.ReflectionProbeContainer || !Data.NoteTrailPS || !Data.BombNoteTrailPS)
                    throw new InvalidOperationException("OldNotes bundle is missing required assets.");
                // Apply once when the bundle is loaded, so restarts do not compound it.
                foreach(var noteMaterial in new[]{Data.NoteMaterial,Data.NoteDebrisMaterial})
                    if(noteMaterial.HasProperty("_FinalColorMul"))
                        noteMaterial.SetFloat("_FinalColorMul",noteMaterial.GetFloat("_FinalColorMul")*1.25f);
                Plugin.Log.Info("OldNotes project assets loaded: classic note/debris shader, trails and reflection probe.");
            }
            catch(Exception e){Data=null;Plugin.Log.Error("OldNotes assets unavailable; native notes retained. "+e);}
            finally
            {
                try {if(bundle)bundle.Unload(false);}
                finally {stream?.Dispose();}
            }
        }
        internal static void Restore()
        {
            foreach(var state in Resources.FindObjectsOfTypeAll<OldNotesMaterialState>())
            {state.Restore();Object.Destroy(state);}
            foreach(var trail in Resources.FindObjectsOfTypeAll<OldNotesTrail>())
            {trail.Restore();Object.Destroy(trail);}
            foreach(var probe in Resources.FindObjectsOfTypeAll<OldNotesProbe>())Object.Destroy(probe.gameObject);
        }
    }
    public sealed class OldNotesProbe : MonoBehaviour { }
    public sealed class OldNotesMaterialState : MonoBehaviour
    {
        [SerializeField] private MeshRenderer target;
        [SerializeField] private Material[] original;
        [SerializeField] private Material replacement;
        [SerializeField] private ReflectionProbeUsage probeUsage;
#pragma warning disable CS0612
        [SerializeField] private ConditionalMaterialSwitcher switcher;
#pragma warning restore CS0612
        [SerializeField] private bool switcherEnabled;
        [SerializeField] private Material originalMaterial0, originalMaterial1;
        internal void Apply(MeshRenderer renderer,Material material,bool reflections=true)
        {
            if(target)return;
            target=renderer;original=renderer.sharedMaterials;replacement=material;probeUsage=renderer.reflectionProbeUsage;
#pragma warning disable CS0612
            switcher=renderer.GetComponent<ConditionalMaterialSwitcher>();
#pragma warning restore CS0612
            if(switcher)
            {
                switcherEnabled=switcher.enabled;
                originalMaterial0=Fields.Get<Material>(switcher,"_material0");
                originalMaterial1=Fields.Get<Material>(switcher,"_material1");
                // Disabled MonoBehaviours still receive Awake on instantiated notes.
                // Both quality branches must retain the project material on clones.
                Fields.Set(switcher,"_material0",material);
                Fields.Set(switcher,"_material1",material);
                switcher.enabled=false;
            }
            renderer.sharedMaterial=material;
            renderer.reflectionProbeUsage=reflections?ReflectionProbeUsage.Simple:ReflectionProbeUsage.Off;
        }
        internal void Restore()
        {
            if(target)
            {
                if(target.sharedMaterial==replacement)target.sharedMaterials=original;
                target.reflectionProbeUsage=probeUsage;
            }
            if(switcher)
            {
                Fields.Set(switcher,"_material0",originalMaterial0);
                Fields.Set(switcher,"_material1",originalMaterial1);
                switcher.enabled=switcherEnabled;
            }
            target=null;switcher=null;
        }
        private void OnDestroy(){Restore();}
    }
}
