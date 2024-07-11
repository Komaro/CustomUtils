using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[Description("RequiresStaticMethodImplementationAttribute를 가지는 class를 상속받는 경우 반드시 RequiresStaticMethodImplementationAttribute.methodName을 반드시 static method로 구현하여야 한다." +
             "추가 옵션으로 RequiresStaticMethodImplementationAttribute.includeAttributeType을 지정하는 경우 지정된 Attribute Type을 반드시 구현하여야 한다.")]
public class RequiresStaticMethodImplementationAttributeAnalyzer : DiagnosticAnalyzer {

    private const string STATIC_ID = "RequiresStaticMethodImplementationAttribute_Static";
    private const string ATTRIBUTE_ID = "RequiresStaticMethodImplementationAttribute_Attribute";
    private const string ATTRIBUTE_NAME = "RequiresStaticMethodImplementationAttribute";

    private static readonly LocalizableString TITLE = "Required static method missing";

    private static readonly LocalizableString STATIC_MESSAGE_FORMAT = "Class '{0}' must have the '{1}' static method";
    private static readonly DiagnosticDescriptor STATIC_RULE = new(STATIC_ID, TITLE, STATIC_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly LocalizableString ATTRIBUTE_MESSAGE_FORMAT = "Class '{0}' must have the '{1}' attribute";
    private static readonly DiagnosticDescriptor ATTRIBUTE_RULE = new(ATTRIBUTE_ID, TITLE, ATTRIBUTE_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(STATIC_RULE, ATTRIBUTE_RULE);

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
        var ordinaryMethodSymbols =  namedTypeSymbol.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(symbol => symbol.MethodKind == MethodKind.Ordinary);
        foreach (var requiresData in requiresAttributeData) {
            var implementMethod = requiresData.ConstructorArguments[0].Value;
            if (implementMethod != null) {
                var implementName = implementMethod.ToString();
                var targetMethodSymbol = ordinaryMethodSymbols.FirstOrDefault(symbol => symbol.Name.Equals(implementName, StringComparison.Ordinal));
                if (targetMethodSymbol == null || targetMethodSymbol.IsStatic == false) {
                    context.ReportDiagnostic(Diagnostic.Create(STATIC_RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, implementName));
                }

                var implementAttributeType = requiresData.ConstructorArguments.Length > 1 ? requiresData.ConstructorArguments[1].Value : null;
                if (implementAttributeType != null && targetMethodSymbol != null) {
                    implementName = implementAttributeType.ToString();
                    if (targetMethodSymbol.GetAttributes().Any(data => data.AttributeClass?.ToString().Equals(implementName, StringComparison.Ordinal) ?? false) == false) {
                        context.ReportDiagnostic(Diagnostic.Create(ATTRIBUTE_RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, implementName));
                    }
                }
            }
        }
    }
}
