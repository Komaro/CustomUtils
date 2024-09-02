using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class BuildConfigProvider {

    private static JObject _jObject;

    public static bool TryConvert<T>(out T config) => (config = Convert<T>()) != null;
    public static T Convert<T>() => _jObject.ToObject<T>() ?? default;

    public static void AddValue(string key, string value) {
        if (_jObject == null) {
            throw new NullReferenceException($"{nameof(_jObject)} is Null");
        }
        
        _jObject.Remove(key);
        _jObject.Add(key, value);
    }
    
    public static bool TryGetValue<T>(string key, out T outValue) {
        outValue = GetValue<T>(key);
        return outValue != null;
    }

    public static T GetValue<T>(string key) {
        if (_jObject == null) {
            throw new NullReferenceException($"{nameof(_jObject)} is Null");
        }

        return _jObject.TryGetValue(key, out var token) ? token.ToObject<T>() : default;
    }

    public static bool IsTrue<T>(T key) where T : struct, Enum => TryGetValue<bool>(key.ToString(), out var isTrue) && isTrue;
    public static bool IsTrue(string key) => TryGetValue<bool>(key, out var isTrue) && isTrue;
    public static bool IsContains(string key) => _jObject.ContainsKey(key);

    #region [Load]
    
    public static JObject Load(string path) {
        try {
            _jObject ??= new JObject();
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

    /// <summary>
    /// Using Only Command Line Build
    /// </summary>
    public static JObject LoadOnCLI() {
        try {
            _jObject ??= new JObject();
            foreach (var arg in Environment.GetCommandLineArgs()) {
                var token = arg.Split(Constants.Separator.BUILD_ARGUMENT);
                if (IsValidArgument(token)) {
                    if (_jObject.ContainsKey(token[0])) {
                        Debug.LogWarning($"'{token[0]}' already exists, and the value '{_jObject.GetValue(token[0])}' will be overwritten with the value of '{token[1]}'.");
                    }
                    
                    _jObject.Add(token[0], token[1]);
                }
            }
            
            return _jObject;
        } catch (Exception ex) {
            throw new JsonException(ex.Message);
        }
    }
    
    #endregion

    private static bool IsValidArgument(string[] token) => token.Length >= 2 && string.IsNullOrEmpty(token[0]) == false && token[0].Length >= 2;
}
