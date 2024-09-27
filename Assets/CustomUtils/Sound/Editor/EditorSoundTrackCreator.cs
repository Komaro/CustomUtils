using System.IO;
using UnityEditor;
using UnityEngine;

public static class EditorSoundTrackCreator {

    
    [MenuItem("Assets/Sound/Create Select Sound Asset to Track")]
    public static void SelectAudioClipToSoundTrack() {
        foreach (var audioClip in Selection.GetFiltered<AudioClip>(SelectionMode.Assets)) {
            var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(audioClip));
            Logger.TraceLog($"{audioClip.name} || {path}");
            var soundTrack = ScriptableObject.CreateInstance<SoundTrack>();
            if (soundTrack != null) {
                soundTrack.eventList = new[] { new SoundTrackEvent {
                    clip = audioClip,
                    loop = false,
                } };
                
                AssetDatabase.CreateAsset(soundTrack, $"{path}/{audioClip.name}.asset");
            }
        }
        
        AssetDatabase.SaveAssets();
    }
}
