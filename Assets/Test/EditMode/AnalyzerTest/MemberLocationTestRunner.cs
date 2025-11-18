using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using SystemColor = System.Drawing.Color;
using NUnit.Framework;
using Color = UnityEngine.Color;
using UnityAssembly = UnityEditor.Compilation.Assembly;
using SystemAssembly = System.Reflection.Assembly;

// 리다이렉션 기능을 구현하기 위해 너무 많은 작업 소요가 발생하며 이를 해결하기 위해선 계획을 처음부터 다시 세우고 집중적으로 처리할 필요가 있음. 현재로서는 투자한 시간 대비 진척이 되지 않음
[Category(TestConstants.Category.ANALYZER)]
[Obsolete("Not Implement")]
public class MemberLocationTestRunner {

    private List<UnityAssembly> assemblyList;
    
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        assemblyList ??= UnityAssemblyProvider.GetUnityAssemblySet().Where(assembly => assembly.IsCustom() && assembly.name != "UniRx").ToList();
        assemblyList.Shuffle();
    }

    private class TempClass {

        public void TempMethod<T, V>(in Action<T, V> a, out int b, ref Action<T, V> c, Dictionary<T, List<V>> dic) {
            b = 1;
        }

        public string ToHex(Color color, bool hasAlpha = false) => $"#{(hasAlpha ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color))}";
        public string ToHex(SystemColor color, bool hasAlpha = false) => hasAlpha ? $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}" : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    
    [Test]
    public void TempTest() {
        var type = typeof(TempClass);
        var methodInfo = type.GetMethod(nameof(TempClass.TempMethod));
        Assert.NotNull(methodInfo);
        foreach (var info in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
            Logger.TraceLog(GetFixName(info));
        }
    }

    [Test]
    public void MemberLocationAnalyzeTest() {
        var assembly = assemblyList.Find(assembly => assembly.name == "CommonAssembly");
        Assert.NotNull(assembly);
        
        AnalyzeMemberInfoLocation(assembly, CancellationToken.None);
    }

    [Test]
    public void AllAssemblyLocationAnalyzeTest() {
        foreach (var assembly in assemblyList) {
            AnalyzeMemberInfoLocation(assembly, CancellationToken.None);
        }
    }

    private void AnalyzeMemberInfoLocation(UnityAssembly assembly, CancellationToken token) {
        if (AssemblyProvider.TryGetSystemAssembly(assembly.name, out var systemAssembly) == false) {
            return;
        }

        var memberCache = new Dictionary<string, MemberInfo>();
        var typeList = systemAssembly.GetTypes().Where(type => type.IsDefined<CompilerGeneratedAttribute>() == false).ToList();
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var type in typeList) {
            var name = GetFixName(type);
            if (memberCache.TryAdd(GetFixName(type), type) == false) {
                Logger.TraceLog($"Duplicate || {name}");
            }

            foreach (var info in type.GetMembers(bindingFlags).Where(info => info.IsDefined<CompilerGeneratedAttribute>() == false)) {
                switch (info) {
                    case FieldInfo:
                    case PropertyInfo:
                    case MethodInfo:
                        name = GetFixName(info);
                        if (memberCache.TryAdd(name, info) == false) {
                            Logger.TraceLog($"Duplicate || {name} || {info.GetType().Name}");
                        }
                        break;
                }
            }
        }

        var syntaxTreeList = new List<SyntaxTree>();
        foreach (var path in assembly.sourceFiles) {
            var fullPath = Path.Combine(Constants.Path.PROJECT_PATH, path);
            syntaxTreeList.Add(CSharpSyntaxTree.ParseText(File.ReadAllTextAsync(fullPath, token).Result, CSharpParseOptions.Default.WithPreprocessorSymbols(assembly.defines), cancellationToken: token).WithFilePath(fullPath));
        }
        
        Assert.NotNull(syntaxTreeList);

        var compilation = CSharpCompilation.Create($"{assembly.name}_Temp")
            .AddReferences(AssemblyProvider.GetSystemAssemblySet().Select(x => MetadataReference.CreateFromFile(x.Location)))
            .AddSyntaxTrees(syntaxTreeList);
        
        Assert.NotNull(compilation);

        foreach (var syntaxTree in syntaxTreeList) {
            token.ThrowIfCancellationRequested();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (var syntax in syntaxTree.GetRootAsync(token).Result.DescendantNodes()) {
                switch (syntax) {
                    case TypeDeclarationSyntax typeSyntax:
                        if (semanticModel.TryGetDeclaredSymbol(typeSyntax, out var typeSymbol) == false) {
                            Logger.TraceLog(typeSyntax.GetLocation().ToString());
                            continue;
                        }
                        
                        var typeName = GetFixName(typeSymbol);
                        if (memberCache.ContainsKey(typeName) == false) {
                            Logger.TraceLog(typeName, SystemColor.Red);
                            Assert.Fail();
                        }
                        break;
                    case FieldDeclarationSyntax fieldSyntax:
                        var variable = fieldSyntax.Declaration.Variables.FirstOrDefault();
                        if (variable == null || semanticModel.TryGetDeclaredSymbol(variable, out var fieldSymbol) == false) {
                            Logger.TraceLog(fieldSyntax.GetLocation().ToString());
                            continue;
                        }
                        
                        var fieldName = GetFixName(fieldSymbol);
                        if (memberCache.ContainsKey(fieldName) == false) {
                            Logger.TraceLog(fieldName, SystemColor.Red);
                            Assert.Fail();
                        }
                        break;
                    case PropertyDeclarationSyntax propertySyntax:
                        if (semanticModel.TryGetDeclaredSymbol(propertySyntax, out var propertySymbol) == false) {
                            Logger.TraceLog(propertySyntax.GetLocation().ToString());
                            continue;
                        }

                        var propertyName = GetFixName(propertySymbol);
                        if (memberCache.ContainsKey(propertyName) == false) {
                            Logger.TraceLog(propertyName, SystemColor.Red);
                            Assert.Fail();
                        }
                        break;
                    case MethodDeclarationSyntax methodSyntax:
                        if (semanticModel.TryGetDeclaredSymbol(methodSyntax, out var methodSymbol) == false) {
                            Logger.TraceLog(methodSyntax.GetLocation().ToString());
                            continue;
                        }

                        var methodName = GetFixName(methodSymbol);
                        if (memberCache.ContainsKey(methodName) == false) {
                            Logger.TraceLog(methodName, SystemColor.Red);
                            Assert.Fail();
                        }
                        break;
                }
            }
        }
        
        Logger.TraceLog($"Pass {assembly.name} analyze test");
    }

    private string GetFixName(MemberInfo info) {
        switch (info) {
            case Type type:
                if (type.IsGenericType) {
                    using (StringUtil.StringBuilderPool.Get(out var builder)) {
                        builder.Append(type.Name);
                        builder.Append('[');
                        builder.AppendJoin(',', type.GetGenericArguments().Select(GetFixName));
                        builder.Append(']');
                        return builder.ToString();
                    }
                }

                return string.IsNullOrEmpty(type.FullName) ? type.Name : type.FullName;
            case FieldInfo:
            case PropertyInfo:
                using (StringUtil.StringBuilderPool.Get(out var builder)) {
                    if (info.DeclaringType != null) {
                        builder.Append(GetFixName(info.DeclaringType));
                        builder.Append("+");
                    }

                    builder.Append(info.Name);
                    return builder.ToString();
                }
            case MethodInfo methodInfo:
                using (StringUtil.StringBuilderPool.Get(out var builder)) {
                    if (methodInfo.DeclaringType != null) {
                        builder.Append(GetFixName(methodInfo.DeclaringType));
                        builder.Append("+");
                    }

                    builder.Append(methodInfo.Name);

                    if (methodInfo.IsGenericMethod) {
                        builder.Append("[");
                        builder.AppendJoin(',', methodInfo.GetGenericArguments().Select(GetFixName));
                        builder.Append("]");
                    }
                    
                    builder.Append("(");
                    builder.AppendJoin(',', methodInfo.GetParameters().Select(x => GetFixName(x.ParameterType)));
                    builder.Append(")");
                    
                    return builder.ToString();
                }
        }

        return info.Name;
    }

    private string GetFixName(ISymbol symbol) {
        using (StringUtil.StringBuilderConcurrentPool.Get(out var builder)) {
            if (symbol.ContainingNamespace.IsGlobalNamespace == false) {
                builder.Append($"{symbol.ContainingNamespace.ToDisplayString()}.");
            }

            switch (symbol) {
                case ITypeParameterSymbol parameterSymbol:
                    builder.Append(parameterSymbol.MetadataName);
                    if (parameterSymbol.ContainingType.IsGenericType) {
                        builder.Append('`');
                        
                    }
                    break;
                case INamedTypeSymbol typeSymbol:
                    AppendNestedType(builder, symbol);
                    // builder.Append(symbol.MetadataName);
                    if (typeSymbol.IsGenericType) {
                        builder.Append("[");
                        builder.AppendJoin(',', typeSymbol.TypeParameters.Select(p => p.Name));
                        builder.Append("]");
                    }
                    
                    break;
                case IFieldSymbol fieldSymbol:
                    // TODO. AppendNestedType 처리에서 필드 명까지 전부 포함되서 처리됨. 수정 필요 EX) CallStack`1+_list[T] --> CallStack`1[T]+_list
                    builder.Append(GetFixName(fieldSymbol.ContainingType));
                    builder.Append('+');
                    builder.Append(fieldSymbol.MetadataName);
                    if (fieldSymbol.ContainingType.IsGenericType) {
                        builder.Append('`');
                        builder.Append(fieldSymbol.ContainingType.TypeArguments.Length);
                        
                        builder.Append("[");
                        builder.AppendJoin(',', fieldSymbol.ContainingType.TypeParameters.Select(GetFixName));
                        builder.Append("]");
                    }
                    
                    break;
                case IMethodSymbol methodSymbol:
                    AppendNestedType(builder, symbol);
                    if (methodSymbol.IsGenericMethod) {
                        builder.Append("[");
                        builder.AppendJoin(',', methodSymbol.TypeParameters.Select(p => p.Name));
                        builder.Append("]");
                    }
                    
                    builder.Append("(");
                    if (methodSymbol.Parameters.Any()) {
                        builder.AppendJoin(',', methodSymbol.Parameters.Select(x => {
                            using (StringUtil.StringBuilderPool.Get(out var parameterBuilder)) {
                                if (x.ContainingType.IsGenericType) {
                                    parameterBuilder.Append('`');
                                    parameterBuilder.Append(x.ContainingType.TypeArguments.Length);
                                }

                                if (x.Type.ContainingNamespace is { IsGlobalNamespace: false }) {
                                    parameterBuilder.Append(x.Type.ContainingNamespace);
                                    parameterBuilder.Append('.');
                                }

                                parameterBuilder.Append(x.Type.Name);
                                if (x.RefKind != RefKind.None) {
                                    parameterBuilder.Append('&');
                                }

                                return parameterBuilder.ToString();
                            }
                        }));
                    }
                    builder.Append(")");
                    break;
                default:
                    AppendNestedType(builder, symbol);
                    break;
            }
            
            return builder.ToString();
        }
    }

    private void AppendNestedType(StringBuilder builder, ISymbol symbol) {
        AppendNestedTypes(builder, symbol);
        return;
        if (symbol.ContainingType != null) {
            AppendNestedType(builder, symbol.ContainingType);
            builder.Append("+");
        }

        if (symbol is INamedTypeSymbol { IsGenericType: true } typeSymbol) {
            builder.Append("[");
            builder.AppendJoin(',', typeSymbol.TypeArguments.Select(x => x.MetadataName));
            builder.Append("]");
        }
        
        if (symbol is IFieldSymbol && symbol.ContainingType is { IsGenericType: true }) {
            builder.Append(symbol.Name);
        } else {
            builder.Append(symbol.MetadataName);
        } 
    }

    private void AppendNestedTypes(StringBuilder builder, ISymbol symbol) {
        if (symbol.ContainingType != null) {
            AppendNestedTypes(builder, symbol.ContainingType);
            builder.Append("+");
        }

        builder.Append(symbol.MetadataName);
    }
}