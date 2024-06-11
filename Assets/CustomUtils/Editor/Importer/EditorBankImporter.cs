using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// It can be applied when using the Fmod plugin.
/// </summary>
public class EditorBankImporter : AssetPostprocessor {
    
    // private static readonly char PATH_SEPARATOR = '/';
    // private static readonly char NAME_SEPARATOR = '_';

    private static readonly Regex BANK_REGEX = new Regex(@"\.bank$");
    
    /// <summary>
    /// .bank 파일 감지 후 확장자를 .bytes로 변환
    /// </summary>
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        var regex = new Regex(@"\.bank$");
        importedAssets?.ForEach(x => {
            if (BANK_REGEX.IsMatch(x)) {
                var path = x.Replace(".strings", "_Strings").Replace(".bank", ".bytes");
                Logger.TraceLog($"{x} => {path}");
                AssetDatabase.MoveAssetToTrash(path);
                AssetDatabase.MoveAsset(x, path);
            }
        });

        AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);
    }
}