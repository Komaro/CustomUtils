using System;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category(TestConstants.Category.FUNCTIONAL)]
public class SafeDelegateTestRunner {
    
    [OneTimeSetUp]
    public void OneTimeSetUp() => LogAssert.ignoreFailingMessages = true;

    [OneTimeTearDown]
    public void OneTimeTearDown() => LogAssert.ignoreFailingMessages = false;

    private delegate void OnVoidHandler();
    
    [Test]
    public void SafeDelegateOperatorTest() {
        var safeDelegate = new SafeDelegate<OnVoidHandler>();
        safeDelegate += () => Logger.TraceLog("Lambda");

        safeDelegate += OnVoidMethod;

        OnVoidHandler handler = () => Logger.TraceLog("Handler");
        safeDelegate += handler;

        safeDelegate.Handler.Invoke();
    }

    private void OnVoidMethod() {
        Logger.TraceLog("Method");
    }
}
