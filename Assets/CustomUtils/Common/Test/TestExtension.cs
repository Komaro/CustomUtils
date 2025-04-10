using System;
using System.Reflection;
using NUnit.Framework;

public static class TestExtension {

    public static void FieldChangeTest<T>(this object obj, string fieldName, BindingFlags flags, Func<T, bool> check) {
        if (obj.GetType().TryGetFieldValue(out var valueObj, obj, fieldName, flags) == false || valueObj is not T value) {
            Assert.Fail($"Field get failed || {fieldName}");
            return;
        }
        
        Assert.IsTrue(check.Invoke(value));
        Logger.TraceLog($"Pass {fieldName} change test");
    }

    public static void MethodCallTest(this object ui, string methodName, BindingFlags flags, Func<bool> check = null, params object[] param) {
        if (ui.GetType().TryGetMethodInfo(out var info, methodName, flags) == false) {
            Assert.Fail($"Method call failed || {methodName}");
            return;
        }

        info.Invoke(ui, param);
        
        Assert.IsTrue(check?.Invoke() ?? true);
        Logger.TraceLog($"Pass {methodName} call test");
    }
}
