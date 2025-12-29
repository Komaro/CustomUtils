using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public class PlayModeTestRunner {

    private UnityMainThreadDispatcherService _service;
    
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        _service = Service.GetService<UnityMainThreadDispatcherService>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        Service.RemoveService<UnityMainThreadDispatcherService>();
        _service = null;
    }

    [UnityTest]
    public IEnumerator YieldCacheDuplicateTest_01() {
        yield return null;
    }

    private IEnumerator YieldTest_01() {
        yield return YieldCache_Mirror.WaitForSecondsRealtime(1f);
        Logger.TraceLog($"{nameof(YieldTest_01)}_01");
        yield return YieldCache_Mirror.WaitForSecondsRealtime(2f);
        Logger.TraceLog($"{nameof(YieldTest_01)}_02");
        yield return YieldCache_Mirror.WaitForSecondsRealtime(3f);
        Logger.TraceLog($"{nameof(YieldTest_01)}_03");
    }

    private IEnumerator YieldTest_02() {
        yield return YieldCache_Mirror.WaitForSecondsRealtime(1f);
        Logger.TraceLog($"{nameof(YieldTest_02)}_01");
        yield return YieldCache_Mirror.WaitForSecondsRealtime(2f);
        Logger.TraceLog($"{nameof(YieldTest_02)}_02");
        yield return YieldCache_Mirror.WaitForSecondsRealtime(3f);
        Logger.TraceLog($"{nameof(YieldTest_02)}_03");
    }
}

public static class YieldCache_Mirror {
    
    private class FloatComparer : IEqualityComparer<float> {
        
        bool IEqualityComparer<float>.Equals(float x, float y) => x == y;
        int IEqualityComparer<float>.GetHashCode(float obj) => obj.GetHashCode();
    }

    public static readonly WaitForEndOfFrame waitFrame = new WaitForEndOfFrame();

    public static readonly WaitForFixedUpdate waitFixedUpdate = new WaitForFixedUpdate();

    private static readonly Dictionary<float, WaitForSeconds> _waitSecondsDic = new(new FloatComparer());

    private static readonly Dictionary<float, WaitForSecondsRealtime> _waitSecondsRealtimeDic = new(new FloatComparer());

    public static WaitForSeconds WaitForSeconds(float seconds) {
        if (_waitSecondsDic.TryGetValue(seconds, out var wfs) == false) {
            _waitSecondsDic.Add(seconds, wfs = new WaitForSeconds(seconds));
        }

        return wfs;
    }

    public static WaitForSecondsRealtime WaitForSecondsRealtime(float seconds) {
        if (_waitSecondsRealtimeDic.TryGetValue(seconds, out var wfs) == false) {
            _waitSecondsRealtimeDic.Add(seconds, wfs = new WaitForSecondsRealtime(seconds));
        }
        
        return wfs;
    }
}