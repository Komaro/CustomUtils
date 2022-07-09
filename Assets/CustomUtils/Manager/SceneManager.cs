using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : Singleton<SceneManager> {

    Dictionary<string, int> _sceneDic = new Dictionary<string, int>();

    public SceneManager() {
        var totalSceneCount = UnitySceneManager.sceneCount;
        for (int i = 0; i < totalSceneCount; i++) {
            var scene = UnitySceneManager.GetSceneAt(i);
            _sceneDic.Add(scene.name, i);
        }
    }

    public void ChangeScene(int index) {
        if (_sceneDic.ContainsValue(index)) {
            UnitySceneManager.LoadScene(index);
        } else {
            Logger.TraceError($"{index} is Invalid Scene Index");  
        }
    }
    
    public void ChangeScene(string scene) {
        if (_sceneDic.TryGetValue(scene, out var index)) {
            UnitySceneManager.LoadScene(index);
        } else {
            Logger.TraceError($"{scene} is Invalid Scene Name");
        }
    }

    public void ChangeScene(Enum sceneType) => ChangeScene(sceneType.ToString());
}