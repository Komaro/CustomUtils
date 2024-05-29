using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

[CustomEditor(typeof(SoundTrack), true)]
public class EditorSoundInstance : Editor {

    private SoundTrack _track;

    private static AudioSource _previewSource;
    
    private static Object _audioMixer;
    
    private static int _currentGroup;
    private static string[] _groups;
    
    public void OnEnable() => _previewSource ??= EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
    
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (target is SoundTrack instance) {
            _track = instance;
        }
        
        _audioMixer = EditorGUILayout.ObjectField("Target Audio Mixer", _audioMixer, typeof(AudioMixer), true);
        if (_audioMixer is AudioMixer mixer) {
            _groups ??= mixer.FindMatchingGroups("Master").ConvertTo(x => x.name).ToArray();
            
            var selectGroup = 0;
            selectGroup = EditorGUILayout.Popup("Target Group", _currentGroup, _groups);
            if (selectGroup != _currentGroup) {
                _currentGroup = selectGroup;
                _previewSource.outputAudioMixerGroup = mixer.FindMatchingGroups(_groups[_currentGroup]).First();
            }
        }
        
        EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
        if (GUILayout.Button("Preview")) {
            _track.PreviewPlay(_previewSource);
        }

        if (GUILayout.Button("Stop")) {
            if (target) {
                _previewSource.Stop();
                _track.PreviewStop();
            }
        }
        
        EditorGUI.EndDisabledGroup();
    }
}
