using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public partial class EditorResourceService {

    private static JObject _resourcesListJson;

    private static readonly Regex RESOURCES_FOLDER_REGEX = new(@"Assets/Resources/");
    
    private const int REMOVE_PREFIX_COUNT = 17;
    
    [ResourceProviderDraw(typeof(ResourcesProvider))]
    private void DrawResourcesProvider() {
        if (_resourcesListJson == null) {
            var resourcesFullPath = $"{Application.dataPath}/{Constants.Resource.RESOURCES_FOLDER}";
            if (Directory.Exists(resourcesFullPath) == false) {
                EditorGUILayout.HelpBox($"{nameof(Resources)} 폴더가 존재하지 않습니다.", MessageType.Error);
                if (GUILayout.Button($"{nameof(Resources)} 폴더 생성")) {
                    CreateResourcesFolder(resourcesFullPath);
                }
            } else {
                var resourcesListJsonPath = $"Assets/{Constants.Resource.RESOURCES_FOLDER}/{Constants.Resource.RESOURCE_LIST_JSON}.json";
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(resourcesListJsonPath);
                if (textAsset != null) {
                    _resourcesListJson = JObject.Parse(textAsset.text);
                    if (_resourcesListJson == null) {
                        EditorGUILayout.HelpBox($"잘못된 Json 형식입니다. 아래 경로의 Json 파일을 확인해 주세요.\n{resourcesListJsonPath}", MessageType.Error);
                    }
                }
            }
        }

        if (_resourcesListJson != null) {
            GUILayout.Label($"{nameof(_resourcesListJson.Count)} : {_resourcesListJson.Count}");
        } else {
            EditorGUILayout.HelpBox($"{nameof(ResourcesProvider)} 기능을 사용하기 위해선 {Constants.Resource.RESOURCE_LIST_JSON}.json 파일이 필요합니다.", MessageType.Info);
        }
        
        if (GUILayout.Button("생성 & 갱신")) {
            var resourcesFullPath = $"{Application.dataPath}/{Constants.Resource.RESOURCES_FOLDER}";
            if (Directory.Exists(resourcesFullPath) == false) {
                EditorCommon.ShowCheckDialogue("경고", $"{nameof(Resources)} 폴더가 존재하지 않습니다. 생성하시겠습니까?\n확인을 누르면 폴더 생성후 Json 파일을 생성합니다.", "확인", "취소", () => {
                    CreateResourcesFolder(resourcesFullPath);
                    GenerateResourcesListJson();
                });
            } else {
                GenerateResourcesListJson();
            }
        }

        if (GUILayout.Button("새로고침")) {
            ResourceListJsonRefresh();
        }
    }

    private void ResourceListJsonRefresh() {
        _resourcesListJson = null;
    }

    private void CreateResourcesFolder(string path) {
        Directory.CreateDirectory(path);
        Logger.TraceLog($"Create Resources Folder || Path = {path}");
        AssetDatabase.Refresh();
    }

    private void GenerateResourcesListJson() {
        // TODO. 프로그래스 UI 처리
        try {
            var jObject = new JObject();
            jObject.AutoAdd(Constants.Resource.RESOURCE_LIST_JSON.ToUpper(), Constants.Resource.RESOURCE_LIST_JSON);
            foreach (var path in AssetDatabase.GetAllAssetPaths()) {
                if (Directory.Exists(path) == false && RESOURCES_FOLDER_REGEX.IsMatch(path)) {
                    var nameKey = Path.GetFileNameWithoutExtension(path).ToUpper();
                    if (jObject.ContainsKey(nameKey)) {
                        Logger.TraceError($"Duplicate Resource {nameof(nameKey)}\nPath|| {path}\nPath || {jObject[nameKey]}");
                        continue;
                    }
                    
                    jObject.AutoAdd(nameKey, path.Remove(0, REMOVE_PREFIX_COUNT).Split('.')[0]);
                }
            }

            File.WriteAllText($"{Application.dataPath}/{Constants.Resource.RESOURCES_FOLDER}/{Constants.Resource.RESOURCE_LIST_JSON}.json", jObject.ToString());
            AssetDatabase.Refresh();
            
            ResourceListJsonRefresh();
        } catch (Exception e) {
            Logger.TraceError(e);
            throw;
        }
    }
}
