using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;

[Category(TestConstants.Category.FUNCTIONAL)]
public class CallStackTestRunner {

    [Test]
    public void CallStackQueueFunctionalTest() {
        var callStack = new CallStack<int>();
        var randomList = RandomUtil.GetRandoms(10).ToList();
        foreach (var value in randomList) {
            callStack.Push(value);
        }

        Assert.AreEqual(callStack.Count, randomList.Count);
        
        Assert.IsTrue(callStack.TryPeek(out var temp));
        Assert.AreEqual(randomList[^1], temp);
        
        Assert.IsTrue(callStack.TryPop(out temp));
        Assert.AreEqual(randomList[^1], temp);
        
        Assert.IsTrue(callStack.TryPop(out temp));
        Assert.AreNotEqual(randomList[^1], temp);
        Assert.AreEqual(randomList[^2], temp);
        
        Assert.IsTrue(callStack.TryPeekTail(out temp));
        Assert.AreEqual(randomList[0], temp);
        
        Assert.IsTrue(callStack.TryPeekTail(out temp, 1));
        Assert.AreEqual(randomList[1], temp);
        
        Assert.AreNotEqual(callStack.Count, randomList.Count);
        Assert.AreEqual(callStack.Count, randomList.Count - 2);
        
        callStack.Push(randomList[2]);
        Assert.AreEqual(callStack.Count, randomList.Count - 2);
        
        Assert.IsTrue(callStack.TryPeek(out temp));
        Assert.AreEqual(randomList[2], temp);
        
        Assert.IsTrue(callStack.TryPeekTail(out temp, 2));
        Assert.AreNotEqual(randomList[2], temp);
        Assert.AreEqual(randomList[3], temp);

        while (callStack.TryPop(out temp)) {
            Logger.TraceLog(temp);
        }
        
        Assert.AreEqual(callStack.Count, 0);
    }
}
