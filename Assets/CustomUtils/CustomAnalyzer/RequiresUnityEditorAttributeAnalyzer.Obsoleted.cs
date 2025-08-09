// TODO. 쓸모 없음. 샘플로 남김
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// [DiagnosticAnalyzer(LanguageNames.CSharp)]
[Obsolete("Useless analyzer. Just sample")]
public abstract class RequiresAttributeAnalyzer : DiagnosticAnalyzer {

    private const string ID = "RequiresUnityEditorAttribute";
    private const string ATTRIBUTE_NAME_PREFIX = "RequiresUnityEditor";
    
    private static readonly LocalizableString TITLE = "Required 'using UnityEditor'";
    private static readonly LocalizableString MESSAGE_FORMAT = "Class '{0}' must have the 'using UnityEditor'";
    private static readonly DiagnosticDescriptor RULE = new(ID, TITLE, MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private readonly ConcurrentDictionary<int, HashSet<string>> _usingSetDic = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, Location>> _classLocationDic = new();
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);
    
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }
    
    private void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context) {
        if (_usingSetDic.TryGetValue(context.Compilation.GetHashCode(), out var usingSet)) {
            if (usingSet.Contains(context.Node.SyntaxTree.FilePath)) {
                return;
            }
            
            if (context.Node is UsingDirectiveSyntax syntax && syntax.Name.ToString().StartsWith("UnityEditor")) {
                usingSet.Add(syntax.SyntaxTree.FilePath);
            }
        }
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context) {
        if (_classLocationDic.TryGetValue(context.Compilation.GetHashCode(), out var locationDic)) {
            if (locationDic.ContainsKey(context.Node.SyntaxTree.FilePath)) {
                return;
            }

            if (context.Node is ClassDeclarationSyntax syntax) {
                if (syntax.AttributeLists.SelectMany(x => x.Attributes).Any(attributeSyntax => attributeSyntax.Name.ToString().StartsWith(ATTRIBUTE_NAME_PREFIX, StringComparison.Ordinal))) {
                    locationDic.TryAdd(context.Node.SyntaxTree.FilePath, syntax.GetLocation());
                }
            }
        }
    }

    private void AnalyzeCompilationStart(CompilationStartAnalysisContext context) {
        var id = context.Compilation.GetHashCode();
        _usingSetDic.TryAdd(id, new HashSet<string>());
        _classLocationDic.TryAdd(id, new ConcurrentDictionary<string, Location>());
        
        context.RegisterCompilationEndAction(AnalyzeCompilationEnd);
    }

    private void AnalyzeCompilationEnd(CompilationAnalysisContext context) {
        var id = context.Compilation.GetHashCode();
        if (_classLocationDic.TryGetValue(id, out var locationDic) && _usingSetDic.TryGetValue(id, out var usingSet)) {
            foreach (var (path, location) in locationDic) {
                if (usingSet.Contains(path) == false) {
                    context.ReportDiagnostic(Diagnostic.Create(RULE, location, path));
                }
            }
            
            usingSet.Clear();
            locationDic.Clear();
            _usingSetDic.TryRemove(id, out _);
            _classLocationDic.TryRemove(id, out _);
        }
    }
}
