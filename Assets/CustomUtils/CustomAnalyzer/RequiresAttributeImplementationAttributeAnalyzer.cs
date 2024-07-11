using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[Description("RequiresAttributeImplementationAttribute를 가지는 class를 상속받는 경우 반드시 RequiresAttributeImplementationAttribute.implementTargetAttributeType이 지정하는 Type의 Attribute를 구현하여야 한다.")]
public class RequiresAttributeImplementationAttributeAnalyzer : DiagnosticAnalyzer {

    private const string ID = "RequiresAttributeImplementationAttribute";
    private const string ATTRIBUTE_NAME = "RequiresAttributeImplementationAttribute";

    private static readonly LocalizableString TITLE = "Required attribute missing";
    private static readonly LocalizableString MESSAGE_FORMAT = "Class '{0}' must have the '{1}' attribute";
    private static readonly DiagnosticDescriptor RULE = new DiagnosticDescriptor(ID, TITLE, MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context) {
        var namedTypeSymbol = (INamedTypeSymbol) context.Symbol;
        var baseType = namedTypeSymbol.BaseType;
        if (baseType == null || namedTypeSymbol.IsAbstract) {
            return;
        }
    
        var requiresAttributeData = baseType.GetAttributes().Where(data => data.AttributeClass?.Name.Equals(ATTRIBUTE_NAME, StringComparison.Ordinal) ?? false);
        foreach (var data in requiresAttributeData) {
            var implementType = data.ConstructorArguments[0].Value;
            if (implementType != null) {
                var implementName = implementType.ToString();
                if (namedTypeSymbol.GetAttributes().Any(implementData => implementData.AttributeClass?.ToString().Equals(implementName, StringComparison.Ordinal) ?? false) == false) {
                    context.ReportDiagnostic(Diagnostic.Create(RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, implementName));
                }
            }
        }
    }
}