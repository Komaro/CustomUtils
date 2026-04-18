using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

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

    [Test]
    public async Task JsonAsyncTest() {
        var data = new JsonData {
            name = RandomUtil.GetRandom(10),
            id = RandomUtil.GetRandom(0, 10000),
        };
        
        Logger.TraceLog(data.ToStringAllFields());

        var text = await JsonUtil.SerializeAsync(data);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);

        var deserializeData = await JsonUtil.DeserializeAsync<JsonData>(text);
        Assert.IsNotNull(deserializeData);
        Assert.IsTrue(data == deserializeData);

        var obj = await JsonUtil.DeserializeAsync(text, typeof(JsonData));
        Assert.IsNotNull(obj);
        Assert.IsAssignableFrom<JsonData>(obj);
        Assert.IsTrue(data == obj as JsonData);
    }

    private record JsonData {
        
        public string name;
        public int id;
    }
}
