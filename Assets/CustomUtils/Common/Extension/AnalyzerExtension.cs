using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

public static class AnalyzerExtension {
    
    public static IEnumerable<AttributeData> GetAttributeData(this INamedTypeSymbol namedTypeSymbol, string name) => namedTypeSymbol == null 
        ? Enumerable.Empty<AttributeData>() 
        : namedTypeSymbol.GetAttributes().Where(data => data.AttributeClass?.Name.Equals(name, StringComparison.Ordinal) ?? false);

    public static IEnumerable<IMethodSymbol> GetMethodSymbols(this INamedTypeSymbol namedTypeSymbol) => namedTypeSymbol == null 
        ? Enumerable.Empty<IMethodSymbol>() 
        : namedTypeSymbol.GetMembers().Where(symbol => symbol.Kind == SymbolKind.Method).Cast<IMethodSymbol>();
}