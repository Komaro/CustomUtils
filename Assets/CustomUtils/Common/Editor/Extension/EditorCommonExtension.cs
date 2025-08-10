using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor;
using UnityEngine;

public static class EditorCommonExtension {

    private static readonly ImmutableHashSet<EventType> _ignoreEventTypeSet = ImmutableHashSet.Create(EventType.Ignore, EventType.Layout, EventType.Repaint, EventType.Used, EventType.ExecuteCommand, EventType.ValidateCommand);

    public static bool IsProcessableEvent(this EventType eventType) => _ignoreEventTypeSet.Contains(eventType) == false;

    public static BuildTargetGroup GetTargetGroup(this BuildTarget type) => BuildPipeline.GetBuildTargetGroup(type);
}

public static class CSharpExtension {

    public static bool TryGetDeclaredSymbol(this SemanticModel model, SyntaxNode syntax, out ISymbol symbol) => (symbol = model.GetDeclaredSymbol(syntax)) != null;

    public static (string path, int line) GetRedirectLocation(this Location location) => (location.GetFilePath(), location.GetLinePosition()); 
    public static string GetFilePath(this Location location) => location.SourceTree?.FilePath ?? string.Empty;
    public static int GetLinePosition(this Location location) => location.GetLineSpan().StartLinePosition.Line + 1;
}