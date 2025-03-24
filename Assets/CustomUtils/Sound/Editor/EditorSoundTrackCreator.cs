using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EditorSoundTrackCreator {

    [MenuItem("Assets/New Sound/Create Select Sound Asset to UnitySoundTrack")]
    public static void SelectAudioClipToUnitySoundTrack() {
        foreach (var audioClip in Selection.GetFiltered<AudioClip>(SelectionMode.Assets)) {
            var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(audioClip));
            if (string.IsNullOrEmpty(path) == false && ScriptableObjectUtil.TryCreateInstance<UnitySoundTrack>(out var soundTrack)) {
                soundTrack.events = new [] { new UnitySoundTrackEvent(audioClip) };
                AssetDatabase.CreateAsset(soundTrack, Path.Combine(path, audioClip.name.AutoSwitchExtension(Constants.Extension.ASSET)));
            }
        }
        
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/New Sound/Create Select Sound Asset to One UnitySoundTrack")]
    public static void SelectAudioClipToOneUnitySoundTrack() {
        if (AssetDatabaseUtil.TryGetAssetDirectory(Selection.objects.First(), out var path) && ScriptableObjectUtil.TryCreateInstance<UnitySoundTrack>(out var soundTrack) && Selection.GetFiltered<AudioClip>(SelectionMode.Assets).TryToArray(out var events, audioClip => new UnitySoundTrackEvent(audioClip))) {
            soundTrack.events = events;
            if (soundTrack.IsValid()) {
                AssetDatabase.CreateAsset(soundTrack, Path.Combine(path, Selection.objects.First().name).AutoSwitchExtension(Constants.Extension.ASSET));
                AssetDatabase.SaveAssets();
            } else {
                soundTrack.Unload();
                Object.Destroy(soundTrack);
            }
        }
    }
    
    // TODO. Obsolete
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
