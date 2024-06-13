using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public static partial class EditorCommon {
    
    public static void OpenCheckDialogue(string title, string message, string okText = "확인", string cancelText = "취소", Action ok = null, Action cancel = null) {
        if (EditorUtility.DisplayDialog(title, message, okText, cancelText)) {
            ok?.Invoke();
        } else {
            cancel?.Invoke();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawSeparator(float topSpace = 10f, float bottomSpace = 10f) {
        GUILayout.Space(topSpace);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2), Color.gray);
        GUILayout.Space(bottomSpace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawLabelTextField(string label, string text, float labelWidth = 100f) {
        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label(label, Constants.Editor.FIELD_TITLE_STYLE, GUILayout.Width(labelWidth));
            return GUILayout.TextField(text);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawButtonTextField(string buttonText, string text, Action<string> onClick = null, float buttonWidth = 150f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, GUILayout.Width(buttonWidth))) {
                onClick?.Invoke(text);
            }

            return GUILayout.TextField(text);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawButtonPasswordField(string buttonText, string password, Action<string> onClick = null, float buttonWidth = 150f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, GUILayout.Width(buttonWidth))) {
                onClick?.Invoke(password);
            }

            return GUILayout.PasswordField(password, '*') ?? string.Empty;
        }
    }

    /// <summary>
    /// Show Plain TextField Toggle
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (string, bool) DrawButtonPasswordField(string buttonText, string password, bool isShow, Action<string> onClick = null, float buttonWidth = 150f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, GUILayout.Width(buttonWidth))) {
                onClick?.Invoke(password);
            }

            return EditorGUILayout.Toggle(isShow, GUILayout.MaxWidth(15f))
                ? (GUILayout.TextField(password) ?? string.Empty, true)
                : (GUILayout.PasswordField(password, '*') ?? string.Empty, false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawButtonPasswordField(string buttonText, ref string password, ref bool isShow, Action<string> onClick = null, float buttonWidth = 150f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, GUILayout.Width(buttonWidth))) {
                onClick?.Invoke(password);
            }

            isShow = EditorGUILayout.Toggle(isShow, GUILayout.MaxWidth(15f));
            password = isShow ? GUILayout.TextField(password) ?? string.Empty : GUILayout.PasswordField(password, '*') ?? string.Empty;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawFolderSelector(string text, string targetDirectory, Action<string> onSelect = null, float width = 120f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(text, GUILayout.Width(width))) {
                var selectDirectory = EditorUtility.OpenFolderPanel("대상 폴더", targetDirectory, string.Empty);
                if (string.IsNullOrEmpty(selectDirectory) == false) {
                    targetDirectory = selectDirectory;
                }

                onSelect?.Invoke(targetDirectory);
            }

            return GUILayout.TextField(targetDirectory);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawLabelToggle(bool toggle, string label, float labelWidth = 300f) => DrawLabelToggle(toggle, new GUIContent(label), GUILayout.Width(labelWidth));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelToggle(ref bool toggle, string label, float labelWidth = 300f) => toggle = DrawLabelToggle(toggle, new GUIContent(label), GUILayout.Width(labelWidth));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawLabelToggle(bool toggle, string label, string tooltip, float labelWidth = 300f) => DrawLabelToggle(toggle, new GUIContent(label, tooltip), GUILayout.Width(labelWidth));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelToggle(ref bool toggle, string label, string tooltip, float labelWidth = 300f) => toggle = DrawLabelToggle(toggle, new GUIContent(label, tooltip), GUILayout.Width(labelWidth));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawLabelToggle(bool toggle, GUIContent labelContent, params GUILayoutOption[] options) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(labelContent, options);
            return EditorGUILayout.Toggle(toggle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DrawEnumPopup<T>(string label, T selected, params GUILayoutOption[] options) where T : Enum => (T) EditorGUILayout.EnumPopup(label, selected, options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetGUITooltip(string tooltip, bool condition) => GUI.tooltip = condition && Event.current.type == EventType.Repaint ? tooltip : string.Empty;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DrawTopToolbar(int index, Action<int> onChange = null, params string[] texts) {
        using (new EditorGUILayout.HorizontalScope()) {
            GUILayout.FlexibleSpace();
            var selectIndex = GUILayout.Toolbar(index, texts, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
            GUILayout.FlexibleSpace();
            
            if (selectIndex != index) {
                onChange?.Invoke(selectIndex);
            }
            
            return selectIndex;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTopToolbar(ref int index, Action<int> onChange = null, params string[] texts) => index = DrawTopToolbar(index, onChange, texts);
}
