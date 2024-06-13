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
    
    public static bool TryLoadJson<T>(string path, out T json) {
        try {
            json = LoadJson<T>(path);
            return json != null;
        } catch (Exception e) {
            Debug.LogError(e);

            json = default;
            return false;
        }
    }

    public static T LoadJson<T>(string path) {
        try {
            if (File.Exists(path) == false) {
                Debug.LogError($"Invalid Path || {path}");
                throw new FileNotFoundException();
            }

            var text = File.ReadAllText(path);
            if (string.IsNullOrEmpty(text) == false) {
                var json = JsonConvert.DeserializeObject<T>(text);
                return json;
            }
        }  catch (Exception e) {
            Debug.LogError(e);
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
    public abstract bool IsNull();

    public bool TryClone<T>(out T config) where T : JsonConfig, new() {
        config = Clone<T>();
        return config != null;
    }
    
    public virtual T Clone<T>() where T : JsonConfig, new() {
        var type = typeof(T);
        if (type.IsAbstract == false) {
            try {
                var cloneConfig = Activator.CreateInstance<T>();
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
}

public abstract class JsonAutoConfig : JsonConfig, IDisposable {
    
    [JsonIgnore] protected List<IDisposable> disposableList = new();
    [JsonIgnore] protected IConnectableObservable<long> intervalObservable;
    [JsonIgnore] protected IDisposable intervalDisposable;

    [JsonIgnore] private readonly ArrayComparer ARRAY_COMPARER = new();
    [JsonIgnore] private readonly CollectionComparer COLLECTION_COMPARER = new();
    
    protected JsonAutoConfig() {
        intervalObservable = Observable.Interval(TimeSpan.FromSeconds(5f)).Publish(); 
        intervalDisposable = intervalObservable.Connect();
    }

    public virtual void StartAutoSave(string path) {
        if (IsAutoSaving()) {
            StopAutoSave();
        }
        
        foreach (var info in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            if (info.FieldType.IsArray) {
                disposableList.Add(intervalObservable.Select(_ => info.GetValue(this) as Array).DistinctUntilChanged(ARRAY_COMPARER).Skip(1)
                    .Subscribe(_ => Save(path)));
            } else if (info.FieldType.IsGenericCollectionType()) {
                disposableList.Add(intervalObservable.Select(_ => info.GetValue(this) is ICollection collection ? collection.CloneEnumerator() : null)
                    .DistinctUntilChanged(COLLECTION_COMPARER).Skip(1)
                    .Subscribe(_ => Save(path)));
            } else {
                disposableList.Add(intervalObservable.DistinctUntilChanged(_ => info.GetValue(this)).Skip(1).Subscribe(_ => Save(path)));
            }
        }
    }

    public virtual void StopAutoSave() => disposableList.SafeClear(x => x.Dispose());
    
    public virtual void Dispose() {
        StopAutoSave();
        intervalDisposable.Dispose();
    }

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
