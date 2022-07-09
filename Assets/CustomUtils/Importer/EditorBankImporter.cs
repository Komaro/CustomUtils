using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// It can be applied when using the Fmod plugin.
/// </summary>
public class EditorBankImporter : AssetPostprocessor {
    
    private static readonly char PATH_SEPARATOR = '/';
    private static readonly char NAME_SEPARATOR = '_';

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        var regex = new Regex(@"\.bank$");

        importedAssets?.ForEach(x => {
            if (regex.IsMatch(x)) {
                // 
                var newPath = x.Replace(".strings", "_Strings").Replace(".bank", ".bytes");
                Logger.TraceLog($"{x} => {newPath}");
                AssetDatabase.MoveAssetToTrash(newPath);
                AssetDatabase.MoveAsset(x, newPath);
            }
        });

        AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);
    }
}