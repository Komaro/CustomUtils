using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public static partial class JsonUtil {
    
#if UNITY_6000_0_OR_NEWER
    
    public static async Task<string> SerializeAsync(object obj, Formatting formatting = Formatting.None) {
        try {
            await Awaitable.BackgroundThreadAsync();
            var json = JsonConvert.SerializeObject(obj, formatting);
            
            await Awaitable.MainThreadAsync();
            return json;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            await Awaitable.MainThreadAsync();
            return string.Empty;
        }
    }
    
    public static async Task<T> DeserializeAsync<T>(string text) {
        try {
            await Awaitable.BackgroundThreadAsync();
            var obj = JsonConvert.DeserializeObject<T>(text);
            
            await Awaitable.MainThreadAsync();
            return obj;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            await Awaitable.MainThreadAsync();
            return default;
        }
    }

    public static async Task<object> DeserializeAsync(string text, Type type) {
        try {
            await Awaitable.BackgroundThreadAsync();
            var obj = JsonConvert.DeserializeObject(text, type);
            
            await Awaitable.MainThreadAsync();
            return obj;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            await Awaitable.MainThreadAsync();
            return default;
        }
    }
    
    public static async Task PopulateAsync(string text, object obj) {
        try {
            await Awaitable.BackgroundThreadAsync();
            JsonConvert.PopulateObject(text, obj);
            await Awaitable.MainThreadAsync();
        } catch (Exception ex) {
            Logger.TraceError(ex);
            await Awaitable.MainThreadAsync();
        }
    }
    
#endif
    
}