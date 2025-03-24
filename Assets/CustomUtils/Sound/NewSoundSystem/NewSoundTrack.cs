using System.Linq;
using UnityEngine;

public abstract class NewSoundTrack<TSoundTrackEvent> : ScriptableObject where TSoundTrackEvent : ISoundTrackEvent {
    
    public GlobalEnum<SoundTrackEnumAttribute> trackType = new();
    public TSoundTrackEvent[] events;
    
    public virtual void Unload() => events?.ForEach(x => x.Unload());

    protected virtual bool IsValidEventList(out SOUND_TRACK_ERROR error) {
        if (events == null) {
            error = SOUND_TRACK_ERROR.EVENT_LIST_NULL;
            return false;
        } 
        
        if (events.Length <= 0) {
            error = SOUND_TRACK_ERROR.EVENT_LIST_EMPTY;
            return false;
        }

        error = default;
        return true;
    }

    protected virtual bool IsValidSoundTrackEvent(ISoundTrackEvent eventTrack, out SOUND_TRACK_ERROR error) {
        if (eventTrack == null) {
            error = SOUND_TRACK_ERROR.EVENT_NULL;
            return false;
        }

        if (eventTrack.IsValid() == false) {
            error = SOUND_TRACK_ERROR.CLIP_INVALID;
            return false;
        }

        error = default;
        return true;
    }
    
    public virtual bool IsValid(out SOUND_TRACK_ERROR error) {
        try {
            if (IsValidEventList(out error) == false) {
                return false;
            }
            
            foreach (var track in events) {
                if (IsValidSoundTrackEvent(track, out error) == false) {
                    return false;
                }
            }
        } catch {
            error = SOUND_TRACK_ERROR.EVENT_EXCEPTION;
            return false;
        }
        
        error = default;
        return true;
    }
    
    public virtual bool IsValid() => IsValidEventList(out _) && events.All(x => IsValidSoundTrackEvent(x, out _));
}