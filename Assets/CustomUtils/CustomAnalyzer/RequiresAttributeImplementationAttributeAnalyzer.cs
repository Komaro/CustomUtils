﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[Description("RequiresAttributeImplementationAttribute를 가지는 class를 상속받는 경우 반드시 RequiresAttributeImplementationAttribute.implementTargetAttributeType이 지정하는 Type의 Attribute를 구현하여야 한다.")]
public class RequiresAttributeImplementationAttributeAnalyzer : BaseDianosticAnalzyer {

    private const string IMPLEMENT_ID = "RequiresAttributeImplementationAttributeAnalyzer_AttributeImplement";
    private const string ID = "RequiresAttributeImplementationAttribute";
    private const string ATTRIBUTE_NAME = "RequiresAttributeImplementationAttribute";
    
    private static readonly LocalizableString TITLE = "Required attribute missing";
    
    private static readonly LocalizableString IMPLEMENT_MESSAGE_FORMAT = "RequiresAttributeImplementationAttribute must be implemented in an abstract class or interface.";
    private static readonly DiagnosticDescriptor IMPLEMENT_RULE = new(IMPLEMENT_ID, TITLE, IMPLEMENT_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);
    
    private static readonly LocalizableString MESSAGE_FORMAT = "Class '{0}' must have the '{1}' attribute";
    private static readonly DiagnosticDescriptor RULE = new(ID, TITLE, MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);
    
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context) {
        if (context.Symbol.IsAbstract || context.Symbol is not INamedTypeSymbol namedTypeSymbol) {
            return;
        }
        
        var attributeDataSet = namedTypeSymbol.GetAttributes().Select(symbol => symbol.AttributeClass?.Name).ToImmutableHashSet();
        foreach (var data in FindAttributes(namedTypeSymbol, ATTRIBUTE_NAME)) {
            var implementType = data.ConstructorArguments[0].Value;
            // TODO. 타입 텍스트 확인 시 간혹 FullName을 체크하여야 하는 경우가 발생함
            // TODO. CreateAssetMenu 같은 경우 implementType이 UnityEngine.CreateAssetMenu로 지정됨
            if (implementType != null && attributeDataSet.Contains(implementType.ToString()) == false) {
                context.ReportDiagnostic(Diagnostic.Create(RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, implementType.ToString()));
            }
        }
    }
}