using System.IO;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

public abstract class EditorDrawer {
    
    protected readonly EditorWindow window;

    public EditorDrawer(EditorWindow window) => this.window = window;

    public virtual void Close() { }
    public virtual void Destroy() { }
    public virtual void CacheRefresh() { }
    public abstract void Draw();
    
    public EditorCoroutine StartCoroutine(IEnumerator enumerator, object owner = null) => EditorCoroutineUtility.StartCoroutine(enumerator, owner ?? window);
    protected void Repaint() => window.Repaint();
}

public abstract class EditorAutoConfigDrawer<TConfig, TNullConfig> : EditorDrawer
    where TConfig : JsonAutoConfig, new()
    where TNullConfig : TConfig, new() {

    protected TConfig config;
    protected SystemWatcherServiceOrder order;

    protected bool isDirtyConfig;
    
    protected abstract string CONFIG_NAME { get; }
    protected abstract string CONFIG_PATH { get; }

    protected EditorAutoConfigDrawer(EditorWindow window) : base(window) {
        config = new TNullConfig();
        order = CreateWatcherOrder();
    }
    
    public override void Close() {
        Service.GetService<SystemWatcherService>().Stop(order);
        if (config?.IsNull() == false) {
            config.StopAutoSave();
            config.Save(CONFIG_PATH);
        }
    }

    public override void Destroy() {
        if (order != null) {
            Service.GetService<SystemWatcherService>().Remove(order);
        }
    }
    
    public override void CacheRefresh() {
        if (JsonUtil.TryLoadJson(CONFIG_PATH, out config)) {
            config.StartAutoSave(CONFIG_PATH);
            Service.GetService<SystemWatcherService>().Start(order);
        } else {
            if (config == null || config.IsNull() == false) {
                config = new TNullConfig();
            }
        }
    }

    public override void Draw() {
        if (config?.IsNull() ?? true) {
            EditorGUILayout.HelpBox($"{CONFIG_NAME} 파일이 존재하지 않습니다. 선택한 설정이 저장되지 않으며 일부 기능을 사용할 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button($"{CONFIG_NAME} 파일 생성")) {
                EditorCommon.ShowCheckDialogue($"{CONFIG_NAME} 파일 생성", $"{CONFIG_NAME} 파일을 생성합니다.\n경로는 아래와 같습니다.\n{CONFIG_PATH}", ok: () => {
                    if ((config = config.Clone<TConfig>()) != null) {
                        config.Save(CONFIG_PATH);
                        config.StartAutoSave(CONFIG_PATH);
                        Service.GetService<SystemWatcherService>().Start(order);
                    }
                });
            }

            EditorCommon.DrawSeparator();
        } else {
            if (isDirtyConfig) {
                EditorGUILayout.HelpBox($"{CONFIG_NAME} 파일의 수정이 확인되었습니다.", MessageType.Info);
                if (EditorCommon.DrawButton($"{CONFIG_NAME} 갱신")) {
                    CacheRefresh();
                    isDirtyConfig = false;
                }
            }
            
            GUILayout.Space(10f);
        }
    }

    protected SystemWatcherServiceOrder CreateWatcherOrder() => new(Path.GetDirectoryName(CONFIG_PATH), CONFIG_NAME, OnSystemWatcherEventHandler);
    
    protected virtual void OnSystemWatcherEventHandler(object ob, FileSystemEventArgs args) {
        if (config == null) {
            Logger.TraceError($"{nameof(config)} is Null.");
            return;
        }
        
        switch (args.ChangeType) {
            case WatcherChangeTypes.Created:
                if (config == null) {
                    config = new TConfig();
                } else if (config.IsNull()) {
                    config = config.Clone<TConfig>();
                }
                break;
            case WatcherChangeTypes.Deleted:
                if (config.IsNull() == false) {
                    config = config.Clone<TNullConfig>();
                }
                break;
            case WatcherChangeTypes.Changed:
                if ((File.GetLastWriteTime(args.FullPath) - config.LastSaveTime).TotalMilliseconds > 100) {
                    isDirtyConfig = true;
                }
                break;
        }
    }
}