using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

[CustomEditor(typeof(SoundTrackEx), true)]
public class EditorSoundTrackExInstance : Editor {
    
    private AudioSource _previewSource;
    private static Object _audioMixer;
    
    private static int _groupIndex;
    private static string[] _groups;

    private bool _isInitialize;
    
    public void OnEnable() {
        if (_isInitialize == false) {
            _previewSource = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
        }
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (target is SoundTrackEx track) {
            _audioMixer = EditorGUILayout.ObjectField("Target Audio Mixer", _audioMixer, typeof(AudioMixer), true);
            if (_audioMixer is AudioMixer mixer) {
                _groups ??= mixer.FindMatchingGroups("Master").ConvertTo(x => x.name).ToArray();

                using (var scope = new EditorGUI.ChangeCheckScope()) {
                    _groupIndex = EditorGUILayout.Popup("Target Group", _groupIndex, _groups);
                    if (scope.changed) {
                        _previewSource.outputAudioMixerGroup = mixer.FindMatchingGroups(_groups[_groupIndex]).First();
                    }
                }
            }
            
            EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
            if (GUILayout.Button("Preview")) {
                track.PreviewPlay(_previewSource);
            }

            if (GUILayout.Button("Stop")) {
                if (target) {
                    _previewSource.Stop();
                    track.PreviewStop();
                }
            }
            
            EditorGUI.EndDisabledGroup();
        }
    }
}
