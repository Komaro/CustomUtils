using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

public class AnalyzerTestRunner {

    [TestCase(typeof(RequiresStaticMethodImplementationAttributeAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/RequiresStaticMethodTestCase")]
    [TestCase(typeof(RequiresAttributeImplementationAttributeAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/RequiresAttributeTestCase")]
    [TestCase(typeof(BuilderAttributeVerifyAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/BuilderAttributeVerifyAnalyzerTestCase")]
    [TestCase(typeof(AttributeAbstractAndInterfaceConstraintsAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/AttributeAbstractAndInterfaceConstraintsAnalyzerTestCase")]
    public async Task AnalyzerTestCaseTest(Type type, string testCaseCodeFolder) => await AnalyzerRunner.AnalyzerTest(type, testCaseCodeFolder);

    [Performance]
    [TestCase(typeof(RequiresStaticMethodImplementationAttributeAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/RequiresStaticMethodTestCase")]
    [TestCase(typeof(RequiresAttributeImplementationAttributeAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/RequiresAttributeTestCase")]
    [TestCase(typeof(BuilderAttributeVerifyAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/BuilderAttributeVerifyAnalyzerTestCase")]
    [TestCase(typeof(AttributeAbstractAndInterfaceConstraintsAnalyzer), "Test/EditMode/AnalyzerTest/AnalyzerTestCaseCode/AttributeAbstractAndInterfaceConstraintsAnalyzerTestCase")]
    public void AnalyzerTestCasePerformanceTest(Type type, string testCaseCodeFolder) {
        var analyzerPerformanceTestGroup = new SampleGroup("PerformanceGroup");
        Measure.Method(Action)
            .WarmupCount(1)
            .MeasurementCount(10)
            .IterationsPerMeasurement(5)
            .SampleGroup(analyzerPerformanceTestGroup)
            .GC()
            .Run();
        
        async void Action() => await Task.WhenAll(AnalyzerRunner.GetTestCaseTaskList(type, Path.Combine(Application.dataPath, testCaseCodeFolder)));
    }
}

internal static class AnalyzerRunner {
    
    public static async Task AnalyzerTest(Type type, string testCaseCodeFolder) {
        var path = Path.Combine(Application.dataPath, testCaseCodeFolder);
        if (Directory.Exists(path)) {
            var logs = await Task.WhenAll(GetTestCaseTaskList(type, path));
            foreach (var result in logs) {
                Logger.Log(result);
            }
            
            var errorCount = logs.Count(x => x.type == LogType.Error);
            Logger.Log("\nAnalyzer test complete\n" +
                       $"Success : {logs.Length - errorCount}\n" +
                       $"Failed  : {errorCount}");

            if (errorCount != 0) {
                Logger.TraceError(logs.Where(log => log.type == LogType.Error).ToStringCollection('\n'));
                Assert.Fail();
            }
        } else {
            Logger.TraceError($"{nameof(testCaseCodeFolder)} is an invalid path || {testCaseCodeFolder}");
        }
    }

    public static List<Task<AnalyzerTestCaseLog>> GetTestCaseTaskList(Type type, string testCaseCodeFolder) {
        if (Activator.CreateInstance(type) is not DiagnosticAnalyzer analyzer) {
            Logger.TraceError($"{type.Name} is an invalid {nameof(Type)}");
            return new List<Task<AnalyzerTestCaseLog>>();
        }
        
        var testCaseCodeList = Directory.GetFiles(testCaseCodeFolder, Constants.Extension.TEST_CASE_FILTER, SearchOption.AllDirectories).ToList(AnalyzerTestCaseCode.Create);
        var metaDataReferences = AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)).Cast<MetadataReference>().ToList();
        var taskList = new List<Task<AnalyzerTestCaseLog>>();
        foreach (var testCaseCode in testCaseCodeList.Where(x => x.HasValue).OrderBy(x => x.Value.type)) {
            taskList.Add(Task.Run(async () => {
                var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                var testCompilation = CSharpCompilation.Create("AnalyzerTestAssembly", new [] { CSharpSyntaxTree.ParseText(SourceText.From(testCaseCode.Value.source), path:testCaseCode.Value.name) }
                    , metaDataReferences, compilationOptions);
                var diagnostics = await testCompilation.WithAnalyzers(ImmutableArray.Create(analyzer)).GetAnalyzerDiagnosticsAsync();
                switch (testCaseCode.Value.type) {
                    case TEST_RESULT_CASE_TYPE.SUCCESS when diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error):
                    case TEST_RESULT_CASE_TYPE.FAIL when diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) == false:
                    case TEST_RESULT_CASE_TYPE.WARNING when diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning) == false:
                        return new AnalyzerTestCaseLog(LogType.Error, $"[Script Test] Failed || {testCaseCode.Value.name}\n\t{diagnostics.ToStringCollection(x => $"[{x.Id}] {x.Location.ToPositionString()} || {x.GetMessage()}", "\n\t")}");
                }

                return new AnalyzerTestCaseLog($"[Script Test] Success || {testCaseCode.Value.name}");
                // return new TestCaseLog($"[Script Test] Success || {testCaseCode.Value.name}\n{diagnostics.ToStringCollection(dig => dig.ToString(), "\n")}");
            }));
        }

        return taskList;
    }
}