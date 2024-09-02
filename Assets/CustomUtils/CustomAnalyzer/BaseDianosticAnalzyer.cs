using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class BaseDianosticAnalzyer : DiagnosticAnalyzer {
    
    private static ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<AttributeData>> _cacheSymbolAttributeDic = new();

    public static bool TryGetAttributeData(INamedTypeSymbol symbol, out ImmutableArray<AttributeData> attributes) => (attributes = GetAttributeData(symbol)) != ImmutableArray<AttributeData>.Empty;

    public static ImmutableArray<AttributeData> GetAttributeData(INamedTypeSymbol symbol) {
        if (_cacheSymbolAttributeDic.TryGetValue(symbol, out var attributes) == false) {
            attributes = symbol.GetAttributes();
            _cacheSymbolAttributeDic.TryAdd(symbol, attributes);
        }
        
        return attributes;
    }
    
    protected bool TryFindAttributes(INamedTypeSymbol namedTypeSymbol, string attributeName, out ImmutableArray<AttributeData> attributeData) => (attributeData = FindAttributes(namedTypeSymbol, attributeName)) != null;
    protected ImmutableArray<AttributeData> FindAttributes(INamedTypeSymbol namedTypeSymbol, string attributeName) => GetInheritedClassAndInterfaces(namedTypeSymbol)
        .SelectMany(symbol => symbol.GetAttributes())
        .Where(attribute => attribute.AttributeClass?.Name.Equals(attributeName, StringComparison.Ordinal) ?? false)
        .ToImmutableArray();

    protected IEnumerable<INamedTypeSymbol> GetInheritedClassAndInterfaces(INamedTypeSymbol symbol) {
        var type = symbol;
        while ((type = type.BaseType) != null) {
            yield return type;
            foreach (var interfaceType in type.AllInterfaces) {
                yield return interfaceType;
            }
        }
    }
}

public static class AnalyzerExtension {

    public static bool IsString(this TypedConstant constant) => constant.Kind == TypedConstantKind.Primitive && constant.Type?.MetadataName == "String";
}