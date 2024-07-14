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
using UnityEngine;

public class AnalyzerTestRunner {
    
    [TestCase(typeof(RequiresStaticMethodImplementationAttributeAnalyzer), "Test/EditMode/AnalyzerTestCaseCode/RequiresStaticMethodTestCase")]
    [TestCase(typeof(RequiresAttributeImplementationAttributeAnalyzer), "Test/EditMode/AnalyzerTestCaseCode/RequiresAttributeTestCase")]
    public async Task AnalyzerTest(Type type, string testCaseCodeFolder) {
        var path = Path.Combine(Application.dataPath, testCaseCodeFolder);
        if (Directory.Exists(path)) {
            var logs = await AnalyzerTestTask(type, path);
            foreach (var result in logs) {
                Logger.Log(result);
            }
            
            var errorCount = logs.Count(x => x.type == LogType.Error);
            Logger.Log("\nAnalyzer test complete\n" +
                       $"Success : {logs.Length - errorCount}\n" +
                       $"Failed  : {errorCount}");
                
            Assert.IsTrue(errorCount == 0);
        } else {
            Logger.TraceError($"{nameof(testCaseCodeFolder)} is an invalid path || {testCaseCodeFolder}");
        }
    }

    private Task<TestCaseLog[]> AnalyzerTestTask(Type type, string testCaseCodeFolder) {
        if (Activator.CreateInstance(type) is not DiagnosticAnalyzer analyzer) {
            Logger.TraceError($"{type.Name} is an invalid {nameof(Type)}");
            return Task.CompletedTask as Task<TestCaseLog[]>;
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
        foreach (var testCaseCode in testCaseCodeList) {
            taskList.Add(Task.Run(() => {
                var testCompilation = CSharpCompilation.Create("AnalyzerTestAssembly"
                    , new [] { CSharpSyntaxTree.ParseText(SourceText.From(testCaseCode.source), path:testCaseCode.name) }
                    , metaDataReferences
                    , new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var testAnalyzerCompilation = testCompilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
                var task = testAnalyzerCompilation.GetAnalyzerDiagnosticsAsync();
                var result = task.Result;
                switch (testCaseCode.type) {
                    case TEST_RESULT_CASE_TYPE.SUCCESS when result.Length != 0:
                        return new TestCaseLog(LogType.Error, $"[Script Test] Failed || {testCaseCode.name}");
                    case TEST_RESULT_CASE_TYPE.FAIL when result.Length == 0: 
                        return new TestCaseLog(LogType.Error, $"[Script Test] Failed || {testCaseCode.name}\n\t{result.ToStringCollection(x => $"[{x.Id}] {x.Location.ToPositionString()} || {x.GetMessage()}", "\n\t")}");
                }
                
                return new TestCaseLog($"[Script Test] Success || {testCaseCode.name}");
            }));
        }

        return Task.WhenAll(taskList);
    }
}
