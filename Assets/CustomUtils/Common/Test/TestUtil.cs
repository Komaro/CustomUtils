using OpenCover.Framework.Model;
using UnityEditor.TestTools.CodeCoverage;
using UnityEngine;
using UnityEngine.TestTools;

public static class TestUtil {

    public static void EvaluateCodeCoverageResult() {
        if (Coverage.enabled) {
            Events.onCoverageSessionFinished += OnFinishCodeCoverageRecord;
        }
    }

    // TODO. 단순 출력. 향후 기능 고도화 필요
    private static void OnFinishCodeCoverageRecord(SessionEventInfo info) {
        foreach (var path in info.SessionResultPaths) {
            if (XmlUtil.TryDeserializeAsClassFromFile<CoverageSession>(path, out var session)) {
                var summary = session.Summary;
                if (summary.VisitedSequencePoints == 0) {
                    Logger.TraceLog("Origin Session");
                    continue;
                }
                
                Logger.TraceLog($"[SequencePoints] {summary.NumSequencePoints} || {summary.VisitedSequencePoints} || {GetPassText(summary.IsSequencePointsPass())}");
                Logger.TraceLog($"[BranchPoints] {summary.NumBranchPoints} || {summary.VisitedBranchPoints} || {GetPassText(summary.IsBranchPointsPass())}");
                Logger.TraceLog($"[Classes] {summary.NumClasses} || {summary.VisitedClasses} || {GetPassText(summary.IsClassesPass())}");
                Logger.TraceLog($"[Methods] {summary.NumMethods} || {summary.VisitedMethods} || {GetPassText(summary.IsMethodsPass())}");
            }
        }
        
        Events.onCoverageSessionFinished -= OnFinishCodeCoverageRecord;
    }

    private static string GetPassText(bool isPass) => isPass ? "PASS".GetColorString(Color.cyan) : "FAIL".GetColorString(Color.red);
}

public static class SummaryExtension {
    
    public static bool IsSequencePointsPass(this Summary summary) => summary.NumSequencePoints == summary.VisitedSequencePoints;
    public static bool IsBranchPointsPass(this Summary summary) => summary.NumBranchPoints == summary.VisitedBranchPoints;
    public static bool IsClassesPass(this Summary summary) => summary.NumClasses == summary.VisitedClasses;
    public static bool IsMethodsPass(this Summary summary) => summary.NumMethods == summary.VisitedMethods;
}
