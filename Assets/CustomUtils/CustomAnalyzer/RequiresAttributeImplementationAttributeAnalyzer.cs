using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiresAttributeImplementationAttributeAnalyzer : DiagnosticAnalyzer {

    private const string ID = "RequiresAttributeImplementationAttribute";
    private static readonly LocalizableString TITLE = "Required attribute missing";
    private static readonly LocalizableString MESSAGE_FORMAT = "Class '{0}' must have the '{1}' attribute";
    private static readonly DiagnosticDescriptor RULE = new DiagnosticDescriptor(ID, TITLE, MESSAGE_FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private const string ATTRIBUTE_NAME = "RequiresAttributeImplementationAttribute";
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RULE);
    
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context) {
        var namedTypeSymbol = (INamedTypeSymbol) context.Symbol;
        var baseType = namedTypeSymbol.BaseType;
        if (baseType == null || namedTypeSymbol.IsAbstract) {
            return;
        }
        
        var requiresAttributeData = baseType.GetAttributes().FirstOrDefault(x => x.AttributeClass.Name == ATTRIBUTE_NAME);
        if (requiresAttributeData != null) {
            var implementType = requiresAttributeData.ConstructorArguments.FirstOrDefault().Value;
            if (implementType != null) {
                var implementTypeName = implementType.ToString();
                var hasAttribute = namedTypeSymbol.GetAttributes().Any(x => x.AttributeClass.Name == implementTypeName);
                if (hasAttribute == false) {
                    var diagnostic = Diagnostic.Create(RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, implementTypeName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}