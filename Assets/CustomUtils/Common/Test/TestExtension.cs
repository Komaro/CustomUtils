using System;
using System.Reflection;
using NUnit.Framework;

public static class TestExtension {

    public static void FieldChangeTest<T>(this object obj, string fieldName, BindingFlags flags, Func<T, bool> check) {
        if (obj.GetType().TryGetFieldValue<T>(out var fieldValue, obj, fieldName, flags) == false) {
            Assert.Fail($"Field get failed || {fieldName}");
            return;
        }
        
        Assert.IsTrue(check.Invoke(fieldValue));
        Logger.TraceLog($"Pass {fieldName} Change Test");
    }

    public static void MethodCallTest(this object ui, string methodName, BindingFlags flags, Func<bool> check = null, params object[] param) {
        if (ui.GetType().TryGetMethodInfo(out var info, methodName, flags) == false) {
            Assert.Fail($"Method call failed || {methodName}");
            return;
        }

        info.Invoke(ui, param);
        if (check?.Invoke() ?? true) {
            Logger.TraceLog($"Pass {methodName} Call Test");
        }
    }
    
    private static bool TryMethodCallTest(this object ui, string methodName, BindingFlags flags, params object[] param) {
        if (ui.GetType().TryGetMethodInfo(out var info, methodName, flags)) {
            info.Invoke(ui, param);
            return true;
        }
        
        return false;
    }
}
