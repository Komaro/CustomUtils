using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UniRx;
using UnityEditor;

public abstract class JsonConfig : IDisposable {

    [JsonIgnore] protected DateTime lastSaveTime;
    [JsonIgnore] public ref DateTime LastSaveTime => ref lastSaveTime; 
    
    public abstract void Dispose(); 
    
    public virtual void Save(string path) {
        lastSaveTime = DateTime.Now;
        JsonUtil.SaveJson(path, this);
    }

    public bool TryClone<T>(out T config) where T : JsonConfig, new() {
        config = Clone<T>();
        return config != null;
    }

    public virtual T Clone<T>() where T : JsonConfig, new() {
        if (Clone(typeof(T)) is T cloneConfig) {
            return cloneConfig;
        }

        return null;
    }

    public bool TryClone(Type type, out object config) {
        config = Clone(type);
        return config != null;
    }

    public virtual object Clone(Type type) {
        if (type == null) {
            Logger.TraceError($"{nameof(type)} is Null");
            return null;
        }

        if (type.IsAbstract == false) {
            try {
                var cloneConfig = Activator.CreateInstance(type);
                if (cloneConfig is JsonAutoConfig == false) {
                    return null;
                }

                foreach (var info in type.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
                    info.SetValue(cloneConfig, info.GetValue(this));
                }

                return cloneConfig;
            } catch (Exception ex) {
                Logger.TraceError(ex);
                throw;
            }
        }

        return null;
    }

    public abstract bool IsNull();
}

// static 필드에서 new()로 생성하지 말 것.
public abstract class JsonAutoConfig : JsonConfig {
    
    [JsonIgnore] protected string savePath;
    [JsonIgnore] protected readonly List<IDisposable> disposableList = new();
    [JsonIgnore] protected IDisposable intervalDisposable;
    [JsonIgnore] protected bool saveFlag;

    [JsonIgnore] private readonly ArrayComparer ARRAY_COMPARER = new();
    [JsonIgnore] private readonly CollectionComparer COLLECTION_COMPARER = new();
    
    public override T Clone<T>() {
        Dispose();
        return base.Clone<T>();
    }

    public override object Clone(Type type) {
        Dispose();
        return base.Clone(type);
    }

    public virtual void StartAutoSave(string savePath) {
        if (IsAutoSaving()) {
            StopAutoSave();
        }

        this.savePath = savePath;
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            return;
        }

        var intervalObservable = Observable.Interval(TimeSpan.FromSeconds(5f)).Publish();
        intervalDisposable = intervalObservable.Connect();
        
        RegisterFieldWatchers(intervalObservable);
        RegisterSaveTrigger();
    }

    protected virtual void RegisterFieldWatchers(IObservable<long> intervalObservable) {
        foreach (var info in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(info => info.IsDefined<JsonIgnoreAttribute>() == false)) {
            var func = DynamicMethodProvider.GetFieldValueFunc(GetType(), info);
            if (info.FieldType.IsArray) {
                disposableList.Add(intervalObservable.Select(_ => func.Invoke(this) as Array)
                    .DistinctUntilChanged(ARRAY_COMPARER)
                    .Subscribe(_ => saveFlag = true));
            } else if (info.FieldType.IsGenericCollectionType()) {
                disposableList.Add(intervalObservable.Select(_ => func.Invoke(this) is ICollection collection ? collection.CloneEnumerator() : null)
                    .DistinctUntilChanged(COLLECTION_COMPARER)
                    .Subscribe(_ => saveFlag = true));
            } else {
                disposableList.Add(intervalObservable.DistinctUntilChanged(_ => func.Invoke(this)).Subscribe(_ => saveFlag = true));
            }
        }
    }

    protected virtual void RegisterSaveTrigger() {
        disposableList.Add(Observable.Interval(TimeSpan.FromSeconds(5f)).Subscribe(_ => {
            if (saveFlag) {
                saveFlag = false;
                Save(savePath);
            }
        }));
    }

    public virtual void StopAutoSave() {
        disposableList?.SafeClear(x => x.Dispose());
        intervalDisposable?.Dispose();
    }

    ~JsonAutoConfig() => Dispose();
    
    public override void Dispose() {
        if (IsNull() == false) {
            StopAutoSave();
        }
    }

    public virtual bool IsAutoSaving() => disposableList.Any();
    
    protected virtual void OnPlayModeStateChanged(PlayModeStateChange state) {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode) {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StartAutoSave(savePath);
        }
    }
    
    private class ArrayComparer : IEqualityComparer<Array> {

        public bool Equals(Array x, Array y) {
            if (x == null || y == null) {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(Array array) => array.GetHashCode();
    }

    private class CollectionComparer : IEqualityComparer<IEnumerator> {

        public bool Equals(IEnumerator x, IEnumerator y) {
            if (x == null || y == null) {
                return false;
            }
            
            x.Reset();
            y.Reset();

            // Check count zero
            if (x.MoveNext() == false || y.MoveNext() == false) {
                return false;
            }

            do {
                if (x.Current?.Equals(y.Current) == false) {
                    return false;
                }
            } while (x.MoveNext() && y.MoveNext());

            return true;
        }

        public int GetHashCode(IEnumerator enumerator) {
            unchecked {
                var hash = 17;
                while (enumerator.MoveNext()) {
                    hash = hash * 31 + enumerator.Current?.GetHashCode() ?? 0;
                }
                
                return hash;
            }
        }
    }
}