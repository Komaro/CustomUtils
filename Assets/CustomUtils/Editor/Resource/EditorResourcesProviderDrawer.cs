using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

[EditorResourceDrawer(RESOURCE_SERVICE_MENU_TYPE.Provider, typeof(ResourcesProvider))]
public class EditorResourcesProviderDrawer : EditorResourceDrawer {

    private JObject _resourcesListJson;

    private readonly string RESOURCES_PATH = $"{Application.dataPath}/{Constants.Folder.RESOURCES}";

    protected override string CONFIG_NAME => string.Empty;
    protected override string CONFIG_PATH => string.Empty;
    
    public EditorResourcesProviderDrawer(EditorWindow window) : base(window) { }
    public override void CacheRefresh() { }

    public override void Draw() {
        if (_resourcesListJson == null) {
            if (Directory.Exists(RESOURCES_PATH) == false) {
                EditorGUILayout.HelpBox($"{nameof(Resources)} 폴더가 존재하지 않습니다.", MessageType.Error);
                if (GUILayout.Button($"{nameof(Resources)} 폴더 생성")) {
                    CreateResourcesFolder();
                }
            } else {
                var resourcesListJsonPath = $"Assets/{Constants.Folder.RESOURCES}/{Constants.Resource.RESOURCE_LIST_JSON}";
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
            EditorGUILayout.HelpBox($"{nameof(ResourcesProvider)} 기능을 사용하기 위해선 {Constants.Resource.RESOURCE_LIST_JSON} 파일이 필요합니다.", MessageType.Info);
        }
        
        if (GUILayout.Button("생성 & 갱신")) {
            if (Directory.Exists(RESOURCES_PATH) == false) {
                EditorCommon.ShowCheckDialogue("경고", $"{nameof(Resources)} 폴더가 존재하지 않습니다. 생성하시겠습니까?\n확인을 누르면 폴더 생성후 Json 파일을 생성합니다.", "확인", "취소", () => {
                    CreateResourcesFolder();
                    ResourceGenerator.GenerateResourcesListJson($"{RESOURCES_PATH}/{Constants.Resource.RESOURCE_LIST_JSON}", OnProgress, OnEnd);
                });
            } else {
                ResourceGenerator.GenerateResourcesListJson($"{RESOURCES_PATH}/{Constants.Resource.RESOURCE_LIST_JSON}", OnProgress, OnEnd);
            }
        }

        if (GUILayout.Button("새로고침")) {
            Refresh();
        }
    }
    
    private void OnProgress(string title, string info, float progress) => EditorUtility.DisplayProgressBar(title, info, progress);
    
    private void OnEnd() {
        EditorUtility.ClearProgressBar();
        Refresh();
    }

    private void Refresh() => _resourcesListJson = null;

    private void CreateResourcesFolder() {
        SystemUtil.CreateDirectory(RESOURCES_PATH);
        AssetDatabase.Refresh();
    }
}
