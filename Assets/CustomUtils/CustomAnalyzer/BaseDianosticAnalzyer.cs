using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class BaseDianosticAnalzyer : DiagnosticAnalyzer {
    
    private static ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<AttributeData>> _cacheSymbolAttributeDic = new();

    protected bool TryGetAttributeData(INamedTypeSymbol symbol, out ImmutableArray<AttributeData> attributes) => (attributes = GetAttributeData(symbol)) != ImmutableArray<AttributeData>.Empty;

    protected ImmutableArray<AttributeData> GetAttributeData(INamedTypeSymbol symbol) {
        if (_cacheSymbolAttributeDic.TryGetValue(symbol, out var attributes) == false) {
            attributes = symbol.GetAttributes();
            _cacheSymbolAttributeDic.TryAdd(symbol, attributes);
        }
        
        return attributes;
    }

    protected bool TryFindAttribute(INamedTypeSymbol namedTypeSymbol, string attributeName, out AttributeData attributeData) => (attributeData = FindAttribute(namedTypeSymbol, attributeName)) != null;
    protected AttributeData FindAttribute(INamedTypeSymbol namedTypeSymbol, string attributeName) => FindAttributes(namedTypeSymbol, attributeName).FirstOrDefault();
    
    protected bool TryFindAttributes(INamedTypeSymbol namedTypeSymbol, string attributeName, out ImmutableArray<AttributeData> attributeData) => (attributeData = FindAttributes(namedTypeSymbol, attributeName)) != null || attributeData.Length <= 0;
    protected ImmutableArray<AttributeData> FindAttributes(INamedTypeSymbol namedTypeSymbol, string attributeName) => GetAllInheritedClassAndInterfaces(namedTypeSymbol)
        .SelectMany(symbol => symbol.GetAttributes())
        .Where(attribute => attribute.AttributeClass?.Name.Equals(attributeName, StringComparison.Ordinal) ?? false)
        .ToImmutableArray();
    
    protected IEnumerable<INamedTypeSymbol> GetAllInheritedClassAndInterfaces(INamedTypeSymbol symbol) {
        yield return symbol;
        foreach (var interfaceType in symbol.AllInterfaces) {
            yield return interfaceType;
        }

        while ((symbol = symbol.BaseType) != null) {
            yield return symbol;
        }
    }
    
    protected bool TryGetSyntaxReferenceLocation(AttributeData data, out Location location) => (location = GetSyntaxReferenceLocation(data)) != null;
    protected Location GetSyntaxReferenceLocation(AttributeData data) => data.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    
    public bool IsString(ref TypedConstant constant) => constant.Kind == TypedConstantKind.Primitive && constant.Type?.MetadataName == "String";
}