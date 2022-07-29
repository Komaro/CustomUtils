using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BuildSettings {

    public JObject json;

    private static BuildSettings _instance = null;

    public static BuildSettings Instance {
        get {
            _instance ??= new BuildSettings();
            return _instance;
        }
    }

    protected BuildSettings() { }

    public T GetValue<T>(string key) {
        if (json == null) {
            throw new NullReferenceException($"{nameof(json)} is Null");
        }

        return json.TryGetValue(key, out var token) ? token.ToObject<T>() : default;
    }

    public bool TryGetValue<T>(string key, out T outValue) {
        outValue = GetValue<T>(key);
        return outValue != null;
    }
    
    public void SetBuildSettings(string path) {
        if (File.Exists(path) == false) {
            throw new FileNotFoundException($"{nameof(FileNotFoundException)} || {path}");
        }

        using var streamReader = File.OpenText(path);
        try {
            var jObject = JObject.Parse(streamReader.ReadToEnd());
            _instance = JsonConvert.DeserializeObject<BuildSettings>(jObject.ToString());
            Instance.json = jObject;
                
            Debug.Log("=================== [Build] Set Json Build Settings ===================");
            Debug.Log($"{json}");
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        } finally {
            streamReader.Close();
        }
    }

    public void SetBuildSettings(JObject json) {
        try {
            if (json == null) {
                throw new NullReferenceException($"{nameof(json)} is Null");
            }

            _instance = JsonConvert.DeserializeObject<BuildSettings>(json.ToString());
            Instance.json = json;
            
            Debug.Log("=================== [Build] Set Json Build Settings ===================");
            Debug.Log($"{json}");
        } catch (Exception e) {
            Debug.LogError(e);
            throw;
        }
    }

    /// <summary>
    /// Using Only Command Line Build
    /// </summary>
    public static void SetBuildSettingsOnCLI() {
        var json = new JObject();
        var commands = Environment.GetCommandLineArgs();
        commands.ForEach(x => {
            var tokens = x.Split(':');
            if (tokens.Length < 2 || string.IsNullOrEmpty(tokens[0]) || tokens[0].Length < 2) {
                Debug.LogError($"Invalid Tokens || {string.Join(':', tokens)}");
                return;
            }
            
            json.Add(tokens[0], tokens[1]);
            Debug.Log($"=================== [Build] Add to {nameof(json)} || {tokens[0]} : {tokens[1]} ===================");
        });

        Instance.SetBuildSettings(json);
    }
}
