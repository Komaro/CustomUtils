using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public static partial class EditorCommon {
    
    public const float DEFAULT_LABEL_WIDTH_FACTOR = 1.035f;
    public const float BUTTON_LABEL_WIDTH_FACTOR = 1.1f;
    public const float TOGGLE_FIT_AREA = 16f;
    
    public static readonly Vector2 TOGGLE_FIT_SIZE = new(16f, 16f);
    
    private static readonly Dictionary<string, GUILayoutOption> _widthCacheDic = new ();
    private static readonly TextGenerator _textGenerator = new ();
    
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
    public static void DrawVerticalSeparator(float leftSpace = 10f, float rightSpace = 10f) {
        GUILayout.Space(leftSpace);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true), GUILayout.Width(2f)), Color.gray);
        GUILayout.Space(rightSpace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFitButton(GUIContent buttonContent) => GUILayout.Button(buttonContent, Constants.Draw.FIT_X2_BUTTON, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawLabelTextField(string label, string text, float labelWidth = 120f) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(labelWidth));
            return EditorGUILayout.TextField(text, Constants.Draw.TEXT_FIELD);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawLabelTextField(string label, ref string text, float labelWidth = 120f) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(labelWidth));
            return text = EditorGUILayout.TextField(text, Constants.Draw.TEXT_FIELD);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawButtonTextField(string buttonText, ref string text, Action onClick = null, float buttonWidth = 0f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, buttonWidth == 0f ? GetCachedWidthOption(buttonText, Constants.Draw.LABEL, BUTTON_LABEL_WIDTH_FACTOR) : GUILayout.Width(buttonWidth))) {
                onClick?.Invoke();
            }

            return text = EditorGUILayout.TextField(text, Constants.Draw.TEXT_FIELD);
        }
    }
    
    public static string DrawLabelTextFieldWithRefresh(string label, ref string text, Action onClick = null) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, GetCachedWidthOption(label, Constants.Draw.BOLD_LABEL));
            if (DrawFitButton(Constants.Draw.REFRESH_ICON)) {
                GUI.FocusControl(null);
                onClick?.Invoke();
            }

            return text = EditorGUILayout.TextField(text, Constants.Draw.TEXT_FIELD, GUILayout.ExpandWidth(true));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawLabelButton(string label, GUIContent buttonContent, GUIStyle labelStyle = null) {
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, labelStyle ?? Constants.Draw.BOLD_LABEL, GetCachedWidthOption(label, Constants.Draw.BOLD_LABEL));
            return GUILayout.Button(buttonContent, Constants.Draw.FIT_X2_BUTTON, GUILayout.ExpandWidth(false));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelLinkButton(string label, string buttonText, Action<string> onClick = null, float labelWidth = 120f) {
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.BOLD_LABEL, GUILayout.Width(labelWidth));
            if (EditorGUILayout.LinkButton(buttonText, GetCachedWidthOption(buttonText, EditorStyles.linkLabel))) {
                onClick?.Invoke(buttonText);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelSelectableLabel(string label, string selectableLabel, float labelWidth = 120f) {
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.BOLD_LABEL, GUILayout.Width(labelWidth));
            EditorGUILayout.SelectableLabel(selectableLabel, Constants.Draw.LABEL, GUILayout.Height(22f), GUILayout.ExpandWidth(true));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawButtonPasswordField(string buttonText, string password, Action<string> onClick = null, float buttonWidth = 150f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, GUILayout.Width(buttonWidth))) {
                onClick?.Invoke(password);
            }
            
            return GUILayout.PasswordField(password, '*') ?? string.Empty;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (string, bool) DrawButtonPasswordField(string buttonText, string password, bool isShow, Action<string> onClick = null, float buttonWidth = 150f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, GUILayout.Width(buttonWidth))) {
                onClick?.Invoke(password);
            }

            return EditorGUILayout.Toggle(isShow, GUILayout.MaxWidth(15f))
                ? (EditorGUILayout.TextField(password) ?? string.Empty, true)
                : (GUILayout.PasswordField(password, '*') ?? string.Empty, false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawButtonPasswordField(string buttonText, ref string password, ref bool isShow, Action<string> onClick = null, float buttonWidth = 150f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, GUILayout.Width(buttonWidth))) {
                onClick?.Invoke(password);
            }

            isShow = EditorGUILayout.Toggle(isShow, GUILayout.MaxWidth(15f));
            password = isShow ? EditorGUILayout.TextField(password) ?? string.Empty : GUILayout.PasswordField(password, '*') ?? string.Empty;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFolderSelector(string buttonText, ref string targetDirectory, Action onSelect = null, float width = 120f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, GUILayout.Width(width))) {
                var selectDirectory = EditorUtility.OpenFolderPanel("폴더 선택", targetDirectory, string.Empty);
                if (string.IsNullOrEmpty(selectDirectory) == false) {
                    targetDirectory = selectDirectory;
                    onSelect?.Invoke();
                }
            }

            EditorGUILayout.TextField(targetDirectory, Constants.Draw.TEXT_FIELD);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFolderOpenSelector(string label, string buttonText, ref string targetDirectory, Action onSelect = null, float width = 120f) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(width));
            if (GUILayout.Button(Constants.Draw.FOLDER_OPEN_ICON, Constants.Draw.FIT_X2_BUTTON, GUILayout.Width(20f), GUILayout.Height(20f))) {
                EditorUtility.RevealInFinder(targetDirectory);
            }
            
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, GUILayout.ExpandWidth(false))) {
                var selectDirectory = EditorUtility.OpenFolderPanel("폴더 선택", targetDirectory, string.Empty);
                if (string.IsNullOrEmpty(selectDirectory) == false) {
                    targetDirectory = selectDirectory;
                    onSelect?.Invoke();
                }
            }
            
            EditorGUILayout.TextField(targetDirectory, Constants.Draw.TEXT_FIELD, GUILayout.ExpandWidth(true));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFileSelector(string buttonText, ref string targetPath, string extension = "", Action<string> onSelect = null, float buttonWidth = 120f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, GUILayout.Width(buttonWidth))) {
                var selectPath = EditorUtility.OpenFilePanel("대상 경로", targetPath, extension);
                if (string.IsNullOrEmpty(selectPath) == false) {
                    targetPath = selectPath;
                    onSelect?.Invoke(targetPath);
                }
            }

            EditorGUILayout.TextField(targetPath, Constants.Draw.TEXT_FIELD);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFileOpenSelector(ref string targetPath, string label, string buttonText, string extension = "", Action onSelect = null, float width = 0f) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, width == 0f ? GetCachedWidthOption(label, EditorStyles.boldLabel) : GUILayout.Width(width));
            if (GUILayout.Button(Constants.Draw.FOLDER_OPEN_ICON, Constants.Draw.FIT_X2_BUTTON, GUILayout.Width(20f), GUILayout.Height(20f))) {
                EditorUtility.RevealInFinder(targetPath);
            }
            
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, GUILayout.ExpandWidth(false))) {
                var selectPath = EditorUtility.OpenFilePanel("대상 경로", targetPath, extension);
                if (string.IsNullOrEmpty(selectPath) == false) {
                    targetPath = selectPath;
                    onSelect?.Invoke();
                }
            }

            EditorGUILayout.TextField(targetPath, Constants.Draw.TEXT_FIELD);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFitToggle(Rect position, ref bool isActive) => isActive = EditorGUI.Toggle(position.GetCenterRect(TOGGLE_FIT_SIZE), isActive);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFitToggle(ref bool toggle) => toggle = EditorGUILayout.Toggle(toggle, Constants.Draw.TOGGLE, GUILayout.MaxWidth(TOGGLE_FIT_AREA));


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
        using (new GUILayout.HorizontalScope(Constants.Draw.TOGGLE_HORIZONTAL_SCOPE)) {
            EditorGUILayout.LabelField(labelContent, Constants.Draw.LABEL, options);
            return EditorGUILayout.Toggle(toggle, Constants.Draw.TOGGLE, GUILayout.MaxWidth(TOGGLE_FIT_AREA));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelToggle(ref bool toggle, string label, Action onChange, params GUILayoutOption[] options) => DrawLabelToggle(ref toggle, new GUIContent(label, string.Empty), onChange, options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelToggle(ref bool toggle, GUIContent labelContent, Action onChange, params GUILayoutOption[] options) {
        using (new GUILayout.HorizontalScope(Constants.Draw.TOGGLE_HORIZONTAL_SCOPE)) {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(labelContent, Constants.Draw.LABEL, options);
            toggle = EditorGUILayout.Toggle(toggle, Constants.Draw.TOGGLE, GUILayout.MaxWidth(TOGGLE_FIT_AREA));
            if (EditorGUI.EndChangeCheck()) {
                onChange?.Invoke();
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DrawEnumPopup<T>(string label, T selected, params GUILayoutOption[] options) where T : Enum {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.ExpandWidth(false));
            return (T) EditorGUILayout.EnumPopup(selected, options);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DrawEnumPopup<T>(string label, ref T selected, float labelWidth = 120f, params GUILayoutOption[] options) where T : Enum {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(labelWidth));
            return selected = (T) EditorGUILayout.EnumPopup(selected, options);
        }
    }

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
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GUILayoutOption GetCachedWidthOption(string text) {
        if (_widthCacheDic.TryGetValue(text, out var option) == false) {
            option = GUILayout.Width(_textGenerator.GetPreferredWidth(text, EditorStyles.label.ToTextGenerationSettings()));
            _widthCacheDic.Add(text, option);
        }

        return option;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GUILayoutOption GetCachedWidthOption(string text, GUIStyle style, float factor = DEFAULT_LABEL_WIDTH_FACTOR) {
        if (_widthCacheDic.TryGetValue(text, out var option) == false) {
            option = GUILayout.Width(_textGenerator.GetPreferredWidth(text, style) * factor);
            _widthCacheDic.Add(text, option);
        }

        return option;
    }
}
