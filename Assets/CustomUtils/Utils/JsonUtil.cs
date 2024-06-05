using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public void Save(string path) => JsonUtil.SaveJson(path, this);
}
