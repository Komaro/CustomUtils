using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

[CustomEditor(typeof(UnitySoundTrack), true)]
public class EditorUnitySoundTrackInstance : Editor {

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
        if (target is UnitySoundTrack track) {
            using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
                _audioMixer = EditorGUILayout.ObjectField("Target Audio Mixer", _audioMixer, typeof(AudioMixer), true);
                if (_audioMixer is AudioMixer mixer) {
                    _groups ??= mixer.FindMatchingGroups("Master").ToArray(x => x.name);
                    using (var scope = new EditorGUI.ChangeCheckScope()) {
                        _groupIndex = EditorGUILayout.Popup("Target Group", _groupIndex, _groups);
                        if (scope.changed) {
                            _previewSource.outputAudioMixerGroup = mixer.FindMatchingGroups(_groups[_groupIndex]).First();
                        }
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);

            using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("Preview")) {
                        track.PreviewPlay(_previewSource);
                        Repaint();
                    }

                    if (_previewSource.isPlaying) {
                        if (GUILayout.Button("Pause")) {
                            _previewSource.Pause();
                            Repaint();
                        }
                    } else {
                        if (GUILayout.Button("UnPause")) {
                            _previewSource.UnPause();
                            Repaint();
                        }
                    }
                    
                    if (GUILayout.Button("Stop")) {
                        _previewSource.Stop();
                        track.PreviewStop();
                        Repaint();
                    }
                }

                if (_previewSource.IsValid()) {
                    EditorCommon.DrawInteractionProgressBar($"{_previewSource.time:F} / {_previewSource.clip.length:F}", _previewSource.time, _previewSource.clip.length, OnProgressBarEvent);
                    if (_previewSource.isPlaying) {
                        Repaint();
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
        }
    }

    private void OnProgressBarEvent(EventType type, float progress) {
        switch (type) {
            case EventType.MouseDown:
                if (_previewSource.isPlaying) {
                    _previewSource.Pause();
                }

                _previewSource.time = Mathf.Lerp(0, _previewSource.clip.length, progress);

                Repaint();
                Event.current.Use();
                break;
            case EventType.MouseUp:
                if (_previewSource.isPlaying == false) {
                    _previewSource.time = Mathf.Lerp(0, _previewSource.clip.length, progress);
                    if (_previewSource.time <= 0) {
                        _previewSource.Play();
                    } else {
                        _previewSource.UnPause();
                    }
                }

                Repaint();
                Event.current.Use();
                break;
            case EventType.MouseDrag:
                _previewSource.time = Mathf.Lerp(0, _previewSource.clip.length, progress);

                Repaint();
                Event.current.Use();
                break;
        }
    }
}