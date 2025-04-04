using System;
using System.Linq;
using UnityEngine;

namespace CustomUtils.Sound.NewSoundSystem {
    
    public abstract class SoundTrack<TSoundTrackEvent> : ScriptableObject where TSoundTrackEvent : ISoundTrackEvent {
    
        public GlobalEnum<SoundTrackEnumAttribute> trackType = new();
        public TSoundTrackEvent[] events;
    
        public virtual void Unload() {
            foreach (var soundTrackEvent in events) {
                soundTrackEvent.Unload();
            }
        }

        protected virtual bool IsValidEvents(out SOUND_TRACK_ERROR error) {
            events.ThrowIfNull(nameof(events));
            if (events.Length <= 0) {
                error = SOUND_TRACK_ERROR.EMPTY_EVENTS;
                return false;
            }

            error = default;
            return true;
        }

        protected virtual bool IsValidSoundTrackEvent(ISoundTrackEvent eventTrack, out SOUND_TRACK_ERROR error) {
            eventTrack.ThrowIfNull(nameof(eventTrack));
            if (eventTrack.IsValid() == false) {
                error = SOUND_TRACK_ERROR.INVALID_CLIP;
                return false;
            }

            error = default;
            return true;
        }
    
        public virtual bool IsValid(out SOUND_TRACK_ERROR error) {
            error = default;
            try {
                if (IsValidEvents(out error) == false) {
                    return false;
                }
            
                foreach (var track in events) {
                    if (IsValidSoundTrackEvent(track, out error) == false) {
                        return false;
                    }
                }
        
                return true;
            } catch(Exception ex) {
                Logger.TraceError(ex);
                error = SOUND_TRACK_ERROR.EXCEPTION_EVENT;
            }
            
            return false;
        }

        public virtual bool IsValid() => IsValidEvents(out _) && events.All(x => IsValidSoundTrackEvent(x, out _));
    }
}