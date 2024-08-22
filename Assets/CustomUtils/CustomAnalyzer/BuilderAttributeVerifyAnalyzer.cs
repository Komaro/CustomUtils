using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BuilderAttributeVerifyAnalyzer : DiagnosticAnalyzer {

    private const string ID = "BuilderAttributeVerfiyAnalyzer";
    private const string BUILDER_ATTRIBUTE_NAME = "BuilderAttribute";
    private const string BUILD_TYPE_ENUM_ATTRIBUTE_NAME = "BuildTypeEnumAttribute";

    private static readonly LocalizableString TITLE = "There is an issue with the enum implementation that is the target of the Enum parameter in the BuilderAttribute";
    private static readonly LocalizableString MESSAGE_FORMAT = "The BuildTypeEnumAttribute implementation cannot be found in the enum target implementation of {0} for the BuilderAttribute";
    private static readonly DiagnosticDescriptor RULE = new(ID, TITLE, MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);

    private static ConcurrentDictionary<ITypeSymbol, bool> _cacheVerifyEnumDic = new();

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.RegisterSymbolAction(AnalyzeAttribute, SymbolKind.NamedType);
    }
    
    private void AnalyzeAttribute(SymbolAnalysisContext context) {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol) {
            return;
        }

        var attributeData = namedTypeSymbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.Name.Equals(BUILDER_ATTRIBUTE_NAME, StringComparison.Ordinal) ?? false);
        if (attributeData == null) {
            return;
        }
        
        foreach (var argument in attributeData.ConstructorArguments) {
            if (argument.Kind == TypedConstantKind.Enum) {
                var enumSymbol = argument.Type?.OriginalDefinition;
                if (enumSymbol != null) {
                    if (_cacheVerifyEnumDic.TryGetValue(enumSymbol, out var isVerify) && isVerify == false) {
                        context.ReportDiagnostic(Diagnostic.Create(RULE, enumSymbol.Locations[0], enumSymbol.Name));
                        return;
                    }
                    
                    if (enumSymbol.GetAttributes().Any(attribute => attribute.AttributeClass?.Name.Equals(BUILD_TYPE_ENUM_ATTRIBUTE_NAME, StringComparison.Ordinal) ?? false)) {
                        _cacheVerifyEnumDic.TryAdd(enumSymbol, true);
                    } else {
                        _cacheVerifyEnumDic.TryAdd(enumSymbol, false);
                        context.ReportDiagnostic(Diagnostic.Create(RULE, enumSymbol.Locations[0], enumSymbol.Name));
                    }
                }
                break;
            }
        }
    }
}
