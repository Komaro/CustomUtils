using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.066f, 0.134f, 0.244f)]
[TrackClipType(typeof(CommandEventPlayable))]
[DisplayName("Command/CommandTrack")]
public class CommandEventTrack : TrackAsset {

    public SoundEventMixerBehaviour Template = new();

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
        foreach (var clip in GetClips()) {
            if (clip.asset is CommandEventPlayable playableAsset) {
                playableAsset.OwningClip = clip;
            }
        }
        
        return ScriptPlayable<SoundEventMixerBehaviour>.Create(graph, Template, inputCount);
    }
}

[Serializable]
public class SoundEventMixerBehaviour : PlayableBehaviour {

    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
        var inputCount = playable.GetInputCount();
        var time = (float)playable.GetGraph().GetRootPlayable(0).GetTime();
        for (int i = 0; i < inputCount; i++) {
            var inputPlayable = (ScriptPlayable<CommandEventPlayableBehavior>)playable.GetInput(i);
            var input = inputPlayable.GetBehaviour();
            input.UpdateBehaviour(time);
        }
    }
}
