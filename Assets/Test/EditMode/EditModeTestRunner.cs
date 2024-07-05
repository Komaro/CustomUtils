using System;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

public class EditModeTestRunner {
    
    
    [Test]
    public void RequiresStaticMethodImplementationAttributeAnalyzerTest() {
        // TODO. .cs 파일을 @"" 구격에 맞춰 컨버트 해주는 기능 추가
        var testSource = $@"
        using System;
        using UnityEditor.Callbacks;

        public class TestClass : AbstractClass {{

            [DidReloadScripts]
            private static void TestMethod() {{

            }}

            [DidReloadScripts]
            private static void SampleMethod() {{

            }}
        }}

        [RequiresStaticMethodImplementation(""TestMethod"", typeof(DidReloadScripts))]
        [RequiresStaticMethodImplementation(""SampleMethod"", typeof(DidReloadScripts))]
        public abstract class AbstractClass {{
            
        }}

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class RequiresStaticMethodImplementationAttribute : Attribute {{

            public string methodName;
            public Type includeAttributeType;

            public RequiresStaticMethodImplementationAttribute(string methodName) => this.methodName = methodName;
            public RequiresStaticMethodImplementationAttribute(string methodName, Type includeAttributeType) : this(methodName) => this.includeAttributeType = includeAttributeType;
        }}";

        // TODO. 모듈화
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(testSource));
        var metaDataReferences = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.IsDynamic == false && string.IsNullOrEmpty(assembly.Location) == false)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location)).Cast<MetadataReference>();
        var testCompilation = CSharpCompilation.Create("AnalyzerTestAssembly", new[] { syntaxTree }, metaDataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var testTargetAnalyzer = new RequiresStaticMethodImplementationAttributeAnalyzer();
        var testAnalyzerCompilation = testCompilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(testTargetAnalyzer));
        var result = testAnalyzerCompilation.GetAnalyzerDiagnosticsAsync().Result;
        
        Logger.TraceLog(result.ToStringCollection(x => $"[{x.Id}] {x.Location}\n{x.GetMessage()}", "\n"));
        Assert.AreEqual(0, result.Length);
    }
}