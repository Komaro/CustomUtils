using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AttributeAbstractAndInterfaceConstraintsAnalyzer : BaseDianosticAnalzyer {

    private const string IMPLEMENT_ID = "AttributeConstraintsAnalyzer";
    
    private static readonly LocalizableString TITLE = "Attribute target missing";
    private static readonly LocalizableString FORMAT = "{0} must be implemented in an abstract class or interface.";
    private static readonly DiagnosticDescriptor RULE = new(IMPLEMENT_ID, TITLE, FORMAT, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private readonly ImmutableHashSet<string> targetAttributes = ImmutableHashSet.Create(
        "RequiresStaticMethodImplementationAttribute", "RequiresAttributeImplementationAttribute");
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>(RULE);
    
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.RegisterSymbolAction(AnalyzeSymbolImplement, SymbolKind.NamedType);
    }

    private void AnalyzeSymbolImplement(SymbolAnalysisContext context) {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol) {
            return;
        }
        
        if (TryGetAttributeData(namedTypeSymbol, out var attributes)) {
            if (attributes.Any(attribute => attribute.AttributeClass != null && targetAttributes.Contains(attribute.AttributeClass.Name))) {
                if (namedTypeSymbol.IsAbstract == false && namedTypeSymbol.TypeKind != TypeKind.Interface) {
                    context.ReportDiagnostic(Diagnostic.Create(RULE, namedTypeSymbol.Locations[0], namedTypeSymbol.Name));
                }
            }
        }
    }
}
