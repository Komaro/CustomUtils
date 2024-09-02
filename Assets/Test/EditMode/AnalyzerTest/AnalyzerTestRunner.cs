using System;
using System.Collections;
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
    
    // public static IEnumerable RequiresUnityEditorAttributeRepeatProvider {
    //     get {
    //         for (var i = 0; i < 10; i++) {public abstract claclass TestAAbastractA {}ClasTestAClassA
    //             yield return new TestCaseData(typeof(RequiresUnityEditorAttributeAnalyzer), "Test/EditMode/AnalyzerTestCaseCode/UnityEditorAnalyzerTestCase");
    //         }
    //     }
    // }
    
    // [TestCaseSource(nameof(RequiresUnityEditorAttributeRepeatProvider))]
    // public async Task RequiresUnityEditorAttributeAnalyzerRepeatTest(Type type, string testCaseCodeFolder) => await AnalyzerRunner.AnalyzerTest(type, testCaseCodeFolder);
    
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
        
        var taskList = AnalyzerRunner.GetTestCaseTaskList(type, Path.Combine(Application.dataPath, testCaseCodeFolder));
        async void Action() {
            await Task.WhenAll(taskList);
        }
        
        Measure.Method(Action)
            .WarmupCount(1)
            .MeasurementCount(10)
            .IterationsPerMeasurement(5)
            .SampleGroup(analyzerPerformanceTestGroup)
            .GC()
            .Run();
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

    public static List<Task<TestCaseLog>> GetTestCaseTaskList(Type type, string testCaseCodeFolder) {
        if (Activator.CreateInstance(type) is not DiagnosticAnalyzer analyzer) {
            Logger.TraceError($"{type.Name} is an invalid {nameof(Type)}");
            return new List<Task<TestCaseLog>>();
        }
        
        var testCaseCodeList = new List<TestCaseCode>();
        foreach (var filePath in Directory.GetFiles(testCaseCodeFolder, Constants.Extension.TEST_CASE_FILTER, SearchOption.AllDirectories)) {
            var testCaseCode = TestCaseCode.Create(filePath);
            if (testCaseCode.HasValue) {
                testCaseCodeList.Add(testCaseCode.Value);
            }
        }
        
        var metaDataReferences = AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)).Cast<MetadataReference>().ToList();
        var taskList = new List<Task<TestCaseLog>>();
        foreach (var testCaseCode in testCaseCodeList.OrderBy(x => x.type)) {
            taskList.Add(Task.Run(() => {
                var testCompilation = CSharpCompilation.Create("AnalyzerTestAssembly"
                    , new [] { CSharpSyntaxTree.ParseText(SourceText.From(testCaseCode.source), path:testCaseCode.name) }
                    , metaDataReferences
                    , new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var testAnalyzerCompilation = testCompilation.WithAnalyzers(ImmutableArray.Create(analyzer));
                var task = testAnalyzerCompilation.GetAnalyzerDiagnosticsAsync();
                var result = task.Result;
                switch (testCaseCode.type) {
                    case TEST_RESULT_CASE_TYPE.SUCCESS when result.Length != 0:
                        return new TestCaseLog(LogType.Error, $"[Script Test] Failed || {testCaseCode.name}\n\t{result.ToStringCollection(x => $"[{x.Id}] {x.Location.ToPositionString()} || {x.GetMessage()}", "\n\t")}");
                    case TEST_RESULT_CASE_TYPE.FAIL when result.Length == 0: 
                        return new TestCaseLog(LogType.Error, $"[Script Test] Failed || {testCaseCode.name}\n\t{result.ToStringCollection(x => $"[{x.Id}] {x.Location.ToPositionString()} || {x.GetMessage()}", "\n\t")}");
                }
                
                return new TestCaseLog($"[Script Test] Success || {testCaseCode.name}");
            }));
        }
        
        return taskList;
    }
} 