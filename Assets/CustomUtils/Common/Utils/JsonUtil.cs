using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class JsonUtil {
    
    public static bool TrySerialize(object obj, out string text) {
        try {
            return string.IsNullOrEmpty(text = JsonConvert.SerializeObject(obj)) == false;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        text = string.Empty;
        return false;
    }
    
    public static bool TryDeserialize<T>(string text, out T obj) {
        try {
            return (obj = JsonConvert.DeserializeObject<T>(text)) != null;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            obj = default;
        }
        
        return false;
    }

    public static bool TryDeserialize(string text, Type type, out object obj) {
        try {
            return (obj = JsonConvert.DeserializeObject(text, type)) != null;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            obj = null;
        }
        
        return false;
    }

    public static bool TryPopulate(string text, object obj) {
        try {
            JsonConvert.PopulateObject(text, obj);
            return true;
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return false;
    }

    public static bool TryLoadJsonOrDecrypt<T>(out T json, string path, string key, ENCRYPT_TYPE type = default) {
        json = LoadJsonOrDecrypt<T>(path, key, type);
        return json != null;
    }

    public static T LoadJsonOrDecrypt<T>(string path, string key, ENCRYPT_TYPE type = default) => LoadJson<T>(path, true) ?? LoadJson<T>(path, key, type);

    public static bool TryLoadJson<T>(string path, out T json) {
        json = LoadJson<T>(path);
        return json != null;
    }

    public static T LoadJson<T>(string path, bool ignoreLog = false) {
        try {
            path = FixJsonExtensionPath(path);
            if (File.Exists(path) == false) {
                Logger.TraceLog($"Invalid Path || {path}", Color.yellow);
                throw new FileNotFoundException();
            }

            if (IOUtil.TryReadText(path, out var text)) {
                return JsonConvert.DeserializeObject<T>(text);
            }
        }  catch (Exception ex) {
            if (ignoreLog == false) {
                Logger.TraceLog(ex, Color.red);
            }
        }
        
        return default;
    }

    public static bool TryLoadJson<T>(out T json, string path, string key, ENCRYPT_TYPE type = default) {
        json = LoadJson<T>(path, key, type);
        return json != null;
    }

    public static T LoadJson<T>(string path, string key, ENCRYPT_TYPE type = default) {
        try {
            path = FixJsonExtensionPath(path);
            if (File.Exists(path) == false) {
                Logger.TraceLog($"Invalid Path || {path}", Color.yellow);
                throw new FileNotFoundException();
            }

            if (IOUtil.TryReadText(path, out var cipherText) && EncryptUtil.TryDecrypt(out var plainText, cipherText, key, type)) {
                return JsonConvert.DeserializeObject<T>(plainText);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return default;
    }

    public static void LoadPopulateJson<T>(string path, T target) {
        try {
            path = FixJsonExtensionPath(path);
            if (File.Exists(path) == false) {
                throw new FileNotFoundException();
            }

            if (IOUtil.TryReadText(path, out var text)) {
                JsonConvert.PopulateObject(text, target);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static void SaveJson(string path, JObject json) {
        try {
            if (json == null) {
                throw new NullReferenceException($"{nameof(json)} is Null");
            }

            SaveJson(path, json.ToString());
        } catch (Exception ex) {
            Logger.TraceError(ex);
            throw;
        }
    }

    public static void SaveJson(string path, object obj) {
        try {
            if (obj == null) {
                throw new NullReferenceException($"{nameof(obj)} is Null");
            }
            
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            if (string.IsNullOrEmpty(json)) {
                throw new JsonException("Serialization failed. An empty result was returned.");
            }
            
            SaveJson(path, json);
        } catch (Exception ex) {
            Logger.TraceError(ex);
            throw;
        }
    }

    public static void SaveJson(string path, string json) {
        try {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(json)) {
                Debug.LogError($"{nameof(path)} or {nameof(json)} is Null or Empty");
                return;
            }
            
            path = FixJsonExtensionPath(path);
            SystemUtil.EnsureDirectoryExists(Directory.GetParent(path)?.FullName);
            
            File.WriteAllText(path, json);
        } catch (Exception ex) {
            Logger.TraceError(ex);
            throw;
        }
    }
    
    public static void SaveEncryptJson(string path, object ob, string encryptKey) {
        try {
            if (string.IsNullOrEmpty(path) || ob == null) {
                Logger.TraceError($"{nameof(path)} is null or empty, or {nameof(ob)} is null");
                return;
            }
            
            path = FixJsonExtensionPath(path);
            SystemUtil.EnsureDirectoryExists(Directory.GetParent(path)?.FullName);
            
            if (EncryptUtil.TryEncryptAES(out var cipherText, JsonConvert.SerializeObject(ob, Formatting.Indented), encryptKey)) {
                File.WriteAllText(path, cipherText);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            throw;
        }
    }

    public static JObject LoadJObject(string path, JsonLoadSettings settings = null) {
        try {
            path = FixJsonExtensionPath(path);
            if (File.Exists(path) == false) {
                Logger.TraceLog($"Invalid Path || {path}", Color.yellow);
                throw new FileNotFoundException();
            }

            if (IOUtil.TryReadText(path, out var jsonText)) {
                return JObject.Parse(jsonText, settings);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }

    public static string FixJsonExtensionPath(string path) => Path.HasExtension(path) == false ? Path.ChangeExtension(path, Constants.Extension.JSON) : path;
}