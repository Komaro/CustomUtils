using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CommandEventPlayable : PlayableAsset, ITimelineClipAsset {

    [NonSerialized]
    public CommandEventPlayableBehavior Template = new();
    [NonSerialized]
    public TimelineClip OwningClip;
    public ClipCaps clipCaps => ClipCaps.None;

    public string command;
    public string exitCommand;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
        var playable = ScriptPlayable<CommandEventPlayableBehavior>.Create(graph, Template);
        var behaviour = playable.GetBehaviour();
        behaviour.clip = OwningClip;
        behaviour.command = command;
        behaviour.exitCommand = exitCommand;
        return playable;
    }
}

[Serializable]
public class CommandEventPlayableBehavior : PlayableBehaviour {

    public string command;
    public string exitCommand;

    [NonSerialized]
    public TimelineClip clip;
    
    private bool _isPlaying;

    public void UpdateBehaviour(float time) {
        if (time >= clip.start && time < clip.end) {
            OnEnter();
        } else {
            OnExit();
        }
    }

    public void OnEnter() {
        if (_isPlaying == false) {
            _isPlaying = true;
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                Logger.TraceLog($"Execute || {command}", Color.green);
            }
#endif
            if (Application.isPlaying && string.IsNullOrEmpty(command) == false) {
                Command.Create(command)?.ExecuteAsync();
            }
        }
    }

    public void OnExit() {
        if (_isPlaying) {
            _isPlaying = false;
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                Logger.TraceError($"Exit Execute || {exitCommand}", Color.magenta);
            }
#endif
            if (Application.isPlaying && string.IsNullOrEmpty(exitCommand) == false) {
                Command.Create(exitCommand)?.ExecuteAsync();
            }
        }
    }
}
