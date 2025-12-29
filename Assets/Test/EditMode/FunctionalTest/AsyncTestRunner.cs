using System.Threading.Tasks;
using NUnit.Framework;

[Category(TestConstants.Category.FUNCTIONAL)]
public class AsyncTestRunner {

    [Test]
    public async Task AsyncTest() {
        var operation = new AsyncCustomOperation();
        _ = RunTest(operation);
        await operation;

        var genericOperation = new AsyncCustomOperation<int>();
        _ = RunTest(genericOperation);
        var intValue = await genericOperation;
        Assert.Equals(intValue, genericOperation.Result);
    }

    private async Task RunTest(AsyncCustomOperation operation) {
        await Task.Delay(1500);
        Logger.TraceLog("Delay");
        operation.Done();
    }

    private async Task<int> RunTest(AsyncCustomOperation<int> operation) {
        await Task.Delay(1500);
        var randomValue = RandomUtil.GetRandom(1, 1500000);
        operation.Complete(randomValue);
        return randomValue;
    }
}
