using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[Description("BuilderAttribute의 생성자에서 object buildType의 경우 BuildTypeEnumAttribute를 가지는 enum인 값을 전달하여야 한다.")]
public class BuilderAttributeVerifyAnalyzer : BaseDianosticAnalzyer {

    private const string MISSING_ID = "BuilderAttributeVerfiyAnalyzer_Missing";
    private const string CLASS_IMPLEMENT_ID = "BuilderAttributeVerfiyAnalyzer_Class";
    
    private const string BUILDER_ATTRIBUTE_NAME = "BuilderAttribute";
    private const string BUILD_TYPE_ENUM_ATTRIBUTE_NAME = "BuildTypeEnumAttribute";

    private static readonly LocalizableString TITLE = "There is an issue with the enum implementation that is the target of the Enum parameter in the BuilderAttribute";

    private static readonly LocalizableString MISSING_MESSAGE_FORMAT = "Constructor parameter 'buildType' of BuilderAttribute must be an enum type. The current type is {0}";
    private static readonly DiagnosticDescriptor MISSING_RULE = new(MISSING_ID, TITLE, MISSING_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly LocalizableString CLASS_IMPLEMENT_MESSAGE_FORMAT = "The BuildTypeEnumAttribute implementation cannot be found in the enum target implementation of {0} for the BuilderAttribute";
    private static readonly DiagnosticDescriptor CLASS_IMPLEMENT_RULE = new(CLASS_IMPLEMENT_ID, TITLE, CLASS_IMPLEMENT_MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MISSING_RULE, CLASS_IMPLEMENT_RULE);

    private static readonly ConcurrentDictionary<ITypeSymbol, bool> _cacheVerifyEnumDic = new();

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.RegisterSymbolAction(AnalyzeAttribute, SymbolKind.NamedType);
    }
    
    private void AnalyzeAttribute(SymbolAnalysisContext context) {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol || TryFindAttribute(namedTypeSymbol, BUILDER_ATTRIBUTE_NAME, out var attributeData) == false) {
            return;
        }

        foreach (var argument in attributeData.ConstructorArguments) {
            if (argument.Type == null) {
                continue;
            }

            var enumSymbol = argument.Type.OriginalDefinition;
            if (argument.Kind != TypedConstantKind.Enum) {
                context.ReportDiagnostic(Diagnostic.Create(MISSING_RULE, GetSyntaxReferenceLocation(attributeData), enumSymbol.Name));
                return;
            }
            
            if (_cacheVerifyEnumDic.TryGetValue(enumSymbol, out var isVerify) && isVerify == false) {
                context.ReportDiagnostic(Diagnostic.Create(CLASS_IMPLEMENT_RULE, GetSyntaxReferenceLocation(attributeData), enumSymbol.Name));
                return;
            }
            
            if (enumSymbol.GetAttributes().Any(attribute => attribute.AttributeClass?.Name.Equals(BUILD_TYPE_ENUM_ATTRIBUTE_NAME, StringComparison.Ordinal) ?? false)) {
                _cacheVerifyEnumDic.TryAdd(enumSymbol, true);
            } else {
                _cacheVerifyEnumDic.TryAdd(enumSymbol, false);
                context.ReportDiagnostic(Diagnostic.Create(CLASS_IMPLEMENT_RULE, GetSyntaxReferenceLocation(attributeData), enumSymbol.Name));
            }
            
            break;
        }
    }
}
