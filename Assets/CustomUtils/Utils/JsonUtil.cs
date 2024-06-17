using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;

public static class JsonUtil {

    public static bool TryLoadJsonIgnoreLog<T>(string path, out T json) {
        try {
            if (File.Exists(path)) {
                json = LoadJson<T>(path);
                return json != null;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        json = default;
        return false;
    }
    
    public static bool TryLoadJson<T>(string path, out T json) {
        try {
            json = LoadJson<T>(path);
            return json != null;
        } catch (Exception) {
            json = default;
            return false;
        }
    }

    public static T LoadJson<T>(string path) {
        try {
            if (File.Exists(path) == false) {
                Logger.TraceLog($"Invalid Path || {path}", Color.yellow);
                throw new FileNotFoundException();
            }

            var text = File.ReadAllText(path);
            if (string.IsNullOrEmpty(text) == false) {
                var json = JsonConvert.DeserializeObject<T>(text);
                return json;
            }
        }  catch (Exception ex) {
            Logger.TraceLog(ex, Color.red);
            throw;
        }
        
        return default;
    }

    public static void SaveJson(string path, JObject json) {
        try {
            if (json == null) {
                throw new NullReferenceException($"{nameof(json)} is Null");
            }

            SaveJson(path, json.ToString());
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
    }

    public static void SaveJson(string path, object ob) {
        try {
            if (ob == null) {
                throw new NullReferenceException($"{nameof(ob)} is Null");
            }
            
            var json = JsonConvert.SerializeObject(ob, Formatting.Indented);
            if (string.IsNullOrEmpty(json)) {
                throw new JsonException("Serialization failed. An empty result was returned.");
            }
            
            SaveJson(path, json);
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
    }

    public static void SaveJson(string path, string json) {
        try {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(json)) {
                Debug.LogError($"{nameof(path)} or {nameof(json)} is Null or Empty");
                return;
            }
            
            var parentPath = Directory.GetParent(path)?.FullName;
            if (string.IsNullOrEmpty(parentPath)) {
                Debug.LogError($"{nameof(parentPath)} is Null or Empty");
                return;
            }
            
            if (Directory.Exists(parentPath) == false) {
                Directory.CreateDirectory(parentPath);
            }
            
            File.WriteAllText(path, json);
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
    }
}

public abstract class JsonConfig {

    public virtual void Save(string path) => JsonUtil.SaveJson(path, this);

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

public abstract class JsonAutoConfig : JsonConfig, IDisposable {
    
    [JsonIgnore] protected List<IDisposable> disposableList = new();
    [JsonIgnore] protected IConnectableObservable<long> intervalObservable;
    [JsonIgnore] protected IDisposable intervalDisposable;
    [JsonIgnore] protected bool saveFlag;

    [JsonIgnore] private readonly ArrayComparer ARRAY_COMPARER = new();
    [JsonIgnore] private readonly CollectionComparer COLLECTION_COMPARER = new();
    
    protected JsonAutoConfig() => intervalObservable = Observable.Interval(TimeSpan.FromSeconds(5f)).Publish();
    
    public override T Clone<T>() {
        Dispose();
        return base.Clone<T>();
    }

    public override object Clone(Type type) {
        Dispose();
        return base.Clone(type);
    }

    public virtual void StartAutoSave(string path) {
        if (IsAutoSaving()) {
            StopAutoSave();
        }
        
        intervalDisposable = intervalObservable.Connect();
        foreach (var info in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            if (info.FieldType.IsArray) {
                disposableList.Add(intervalObservable.Select(_ => info.GetValue(this) as Array)
                    .DistinctUntilChanged(ARRAY_COMPARER)
                    .Subscribe(_ => saveFlag = true));
            } else if (info.FieldType.IsGenericCollectionType()) {
                disposableList.Add(intervalObservable.Select(_ => info.GetValue(this) is ICollection collection ? collection.CloneEnumerator() : null)
                    .DistinctUntilChanged(COLLECTION_COMPARER)
                    .Subscribe(_ => saveFlag = true));
            } else {
                disposableList.Add(intervalObservable.DistinctUntilChanged(_ => info.GetValue(this)).Subscribe(_ => saveFlag = true));
            }
        }
        
        disposableList.Add(Observable.Interval(TimeSpan.FromSeconds(5f)).Subscribe(_ => {
            if (saveFlag) {
                saveFlag = false;
                Save(path);
            }
        }));

    }

    public virtual void StopAutoSave() => disposableList?.SafeClear(x => x.Dispose());
    
    public virtual void Dispose() {
        StopAutoSave();
        intervalDisposable?.Dispose();
    }

    ~JsonAutoConfig() => Dispose();

    public virtual bool IsAutoSaving() => disposableList.Any();

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
            
            while (x.MoveNext() && y.MoveNext()) {
                if (x.Current?.Equals(y.Current) == false) {
                    return false;
                }
            }

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
