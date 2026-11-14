// Adapted from the supplied OldNotes/Installers/CNGameInstaller.cs (ClassiNotes).
using System.Linq;
using SiraUtil.Extras;
using SiraUtil.Objects.Beatmap;
using SiraUtil.Objects.Beatmap.Debris;
using UnityEngine;
using Zenject;
using Object=UnityEngine.Object;

namespace OldSaber
{
    internal sealed class OldNotesInstaller : Installer
    {
        private readonly PluginConfig config;
        private readonly bool proMode;
        private Material sparkle;
        public OldNotesInstaller(PluginConfig config,[InjectOptional] GameplayCoreSceneSetupData setup)
        {this.config=config;proMode=setup!=null && setup.gameplayModifiers.proMode;}
        public override void InstallBindings()
        {
            if(!Plugin.Active || !config.OldNotes)return;
            Container.RegisterRedecorator(new BombNoteRegistration(DecorateBomb,-1));
            if(!OldNotesAssets.Data)return;
            sparkle=Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m=>m.name=="Sparkle");
            if(proMode)Container.RegisterRedecorator(new ProModeNoteRegistration(DecorateNote,-1));
            else Container.RegisterRedecorator(new BasicNoteRegistration(DecorateNote,-1));
            Container.RegisterRedecorator(new BurstSliderHeadNoteRegistration(DecorateNote,-1));
            Container.RegisterRedecorator(new BurstSliderNoteRegistration(DecorateBurst,-1));
            Container.RegisterRedecorator(new NormalNoteDebrisHDRegistration(DecorateDebris,-1));
            Container.RegisterRedecorator(new NormalNoteDebrisLWRegistration(DecorateDebris,-1));
            Container.RegisterRedecorator(new BurstSliderHeadNoteDebrisHDRegistration(DecorateDebris,-1));
            Container.RegisterRedecorator(new BurstSliderHeadNoteDebrisLWRegistration(DecorateDebris,-1));
            Container.RegisterRedecorator(new BurstSliderElementNoteHDRegistration(DecorateDebris,-1));
            Container.RegisterRedecorator(new BurstSliderElementNoteLWRegistration(DecorateDebris,-1));
            Object.Instantiate(OldNotesAssets.Data.ReflectionProbeContainer).AddComponent<OldNotesProbe>();
            Plugin.Log.Info("OldNotes enabled for this song: project shader, trails and classic debris.");
        }
        private void Trail(NoteController note,GameObject prefab)
        {
            if(!sparkle || note.GetComponent<OldNotesTrail>())return;
            bool active=note.gameObject.activeSelf;
            note.gameObject.SetActive(false);
            var trail=Object.Instantiate(prefab,note.transform,false);
            trail.GetComponent<ParticleSystemRenderer>().sharedMaterial=sparkle;
            note.gameObject.AddComponent<OldNotesTrail>().Init(note,trail.GetComponent<ParticleSystem>());
            note.gameObject.SetActive(active);
        }
        private void Material(Component note,string child,Material material)
        {
            var mesh=note.transform.Find(child);
            var renderer=mesh ? mesh.GetComponent<MeshRenderer>() : null;
            if(!renderer){Plugin.Log.Warn("OldNotes: missing "+child+" on "+note.name);return;}
            var state=renderer.GetComponent<OldNotesMaterialState>();
            if(!state)state=renderer.gameObject.AddComponent<OldNotesMaterialState>();
            state.Apply(renderer,material);
        }
        private void Note(NoteController note)
        {Trail(note,OldNotesAssets.Data.NoteTrailPS);Material(note,"NoteCube",OldNotesAssets.Data.NoteMaterial);}
        private GameNoteController DecorateNote(GameNoteController note){Note(note);return note;}
        private BurstSliderGameNoteController DecorateBurst(BurstSliderGameNoteController note){Note(note);return note;}
        private BombNoteController DecorateBomb(BombNoteController note){ClassicBomb.Apply(note);if(OldNotesAssets.Data)Trail(note,OldNotesAssets.Data.BombNoteTrailPS);return note;}
        private NoteDebris DecorateDebris(NoteDebris debris)
        {Material(debris,"NoteDebrisMesh",OldNotesAssets.Data.NoteDebrisMaterial);return debris;}
    }
}
