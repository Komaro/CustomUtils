using UnityEditor.TestTools.CodeCoverage;
using UnityEngine.TestTools;

public static class TestUtil {

    public static void EvaluateCodeCoverageResult() {
        if (Coverage.enabled) {
            Events.onCoverageSessionFinished += OnFinishCodeCoverageRecord;
        }
    }

    private static void OnFinishCodeCoverageRecord(SessionEventInfo info) {
        foreach (var path in info.SessionResultPaths) {
            Logger.TraceLog(path);
        }

        // TODO. json 획득 후 결과 평가 후 로그 출력
        
        Events.onCoverageSessionFinished -= OnFinishCodeCoverageRecord;
    }
}
