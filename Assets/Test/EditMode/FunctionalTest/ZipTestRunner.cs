using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

[Category(TestConstants.Category.FUNCTIONAL)]
public class ZipTestRunner {

    [Test]
    public async Task ZipTest() {
        var zipPath = Constants.Path.COMMON_CONFIG_PATH;
        var destinationPath = Path.Combine(Constants.Path.PROJECT_TEMP_PATH, nameof(ZipTestRunner).AutoSwitchExtension(Constants.Extension.ZIP));
        await SystemUtil.ZipAsync(zipPath, destinationPath, Constants.Extension.JSON_FILTER);
        
        var fileInfo = new FileInfo(destinationPath);
        Assert.IsTrue(fileInfo.Exists);
        Assert.IsTrue(fileInfo.Length > 0);

        using var archive = ZipFile.OpenRead(destinationPath);
        Assert.IsNotNull(archive);

        var directoryInfo = new DirectoryInfo(zipPath);
        Assert.IsTrue(directoryInfo.Exists);

        var zipTargetSet = directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Select(info => Path.GetRelativePath(zipPath, info.FullName)).ToImmutableHashSetWithDistinct();
        foreach (var entry in archive.Entries) {
            Assert.IsTrue(zipTargetSet.Contains(entry.FullName));
            Logger.TraceLog($"{entry.Name} || {entry.FullName}");
        }
        
        Logger.TraceLog("Pass zip async test");
        await Task.CompletedTask;
    }
}
