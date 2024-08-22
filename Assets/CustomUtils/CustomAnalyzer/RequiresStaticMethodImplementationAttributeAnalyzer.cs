using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[Description("RequiresStaticMethodImplementationAttribute를 가지는 class를 상속받는 경우 반드시 RequiresStaticMethodImplementationAttribute.methodName을 반드시 static method로 구현하여야 한다." +
             "추가 옵션으로 RequiresStaticMethodImplementationAttribute.includeAttributeType을 지정하는 경우 지정된 Attribute Type을 반드시 구현하여야 한다.")]
public class RequiresStaticMethodImplementationAttributeAnalyzer : DiagnosticAnalyzer {

    private const string IMPLEMENT_ID = "RequiresStaticMethodImplementationAttribute_AttributeImplement";
    private const string STATIC_ID = "RequiresStaticMethodImplementationAttribute_Static";
    private const string ATTRIBUTE_ID = "RequiresStaticMethodImplementationAttribute_Attribute";
    private const string ATTRIBUTE_NAME = "RequiresStaticMethodImplementationAttribute";

    private static readonly LocalizableString TITLE = "Required static method missing";

    private static readonly LocalizableString IMPLEMENT_MESSAGE_FORMAT = "RequiresStaticMethodImplementationAttribute must be implemented in an abstract class or interface.";
    private static readonly DiagnosticDescriptor IMPLEMENT_RULE = new(IMPLEMENT_ID, TITLE, IMPLEMENT_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);
    
    private static readonly LocalizableString STATIC_MESSAGE_FORMAT = "Class '{0}' must have the '{1}' static method";
    private static readonly DiagnosticDescriptor STATIC_RULE = new(STATIC_ID, TITLE, STATIC_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly LocalizableString ATTRIBUTE_MESSAGE_FORMAT = "Class '{0}' must have the '{1}' attribute";
    private static readonly DiagnosticDescriptor ATTRIBUTE_RULE = new(ATTRIBUTE_ID, TITLE, ATTRIBUTE_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(IMPLEMENT_RULE, STATIC_RULE, ATTRIBUTE_RULE);

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeSymbolRestrict, SymbolKind.NamedType);
    }

    private void AnalyzeSymbolRestrict(SymbolAnalysisContext context) {
        if (context.Symbol.IsAbstract) {
            return;
        }
        
        var namedTypeSymbol = (INamedTypeSymbol) context.Symbol;
        var attributeData = GetInheritedClassAndInterfaces(namedTypeSymbol).SelectMany(symbol => symbol.GetAttributes()).Where(attribute => attribute.AttributeClass?.Name.Equals(ATTRIBUTE_NAME, StringComparison.Ordinal) ?? false).ToImmutableArray();
        if (attributeData.Length <= 0) {
            return;
        }

        var ordinaryMethodDic = namedTypeSymbol.GetMembers().Where(symbol => symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary }).ToImmutableDictionary(symbol => symbol.Name, symbol => symbol as IMethodSymbol);
        foreach (var data in attributeData) {
            var implementMethod = data.ConstructorArguments[0].Value;
            if (implementMethod != null) {
                var implementName = implementMethod.ToString();
                if (ordinaryMethodDic.TryGetValue(implementName, out var methodSymbol) == false || methodSymbol.IsStatic == false) {
                    context.ReportDiagnostic(Diagnostic.Create(STATIC_RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, implementName));
                }

                if (methodSymbol != null && data.ConstructorArguments.Length > 1 && data.ConstructorArguments[1].Value != null) {
                    implementName = data.ConstructorArguments[1].Value.ToString();
                    if (methodSymbol.GetAttributes().Any(x => x.AttributeClass?.ToString().Equals(implementName, StringComparison.Ordinal) ?? false) == false) {
                        context.ReportDiagnostic(Diagnostic.Create(ATTRIBUTE_RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, implementName));
                    }
                }
            }
        }
    }
    
    private IEnumerable<INamedTypeSymbol> GetInheritedClassAndInterfaces(INamedTypeSymbol symbol) {
        var type = symbol;
        while ((type = type.BaseType) != null) {
            yield return type;
            foreach (var interfaceType in type.AllInterfaces) {
                yield return interfaceType;
            }
        }
    }
}
