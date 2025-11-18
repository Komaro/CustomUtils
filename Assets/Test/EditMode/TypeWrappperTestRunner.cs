using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Test.TypeWrapper.Sample;
using UnityEngine;
using UnityEngine.TestTools;
using Color = System.Drawing.Color;

[Category(TestConstants.Category.REFLECTION)]
[Category(TestConstants.Category.ANALYZER)]
public class TypeWrapperTestRunner {

    private static readonly Dictionary<MemberInfo, ISymbol> _symbolDic = new();
    private static readonly Dictionary<ISymbol, MemberInfo> _memberInfoDic = new();

    private static Dictionary<Assembly, CSharpCompilation> _compilationDic = new();

    [OneTimeSetUp]
    public void SetUP() {
        LogAssert.ignoreFailingMessages = true;
    }

    [OneTimeTearDown]
    public void TearDown() {
        LogAssert.ignoreFailingMessages = false;
    }

    public static LocationResolver GetResolver(ISymbol symbol) {
        if (_memberInfoDic.TryGetValue(symbol, out var type)) {
            var resolver = GetResolver(type);
            return resolver;
        }
        
        return default;
    }
    
    public static LocationResolver GetResolver(MemberInfo info) {
        if (_symbolDic.TryGetValue(info, out var symbol) == false) {
            switch (info) {
                case Type type:
                    return GetResolver(type);
                case MethodInfo methodInfo:
                    return GetResolver(methodInfo);
            }
        }

        return default;
    }

    public static LocationResolver GetResolver(Type type) {
        if (_symbolDic.TryGetValue(type, out var symbol) == false) {
            if (_compilationDic.TryGetValue(type.Assembly, out var compilation)) {
                if (type.IsGenericTypeDefinition) {
                    symbol = compilation.GetTypeByMetadataName(type.GetGenericTypeDefinition().FullName ?? string.Empty);
                } else if (type.IsGenericType) {
                    symbol = compilation.GetTypeByMetadataName(type.GetGenericTypeDefinition().MakeGenericType(type.GenericTypeArguments).FullName ?? string.Empty);
                } else {
                    symbol = compilation.GetTypeByMetadataName(type.FullName ?? string.Empty);
                }

                if (symbol == null) {
                    Logger.TraceLog($"{type.FullName} is missing full name", Color.Red);
                    throw new NullReferenceException<ISymbol>(nameof(symbol));
                }
                
                _symbolDic.Add(type, symbol);
                _memberInfoDic.Add(symbol, type);
            } else {
                throw new KeyNotFoundException($"{type.Assembly.GetName().Name} is invalid key");
            }
        }
        
        return new LocationResolver(type, symbol);
    }

    public static LocationResolver GetResolver(ParameterInfo info) => GetResolver(info.ParameterType);

    public static LocationResolver GetResolver(MethodInfo info) {
        if (_symbolDic.TryGetValue(info, out var symbol) == false) {
            if (info.ReflectedType != null) {
                if (GetResolver(info.ReflectedType).Symbol is not ITypeSymbol typeSymbol) {
                    throw new MissingReferenceException();
                }
                
                var parameters = info.GetParameters();
                var genericArguments = info.GetGenericArguments();
                foreach (var member in typeSymbol.GetMembers()) {
                    if (member is not IMethodSymbol methodSymbol || methodSymbol.MethodKind == MethodKind.Constructor) {
                        continue;
                    }

                    if (methodSymbol.Name != info.Name) {
                        continue;
                    }

                    if (methodSymbol.Parameters.Length != parameters.Length) {
                        continue;
                    }

                    if (genericArguments.Length != methodSymbol.TypeArguments.Length) {
                        continue;
                    }
                    
                    var isValid = true;
                    for (var index = 0; index < parameters.Length; index++) {
                        var parameterType = parameters[index].ParameterType;
                        var parameterSymbol = methodSymbol.Parameters[index];
                        if (parameterType.IsGenericParameter) {
                            if (parameterSymbol.Type.TypeKind != TypeKind.TypeParameter) {
                                isValid = false;
                                break;
                            }

                            if (parameterSymbol.GetTypeParameterSymbol().Ordinal != parameterType.GenericParameterPosition) {
                                isValid = false;
                                break;
                            }

                            continue;
                        }
                        
                        if (parameterType.IsByRef) {
                            if (parameterSymbol.IsRef() == false) {
                                isValid = false;
                                break;
                            }

                            parameterType = parameterType.GetElementType();
                        }

                        if (GetResolver(parameterType).Symbol is not ITypeSymbol parameterTypeSymbol) {
                            isValid = false;
                            break;
                        }
                        
                        if (parameterSymbol.Type.Equals(parameterTypeSymbol, SymbolEqualityComparer.Default) == false) {
                            isValid = false;
                            break;
                        }
                    }
                    
                    if (isValid) {
                        // Logger.TraceLog($"Catch || {info.GetCleanFullName()}");
                        _symbolDic.AutoAdd(info, methodSymbol);
                        _memberInfoDic.AutoAdd(methodSymbol, info);
                        break;
                    }
                }
            }
        }

        if (_symbolDic.TryGetValue(info, out symbol)) {
            return new LocationResolver(info, symbol); 
        }
        
        throw new KeyNotFoundException($"{info.GetCleanFullName()} is invalid key");
    }

    private static BindingFlags GetBindingFlags(ISymbol symbol) {
        var flags = symbol.DeclaredAccessibility switch {
            Accessibility.Public => BindingFlags.Public,
            _ => BindingFlags.NonPublic
        };

        if (symbol.IsStatic) {
            flags |= BindingFlags.Static;
        } else {
            flags |= BindingFlags.Instance;
        }

        return flags;
    }

    private static IEnumerable<ITypeSymbol> GetBaseTypes(ISymbol symbol) {
        if (symbol is ITypeSymbol typeSymbol) {
            do {
                yield return typeSymbol;
            } while ((typeSymbol = typeSymbol.BaseType) != null);
        }
    }

    [Test]
    public void TypeWrappingTest() {
        var assemblyList = AssemblyProvider.GetSystemAssemblySet().Where(assembly => assembly.IsDynamic == false && assembly.IsCustom() && assembly.GetName().Name == "TestAssembly.EditMode").ToImmutableDictionary(assembly => assembly, assembly => assembly.ExportedTypes.ToImmutableList());
        Assert.IsNotEmpty(assemblyList);
        Logger.TraceLog(assemblyList.ToStringCollection(pair => pair.Key.GetName().Name, ", "));
        
        foreach (var (systemAssembly, typeList) in assemblyList) {
            // var filterList = typeList.Where(type => type.IsSubclassOf(typeof(DiagnosticAnalyzer)) && type.IsDefined<ObsoleteAttribute>() == false).ToImmutableList();
            if (/*filterList.Any() && */UnityAssemblyProvider.TryGetUnityAssembly(systemAssembly, out var unityAssembly)) {
                var compilation = CSharpCompilation.Create($"TempAssembly_{unityAssembly.name}")
                    .AddSyntaxTrees(unityAssembly.sourceFiles.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).WithFilePath(Path.Combine(Constants.Path.PROJECT_PATH, path))))
                    .AddReferences(AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)));
                
                _compilationDic.AutoAdd(systemAssembly, compilation);
                foreach (var name in compilation.ReferencedAssemblyNames.Select(assemblyIdentity => assemblyIdentity.Name)) {
                    if (AssemblyProvider.TryGetSystemAssembly(name, out var assembly)) {
                        _compilationDic.AutoAdd(assembly, compilation);
                    }
                }
            }
        }
        
        Logger.Log(string.Empty);
        
        Logger.TraceLog($"\n{_compilationDic.Keys.ToStringCollection(assembly => assembly.GetName().Name, "\n")}");
        
        Logger.Log(string.Empty);
        
        // Test Get Type Wrapper
        Logger.TraceLog($"{nameof(TypeResolver)} Test\n");

        var sampleTypeList = new List<Type> {
            typeof(Sample_01),
            // typeof(Sample_01.Sample_02),
            // typeof(Sample_02<>),
            // typeof(Sample_02<>.Sample_03),
            // typeof(Sample_03<,>),
            // typeof(Sample_03<,>.Sample_04),
        };

        foreach (var type in sampleTypeList) {
            var typeWrapper = GetResolver(type);
            Assert.True(typeWrapper.IsValid());
            
            Logger.TraceLog(typeWrapper.GetLocation());
            
            foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                var methodResolver = GetResolver(methodInfo);
                Assert.True(methodResolver.IsValid());
                
                Logger.TraceLog(methodResolver.GetLocation());
            }
        }
    }
}

namespace Test.TypeWrapper.Sample {

    public class Sample_01 {

        public void Sample_Method_01() {
            
        }
        
        public void Sample_Method_01<T>(T a) {
            
        }
        
        public void Sample_Method_01<T, V>(T a, V b) {
            
        }
        
        public void Sample_Method_01(int a) {
            
        }
        
        public void Sample_Method_01(in int a) {
            
        }
        
        public void Sample_Method_01(int a, float b) {
            
        }
        
        public void Sample_Method_01(in int a, ref float b) {
            
        }
        
        public class Sample_02 {
            
        }
    }

    public class Sample_02<T> {

        public class Sample_03 {
            
        }
    }

    public class Sample_03<T, V> {

        public class Sample_04 : Sample_03<int, float> {
            
        }
    }
}

public interface IValidator {

    public bool IsValid();
}

public readonly ref struct LocationResolver {
    
    public MemberInfo Info { get; }
    public ISymbol Symbol { get; }

    public LocationResolver(MemberInfo info, ISymbol symbol) {
        Info = info;
        Symbol = symbol;
    }

    public (string path, int line) GetLocation() {
        if (IsValid()) {
            var location = Symbol.Locations.First();
            return (location.GetFilePath(), location.GetLinePosition());
        }

        return default;
    }
    
    public bool IsValid() => Info != null && Symbol != null;
}

public readonly ref struct ParameterResolver {
    
}

[Temp]
public readonly ref struct TypeResolver {

    public Type Type { get; }
    public ISymbol Symbol { get; }

    public TypeResolver(Type type, ISymbol symbol) {
        Type = type;
        Symbol = symbol;
    }
    
    public (string path, int line) GetLocation() {
        if (Symbol != null) {
            var location = Symbol.Locations.First();
            return (location.GetFilePath(), location.GetLinePosition());
        }
        
        return default;
    }

    public bool IsValid() => Type != null && Symbol != null;
}

[Temp]
public ref struct MethodResolver {

    public MemberInfo Info { get; private set; }
    public ISymbol Symbol { get; private set; }

    public MethodResolver(MemberInfo info, ISymbol symbol) {
        Info = info;
        Symbol = symbol;
    }

    public (string path, int line) GetLocation() {
        if (Symbol != null) {
            var location = Symbol.Locations.First();
            return (location.GetFilePath(), location.GetLinePosition());
        }

        return default;
    }

    public bool IsValid() => Info != null && Symbol != null;
}