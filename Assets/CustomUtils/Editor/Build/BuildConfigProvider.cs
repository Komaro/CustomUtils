using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class BuildConfigProvider {

    private static JObject _jObject;

    public static void AddValue(string key, string value) {
        _jObject.ThrowIfNull(nameof(_jObject));
        _jObject.Remove(key);
        _jObject.Add(key, value);
    }

    public static void AddValue(string key, bool value) {
        _jObject.ThrowIfNull(nameof(_jObject));
        _jObject.Remove(key);
        _jObject.Add(key, value);
    }
    
    public static bool TryGetValue<T>(string key, out T value) => (value = GetValue<T>(key)) != null;

    public static T GetValue<T>(string key) {
        _jObject.ThrowIfNull(nameof(_jObject));
        return _jObject.TryGetValue(key, out var token) ? token.ToObject<T>() : default;
    }

    public static bool TryGetValue(string key, out string value) => string.IsNullOrEmpty(value = GetValue(key)) == false;
    
    public static string GetValue(string key) {
        _jObject.ThrowIfNull(nameof(_jObject));
        return _jObject.TryGetValue(key, out var token) ? token.ToString() : string.Empty;
    }
    
    public static bool IsTrue<T>(T key) where T : struct, Enum => TryGetValue<bool>(key.ToString(), out var isTrue) && isTrue;
    public static bool IsTrue(string key) => TryGetValue<bool>(key, out var isTrue) && isTrue;
    public static bool IsContains(string key) => _jObject.ContainsKey(key);

    #region [Load]
    
    public static JObject Load(string path) {
        try {
            return _jObject = JsonUtil.LoadJObject(path);
        } catch (Exception ex) {
            throw new JsonException(ex.Message);
        }
    }

    public static JObject Load(object ob) {
        try {
            _jObject ??= new JObject();
            foreach (var (key, value) in JObject.FromObject(ob)) {
                if (_jObject.ContainsKey(key)) {
                    _jObject.Remove(key);
                }
                
                _jObject.Add(key, value);
            }
            
            return _jObject;
        } catch (Exception ex) {
            throw new JsonException(ex.Message);
        }
    }

    // -name value → ("name", "value")
    // -flag → ("flag", true)
    public static JObject LoadOnCLI() {
        try {
            _jObject ??= new JObject();
            var args = Environment.GetCommandLineArgs().AsSpan()[1..];
            for (var index = 0; index < args.Length - 1; index++) {
                if (args[index].StartsWith('-')) {
                    if (args[index + 1].StartsWith('-') == false) {
                        AddValue(args[index].Remove(0, 1), args[++index]);
                    } else {
                        AddValue(args[index].Remove(0, 1), true);
                    }
                }
            }
            
            Debug.Log($"[{nameof(BuildConfigProvider)}]\n{_jObject.Properties().ToStringCollection(x => $"{x.Name.ToString()} || {x.Value}", '\n')}");
            return _jObject;
        } catch (Exception ex) {
            throw new JsonException(ex.Message);
        }
    }

    #endregion

    private static bool IsValidArgument(string[] token) => token.Length >= 2 && string.IsNullOrEmpty(token[0]) == false && token[0].Length >= 2;
}
