using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EditModeTestRunner {
    
    [TestCase(1)]
    [TestCase(2)]
    public void CaseTest(int value) {
        Logger.TraceLog($"{nameof(CaseTest)} param || {value}");
    }
}
