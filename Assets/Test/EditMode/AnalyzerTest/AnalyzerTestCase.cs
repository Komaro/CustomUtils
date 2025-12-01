using System.IO;
using Microsoft.CodeAnalysis;
using UnityEngine;

// TODO. Analyze 전용 테스트 케이스 모듈화 혹은 명칭 변경 및 추출 필요
internal struct AnalyzerTestCaseCode {
    
    public string name;
    public string source;
    public TEST_RESULT_CASE_TYPE type;

    public static AnalyzerTestCaseCode? Create(string path) {
        var fullName = Path.GetFileNameWithoutExtension(path);
        var splitName = fullName.Split(".");
        if (splitName.Length < 2) {
            Logger.TraceLog($"Invalid test case Naming || {Path.GetFileName(path)}", Color.yellow);
            return null;
        }
        
        if (EnumUtil.TryConvert<TEST_RESULT_CASE_TYPE>(splitName[1], out var type)) {
            if (IOUtil.TryReadText(path, out var source)) {
                return new AnalyzerTestCaseCode {
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

internal readonly struct AnalyzerTestCaseLog {
    
    public readonly LogType type;
    public readonly string log;

    public AnalyzerTestCaseLog(string log) {
        type = LogType.Log;
        this.log = log;
    }
    
    public AnalyzerTestCaseLog(LogType type, string log) {
        this.type = type;
        this.log = log;
    }

    public override string ToString() => log;
}

internal enum TEST_RESULT_CASE_TYPE {
    NONE,
    SUCCESS,
    FAIL,
    WARNING,
    
    Success = SUCCESS,
    Fail = FAIL,
    Warning = WARNING,
}

// TODO. 추출 혹은 제거 검토
public static class LocationExtension {

    public static string ToPositionString(this Location location) {
        var pos = location.GetLineSpan();
        return $"[{pos.Path}({pos.StartLinePosition.Line + 1}:{pos.StartLinePosition.Character + 1})]";
    }
}