
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;


public class ThreadTestRunner {

    private static List<int> _mainThreadAccessTargetList = new();

    [OneTimeSetUp]
    public void OneTimeSetup() {
        _mainThreadAccessTargetList ??= new List<int>();
        _mainThreadAccessTargetList.Clear();
        _mainThreadAccessTargetList.AddRange(RandomUtil.GetRandoms(1000));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _mainThreadAccessTargetList?.Clear();

    [TestCase(10)]
    public void MultiThreadAccessExceptionTest(int threadCount) {
        Assert.ThrowsAsync<InvalidOperationException>(async () => {
            var tasks = RunMultiThreads(threadCount);
            foreach (var randomValue in RandomUtil.GetRandoms(500000)) {
                _mainThreadAccessTargetList.Add(randomValue);
            }

            await Task.WhenAll(tasks);
        });
    }
    
    [TestCase(10)]
    public void MultiThreadAccessPassTest(int threadCount) {
        Assert.DoesNotThrowAsync(async () => {
            var tasks = RunMultiThreads(threadCount);
            foreach (var randomValue in RandomUtil.GetRandoms(500000)) {
                _mainThreadAccessTargetList.Add(randomValue);
            }

            await Task.WhenAll(tasks);
        });
    }

    private IEnumerable<Task> RunMultiThreads(int threadCount) {
        try {
            var taskList = new List<Task>();
            for (var i = 0; i < threadCount; i++) {
                taskList.Add(Task.Run(() => _mainThreadAccessTargetList.ForEach(value => {
                    _ = value;
                    _ = value;
                    _ = value;
                    _ = value;
                    _ = value;
                    _ = value;
                    _ = value;
                    _ = value;
                    _ = value;
                })));
            }

            return taskList;
        } catch (Exception ex) {
            Logger.TraceLog(ex);
        }

        return new List<Task>();
    }
}