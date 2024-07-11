using System.IO;
using Microsoft.CodeAnalysis;
using UnityEngine;

internal struct TestCaseCode {
    
    public string name;
    public string source;
    public TEST_RESULT_CASE_TYPE type;

    public static TestCaseCode? Create(string path) {
        var fullName = Path.GetFileNameWithoutExtension(path);
        var splitName = fullName.Split(".");
        if (splitName.Length < 2) {
            Logger.TraceLog($"Invalid test case Naming || {Path.GetFileName(path)}", Color.yellow);
            return null;
        }
        
        if (EnumUtil.TryGetValue<TEST_RESULT_CASE_TYPE>(splitName[1], out var type)) {
            if (SystemUtil.TryReadAllText(path, out var source)) {
                return new TestCaseCode {
                    name = fullName,
                    source = source,
                    type = type,
                };
            }

            Logger.TraceLog("Failed to read test case", Color.red);
            return null;
        }

        Logger.TraceLog($"Invalid test case {nameof(TEST_RESULT_CASE_TYPE)} || {splitName[1]}", Color.yellow);
        return null;
    }
}

internal readonly struct TestCaseLog {
    
    public readonly LogType type;
    public readonly string log;

    public TestCaseLog(string log) {
        type = LogType.Log;
        this.log = log;
    }
    
    public TestCaseLog(LogType type, string log) {
        this.type = type;
        this.log = log;
    }

    public override string ToString() => log;
}

internal enum TEST_RESULT_CASE_TYPE {
    NONE,
    SUCCESS,
    FAIL,
    
    Success = SUCCESS,
    Fail = FAIL,
}

public static class LocationExtension {

    public static string ToPositionString(this Location location) {
        var pos = location.GetLineSpan();
        return $"[{pos.Path}({pos.StartLinePosition.Line + 1}:{pos.StartLinePosition.Character + 1})]";
    }
}