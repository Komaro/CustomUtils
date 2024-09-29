using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public static partial class EditorCommon {
    
    private static readonly Dictionary<string, GUILayoutOption> _widthCacheDic = new ();

    public static void ShowCheckDialogue(string title, string message, string okText = "확인", string cancelText = "취소", Action ok = null, Action cancel = null) {
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
    public static string DrawLabelTextField(string label, string text, float labelWidth = 120f, GUIStyle textFieldStyle = null) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(labelWidth));
            return EditorGUILayout.TextField(text, textFieldStyle ?? Constants.Draw.TEXT_FIELD);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelTextField(string label, ref string text, float labelWidth = 120f) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(labelWidth));
            text = EditorGUILayout.TextField(text, Constants.Draw.TEXT_FIELD);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawButtonTextField(string buttonText, ref string text, Action onClick = null, float buttonWidth = 0f) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button(buttonText, Constants.Draw.BUTTON, buttonWidth <= 0f ? GetCachedFixWidthOption(buttonText, Constants.Draw.BUTTON) : GUILayout.Width(buttonWidth))) {
                onClick?.Invoke();
            }

            return text = EditorGUILayout.TextField(text, Constants.Draw.TEXT_FIELD);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawLabelTextFieldWithRefresh(string label, string text, Action onClick = null) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GetCachedFixWidthOption(label, Constants.Draw.TITLE_STYLE));
            if (DrawFitButton(Constants.Draw.REFRESH_ICON)) {
                GUI.FocusControl(null);
                onClick?.Invoke();
            }

            return EditorGUILayout.TextField(text, Constants.Draw.TEXT_FIELD, GUILayout.ExpandWidth(true));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawLabelTextFieldWithRefresh(string label, ref string text, Action onClick = null) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GetCachedFixWidthOption(label, Constants.Draw.TITLE_STYLE));
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
            EditorGUILayout.LabelField(label, labelStyle ?? Constants.Draw.BOLD_LABEL, GetCachedFixWidthOption(label, Constants.Draw.BOLD_LABEL));
            return DrawFitButton(buttonContent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelLinkButton(string label, string buttonText, Action<string> onClick = null, float labelWidth = 120f) {
        using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.BOLD_LABEL, GUILayout.Width(labelWidth));
            if (EditorGUILayout.LinkButton(buttonText, GetCachedFixWidthOption(buttonText, EditorStyles.linkLabel))) {
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
    public static void DrawFolderOpenSelector(string label, string buttonText, ref string targetDirectory, Action onSelect = null) {
        using (new GUILayout.HorizontalScope()) {
            DrawFitLabel(label, Constants.Draw.TITLE_STYLE);
            DrawFolderOpenSelector(buttonText, ref targetDirectory, onSelect);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFolderOpenSelector(string label, string buttonText, ref string targetDirectory, float width, Action onSelect = null) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(width));
            DrawFolderOpenSelector(buttonText, ref targetDirectory, onSelect);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFolderOpenSelector(string buttonText, ref string targetDirectory, Action onSelect = null) {
        if (DrawFixButton(Constants.Draw.FOLDER_OPEN_ICON)) {
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
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, width == 0f ? GetCachedFixWidthOption(label, EditorStyles.boldLabel) : GUILayout.Width(width));
            if (DrawFixButton(Constants.Draw.FOLDER_OPEN_ICON)) {
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
            return EditorGUILayout.Toggle(toggle, Constants.Draw.TOGGLE, GetCachedFixWidthOption(Constants.Draw.TOGGLE));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelToggle(ref bool toggle, string label, Action onChange, params GUILayoutOption[] options) => DrawLabelToggle(ref toggle, new GUIContent(label, string.Empty), onChange, options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLabelToggle(ref bool toggle, GUIContent labelContent, Action onChange, params GUILayoutOption[] options) {
        using (new GUILayout.HorizontalScope(Constants.Draw.TOGGLE_HORIZONTAL_SCOPE)) {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(labelContent, Constants.Draw.LABEL, options);
            toggle = EditorGUILayout.Toggle(toggle, Constants.Draw.TOGGLE, GetCachedFixWidthOption(Constants.Draw.TOGGLE));
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
    public static void DrawEnumPopup<T>(string label, ref T selected, float labelWidth = 120f, params GUILayoutOption[] options) where T : Enum {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(labelWidth));
            selected = (T) EditorGUILayout.EnumPopup(selected, options); 
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DrawEnumPopup<T>(string label, T selected, float labelWidth = 120f, params GUILayoutOption[] options) where T : Enum {
        using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField(label, Constants.Draw.TITLE_STYLE, GUILayout.Width(labelWidth));
            return (T) EditorGUILayout.EnumPopup(selected, options);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DrawTopToolbar(int index, Action<int> onChange = null, params string[] texts) {
        using (new EditorGUILayout.HorizontalScope()) {
            GUILayout.FlexibleSpace();
            var selectIndex = GUILayout.Toolbar(index, texts, Constants.Draw.LARGE_BUTTON, GUI.ToolbarButtonSize.FitToContents);
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
    public static bool DrawToggleBox(IEnumerable<ToggleDraw> toggleDraws, Action onChange = null) {
        EditorGUI.BeginChangeCheck();
        foreach (var defineSymbol in toggleDraws) {
            if (defineSymbol.HasHeader()) {
                DrawFitLabel(defineSymbol.header);
            }

            DrawLabelToggle(ref defineSymbol.isActive, defineSymbol.name, 150f);
        }
        
        if (EditorGUI.EndChangeCheck()) {
            onChange?.Invoke();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DrawWideTextArea(string label, string text, float height = 100f) {
        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label(label, Constants.Draw.TITLE_STYLE, GetCachedFixWidthOption(label, Constants.Draw.TITLE_STYLE), GUILayout.Height(height));
            return EditorGUILayout.TextArea(text, GUILayout.Height(height), GUILayout.ExpandWidth(true));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawWideTextArea(string label, ref string text, float height = 100f) {
        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label(label, Constants.Draw.TITLE_STYLE, GetCachedFixWidthOption(label, Constants.Draw.TITLE_STYLE), GUILayout.Height(height));
            text = EditorGUILayout.TextArea(text, GUILayout.Height(height), GUILayout.ExpandWidth(true));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DrawCursorNavigator(string label, int cursor, int max) {
        using (new GUILayout.HorizontalScope()) {
            EditorGUI.BeginDisabledGroup(cursor <= 0);
            if (GUILayout.Button("<", GUILayout.Height(30f))) {
                cursor = Math.Max(0, cursor - 1);
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.Label($"{label} [{cursor + 1} / {max}]", Constants.Draw.TITLE_STYLE, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Width(200f));

            EditorGUI.BeginDisabledGroup(max <= 1 || cursor >= max - 1);
            if (GUILayout.Button(">", GUILayout.Height(30f))) {
                cursor = Math.Min(max, cursor + 1);
            }
            EditorGUI.EndDisabledGroup();
        }

        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DrawCursorRecursiveNavigator(string label, int cursor, int max) {
        using (new GUILayout.HorizontalScope()) {
            if (GUILayout.Button("<", GUILayout.Height(30f))) {
                cursor = cursor <= 0 ? max : cursor - 1;
            }
                        
            GUILayout.Label($"{label} [{cursor + 1} / {max}]", Constants.Draw.TITLE_STYLE, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Width(200f));

            if (GUILayout.Button(">", GUILayout.Height(30f))) {
                cursor = (cursor + 1) % max;
            }
        }
        
        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawInteractionProgressBar(string text, float value, float maxValue, Action<EventType, float> onEvent = null) {
        using (new EditorGUILayout.HorizontalScope()) {
            var position = EditorGUILayout.GetControlRect(false, 50f);
            EditorGUI.DrawRect(position, Color.gray);

            var progress = value / maxValue;
            var innerPosition = position;
            innerPosition.width = Mathf.Lerp(0, position.width, progress);
            EditorGUI.DrawRect(innerPosition, Constants.Colors.INNER_PROGRESS_BAR);

            EditorGUI.LabelField(position, text, Constants.Draw.BOLD_CENTER_LABEL);

            if (position.Contains(Event.current.mousePosition) && Event.current.type.IsProcessableEvent()) {
                switch (Event.current.type) {
                    case EventType.MouseDown:
                    case EventType.MouseDrag:
                        progress = Mathf.InverseLerp(position.x, position.xMax, Event.current.mousePosition.x);
                        innerPosition.width = Mathf.Lerp(0, position.width, progress);
                        EditorGUI.DrawRect(innerPosition, Constants.Colors.INNER_PROGRESS_BAR);
                        break;
                }

                onEvent?.Invoke(Event.current.type, progress);
            }
        }
    }
    
    #region [Fit]
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFitLabel(string label, GUIStyle style = null) => EditorGUILayout.LabelField(label, style ?? Constants.Draw.LABEL, GetCachedFixWidthOption(label, style ?? Constants.Draw.LABEL));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFitButton(string buttonText, GUIStyle style = null) => GUILayout.Button(buttonText, style ?? Constants.Draw.BUTTON, GetCachedFixWidthOption(buttonText, style ?? Constants.Draw.BUTTON));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFitButton(GUIContent buttonContent, GUIStyle style = null) => GUILayout.Button(buttonContent, style ?? Constants.Draw.FIT_X2_BUTTON, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFitToggle(Rect position, bool toggle, bool isDisabled = true) {
        using (new EditorGUI.DisabledGroupScope(isDisabled)) {
            return EditorGUI.Toggle(position.GetCenterRect(Constants.Draw.TOGGLE.CalcSize(GUIContent.none)), toggle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFitToggle(Rect position, ref bool toggle) => toggle = EditorGUI.Toggle(position.GetCenterRect(Constants.Draw.TOGGLE.CalcSize(GUIContent.none)), toggle);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawFitToggle(ref bool toggle) => toggle = EditorGUILayout.Toggle(toggle, Constants.Draw.TOGGLE, GetCachedFixWidthOption(Constants.Draw.TOGGLE));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFitToggle(bool toggle) => toggle = EditorGUILayout.Toggle(toggle, Constants.Draw.TOGGLE, GetCachedFixWidthOption(Constants.Draw.TOGGLE));
    
    #endregion

    #region [Fix]

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawFixButton(GUIContent buttonContent, float width = 20f, float height = 20f, GUIStyle style = null) {
        return GUILayout.Button(buttonContent, style ?? Constants.Draw.FIT_X2_BUTTON, GUILayout.Width(width), GUILayout.Height(height));
    }
    
    #endregion

    private static GUILayoutOption GetCachedFixWidthOption(string text, GUIStyle style) {
        if (_widthCacheDic.TryGetValue(text, out var option) == false) {
            _widthCacheDic.Add(text, option = GUILayout.Width(style.CalcSize(new GUIContent(text)).x));
        }
        
        return option;
    }
    
    private static GUILayoutOption GetCachedFixWidthOption(Texture texture, GUIStyle style) {
        if (_widthCacheDic.TryGetValue(texture.name, out var option) == false) {
            _widthCacheDic.Add(texture.name, option = GUILayout.Width(style.CalcSize(new GUIContent(texture)).x));
        }

        return option;
    }

    private static GUILayoutOption GetCachedFixWidthOption(GUIStyle style) {
        if (_widthCacheDic.TryGetValue(style.name, out var option) == false) {
            _widthCacheDic.Add(style.name, option = GUILayout.Width(style.CalcSize(GUIContent.none).x));
        }
        
        return option;
    }
}

public record ToggleDraw {

    public string name;
    public bool isActive;

    public string header;

    public ToggleDraw(string name, bool isActive) {
        this.name = name;
        this.isActive = isActive;
    }
        
    public ToggleDraw(string name, bool isActive, string header) : this(name, isActive) => this.header = header;

    public bool HasHeader() => string.IsNullOrEmpty(header) == false;

    public override string ToString() => name;
}