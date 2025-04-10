using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category(TestConstants.Category.SERVICE)]
public class MonoServiceTestRunner {
    
    [OneTimeSetUp]
    public void OneTimeSetUp() => LogAssert.ignoreFailingMessages = true;

    [OneTimeTearDown]
    public void OneTimeTearDown() => LogAssert.ignoreFailingMessages = false;

    [UnityTest]
    public IEnumerator MonoServiceCoroutineTest() {
        if (Service.TryGetService<MonoService>(out var service)) {
            using (service.Get(out var monoObject)) {
                Assert.IsNotNull(monoObject);
                
                yield return monoObject.StartCoroutine(DelayCall(1f));
                yield return new WaitForEndOfFrame();
                
                monoObject.AttachUpdate(() => Logger.TraceLog(nameof(monoObject.AttachUpdate)));
                monoObject.AttachFixedUpdate(() => Logger.TraceLog(nameof(monoObject.AttachFixedUpdate)));
                monoObject.AttachLateUpdate(() => Logger.TraceLog(nameof(monoObject.AttachLateUpdate)));
                
                yield return new WaitForEndOfFrame();
                monoObject.ClearUpdate();
                monoObject.FieldChangeTest<SafeDelegate<Action>>("OnUpdate", BindingFlags.NonPublic | BindingFlags.Instance, safeDelegate => safeDelegate.Count <= 0);
                
                yield return new WaitForEndOfFrame();
                monoObject.ClearFixedUpdate();
                monoObject.FieldChangeTest<SafeDelegate<Action>>("OnFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance, safeDelegate => safeDelegate.Count <= 0);
                
                yield return new WaitForEndOfFrame();
                monoObject.ClearLateUpdate();
                monoObject.FieldChangeTest<SafeDelegate<Action>>("OnLateUpdate", BindingFlags.NonPublic | BindingFlags.Instance, safeDelegate => safeDelegate.Count <= 0);
                
                yield return new WaitForSeconds(0.25f);
            }
        }
    }

    private IEnumerator DelayCall(float delay) {
        yield return new WaitForSeconds(delay);
        Logger.TraceLog($"Delay || {delay}");
    }
}
