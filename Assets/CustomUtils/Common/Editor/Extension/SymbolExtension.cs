using Microsoft.CodeAnalysis;

public static class SymbolExtension {

    public static ITypeParameterSymbol GetTypeParameterSymbol(this IParameterSymbol symbol) => symbol.Type as ITypeParameterSymbol;
    public static bool IsRef(this IParameterSymbol symbol) => symbol.RefKind != RefKind.None;
}
