// Adapted from OldNotes/Components/TrailNoteVisuals.cs.
using UnityEngine;
namespace OldSaber
{
    public sealed class OldNotesTrail : MonoBehaviour, INoteControllerDidInitEvent,
        INoteControllerNoteDidStartJumpEvent, INoteControllerNoteDidStartDissolvingEvent
    {
        [SerializeField] private NoteController note;
        [SerializeField] private ParticleSystem particles;
        private NoteMovement movement;
        private bool subscribed, onFloor;
        internal void Init(NoteController controller,ParticleSystem system)
        {note=controller;particles=system;}
        private void Awake()
        {
            if(!note || !particles)return;
            movement=Fields.Get<NoteMovement>(note,"_noteMovement");
            note.didInitEvent.Add(this);
            note.noteDidStartJumpEvent.Add(this);
            note.noteDidStartDissolvingEvent.Add(this);
            movement.didInitEvent+=MovementInit;
            movement.noteDidStartJumpEvent+=Jump;
            movement.noteDidFinishJumpEvent+=Finish;
            subscribed=true;Emit(false);
        }
        private void Emit(bool value)
        {if(particles){var emission=particles.emission;emission.enabled=value;}}
        public void HandleNoteControllerDidInit(NoteControllerBase controller)
        {
            if(!particles)return;
            particles.Clear();var main=particles.main;main.startColor=Color.white;
        }
        public void HandleNoteControllerNoteDidStartJump(NoteController controller){Jump();}
        public void HandleNoteControllerNoteDidStartDissolving(NoteControllerBase controller,float duration){Finish();}
        private void MovementInit(){onFloor=true;Emit(false);}
        private void Jump(){onFloor=false;Emit(false);}
        private void Finish(){onFloor=false;Emit(false);}
        private void Update(){Emit(Plugin.Active && onFloor && transform.localPosition.z<30f);}
        private void OnDisable(){Finish();if(particles)particles.Clear();}
        internal void Restore()
        {
            if(subscribed)
            {
                if(note)
                {
                    note.didInitEvent.Remove(this);note.noteDidStartJumpEvent.Remove(this);
                    note.noteDidStartDissolvingEvent.Remove(this);
                }
                if(movement)
                {
                    movement.didInitEvent-=MovementInit;movement.noteDidStartJumpEvent-=Jump;
                    movement.noteDidFinishJumpEvent-=Finish;
                }
                subscribed=false;
            }
            if(particles){Destroy(particles.gameObject);particles=null;}
            enabled=false;
        }
        private void OnDestroy(){Restore();}
    }
}
