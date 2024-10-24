using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;

public class UIViewModelTestRunner {

    private static UIViewModel _viewModel;
    
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        
    }

    [Test]
    public void Test<T>() {
        
    }

    // TODO. Need Coverage Test
    [Test]
    public void ViewModelTest() {
        var viewModel = new TestSimpleUIViewModel {
            Title = { Value = "TestViewModel" },
            Count = {  Value = 5 }
        };
        
        viewModel.OnModelChanged += (fieldName, args) => {
            switch (args) {
                case NotifyCollectionChangedEventArgs listArgs:
                    Logger.TraceLog($"{fieldName} || {listArgs.action}");
                    break;
                default:
                    Logger.TraceLog($"{fieldName}");
                    break;
            }
        };
        
        Assert.IsTrue(viewModel.Count == 5);
        
        viewModel.IncreaseCount(5);
        Assert.IsTrue(viewModel.Count == 10);
        
        viewModel.DecreaseCount(2);
        Assert.IsTrue(viewModel.Count == 8);
        
        Logger.TraceLog($"Pass {nameof(viewModel.Count)}");
        
        viewModel.DecreaseCount(3);
        
        viewModel.Collection.Add(1);
        viewModel.Collection.Clear();
        
        viewModel.Collection.Add(5);
        viewModel.Collection.RemoveAt(0);
        
        Assert.IsTrue(viewModel.Collection.Count == 0);
        Logger.TraceLog($"Pass {nameof(viewModel.Collection)}\n");

        viewModel.List.Add(1);
        viewModel.List.Add(2);
        viewModel.List.Clear();
        
        viewModel.List.Add(3);
        viewModel.List.Add(4);
        viewModel.List.RemoveAt(1);
        
        Assert.IsTrue(viewModel.List.Count == 1 && viewModel.List.First() == 3);
        Logger.TraceLog($"Pass {nameof(viewModel.List)}\n");
        
        viewModel.Dictionary.Add(1, 10);
        viewModel.Dictionary.Add(2, 10);
        viewModel.Dictionary.Add(3, 10);
        viewModel.Dictionary.Clear();
        
        viewModel.Dictionary.Add(5, 11);
        viewModel.Dictionary.Remove(5);

        Assert.IsTrue(viewModel.Dictionary.Count == 0);
        Logger.TraceLog($"Pass {nameof(viewModel.Dictionary)}\n");
        
    }

    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(10000)]
    [Performance]
    public void ViewModelPerformanceTest(int iterationCount) {
        var defaultGroup = new SampleGroup("Test Group");
        
        Measure.Method(() => {
            _ = new TestSimpleUIViewModel();
        }).WarmupCount(5).MeasurementCount(10).IterationsPerMeasurement(iterationCount).SampleGroup(defaultGroup).GC().Run();
    }
}
