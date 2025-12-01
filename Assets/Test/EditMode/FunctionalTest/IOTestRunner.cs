using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[Category(TestConstants.Category.FUNCTIONAL)]
public class IOTestRunner {
    
    private CancellationTokenSource _source = new();
    
    [OneTimeTearDown]
    public void OneTimeTearDown() => _source.Cancel();

    [Test]
    public async Task ReadAllBytesParallelAsyncTest() {
        var info = new DirectoryInfo(Constants.Path.COMMON_CONFIG_PATH);
        Assert.IsTrue(info.Exists);
        
        Logger.TraceLog($"MainThread || {Thread.CurrentThread.ManagedThreadId}");
        await foreach (var bytes in IOUtil.ReadAllBytesParallelAsync(info.EnumerateFiles(Constants.Extension.JSON_FILTER, SearchOption.AllDirectories).Select(fileInfo => fileInfo.FullName), token:_source.Token)) {
            Assert.IsNotEmpty(bytes);
        }
    }
}
