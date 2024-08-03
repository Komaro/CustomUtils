using System.Linq;
using Assembly = UnityEditor.Compilation.Assembly;

public static class AssemblyExtension {
    
    public static bool IsBuiltin(this Assembly assembly) {
        if (assembly.sourceFiles.Length <= 0) {
            return false;
        }

        return assembly.sourceFiles.Any(path => path.StartsWith(Constants.Folder.ASSETS) == false);
    }
}
