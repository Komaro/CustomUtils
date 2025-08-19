using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor;
using UnityEngine;
using Color = System.Drawing.Color;
using UnityAssembly = UnityEditor.Compilation.Assembly;

public class CodeLocationService : IOperationService {

    public bool IsActiveAnalyze { get; private set; }
    public bool IsActiveFullProcessor { get; private set; }
    public bool IsAnalyzing { get; private set; }

    private int _taskId;

    private CSharpCompilation _compilation;
    private readonly Dictionary<MemberInfo, ISymbol> _symbolDic = new();

    private readonly CancellationTokenSource _tokenSource = new();
    
    private const string ACTIVE_ANALYZE = "ActiveAnalzye";
    private const string ACTIVE_FULL_PROCESSOR = "ActiveFullProcessor";

    async Task IOperationService.InitAsync(ServiceOperation operation) {
        IsActiveAnalyze = EditorPrefsUtil.GetBool(ACTIVE_ANALYZE);
        IsActiveFullProcessor = EditorPrefsUtil.GetBool(ACTIVE_FULL_PROCESSOR);
        
        await Task.Yield();
        lock (_compilation) {
            _compilation = CSharpCompilation.Create($"{nameof(CodeLocationService)}Assembly").AddReferences(AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)));
        }
        
        operation.Done();
    }

    async Task IOperationService.StartAsync(ServiceOperation operation) {
        await AssemblyAnalyze(operation);
        operation.Done();
    }

    async Task IOperationService.StopAsync(ServiceOperation operation) {
        _compilation.RemoveAllSyntaxTrees();
        operation.Done();
        await Task.CompletedTask;
    }

    async Task IOperationService.RefreshAsync(ServiceOperation operation) {
        _compilation.RemoveAllSyntaxTrees();
        await AssemblyAnalyze(operation);
        operation.Done();
    }

    // async Task IAsyncService.InitAsync() {
    //     IsActiveAnalyze = EditorPrefsUtil.GetBool(ACTIVE_ANALYZE);
    //     IsActiveFullProcessor = EditorPrefsUtil.GetBool(ACTIVE_FULL_PROCESSOR);
    //     lock (_compilation) {
    //         _compilation = CSharpCompilation.Create($"{nameof(CodeLocationService)}Assembly").AddReferences(AssemblyProvider.GetSystemAssemblySet().Select(assembly => MetadataReference.CreateFromFile(assembly.Location)));
    //     }
    //     
    //     await Task.CompletedTask;
    // }

    // async Task IAsyncService.StartAsync() => await AssemblyAnalyze();

    // Task IAsyncService.StopAsync() {
    //     _compilation.RemoveAllSyntaxTrees();
    //     return Task.CompletedTask;
    // }

    // async Task IAsyncService.RefreshAsync() {
    //     _compilation.RemoveAllSyntaxTrees();
    //     await AssemblyAnalyze();
    // }
    
    private async Task AssemblyAnalyze(ServiceOperation operation) {
        if (IsActiveAnalyze == false) {
            return;
        }

        if (IsAnalyzing) {
            Logger.TraceLog("Already analyzing code location", Color.Yellow);
            if (Progress.Cancel(_taskId) == false) {
                await Task.FromException(new InvalidOperationException($"{_taskId} {nameof(Progress)} cancellation failed"));
            }
            
            _tokenSource.Cancel();    
        }

        try {
            _taskId = Progress.Start("Code Location Analyzing");
            operation.Init();
            IsAnalyzing = true;
            
            await Task.Run(() => {
                var processorCount = IsActiveFullProcessor ? Environment.ProcessorCount : Environment.ProcessorCount / 2;
                AssemblyProvider.GetSystemAssemblySet().Where(assembly => assembly.IsCustom()).SelectNotNull(UnityAssemblyProvider.GetUnityAssembly)
                    .AsParallel().WithDegreeOfParallelism(processorCount).WithCancellation(_tokenSource.Token)
                    .ForAll(assembly => {
                        var id = Progress.Start($"{assembly.name} Analyzing", parentId: _taskId);
                        var syntaxTreeList = ParseAssemblySourceFiles(assembly, _tokenSource.Token);
                        lock (_compilation) {
                            _compilation.AddSyntaxTrees(syntaxTreeList);
                        }

                        Progress.Finish(id);
                    });
            });
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            Progress.Finish(_taskId);
            operation.Done();
            IsAnalyzing = false;
        }
    }

    private List<SyntaxTree> ParseAssemblySourceFiles(UnityAssembly assembly, CancellationToken token) {
        var id = Progress.Start($"{assembly.name} Analyzing", parentId: _taskId);
        var syntaxTreeList = assembly.sourceFiles.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllTextAsync(path, token).Result).WithFilePath(Path.Combine(Constants.Path.PROJECT_PATH, path))).ToList();
        Progress.Finish(id);
        return syntaxTreeList;
    }
    
    public LocationResolver GetResolver(MemberInfo info) {
        if (_symbolDic.TryGetValue(info, out var symbol) == false) {
            switch (info) {
                case Type type:
                    return GetResolver(type);
                case FieldInfo fieldInfo:
                    return GetResolver(fieldInfo);
                case PropertyInfo propertyInfo:
                    return GetResolver(propertyInfo);
                case MethodInfo methodInfo:
                    return GetResolver(methodInfo);
                default:
                    return default;
            }
        }

        return new LocationResolver(info, symbol);
    }
    
    [RefactoringRequired("최적화 필요")]
    public LocationResolver GetResolver(Type type) {
        if (_symbolDic.TryGetValue(type, out var symbol) == false) {
            if (type.IsGenericTypeDefinition) {
                symbol = _compilation.GetTypeByMetadataName(type.GetGenericTypeDefinition().FullName ?? string.Empty);
            } else if (type.IsGenericType) {
                symbol = _compilation.GetTypeByMetadataName(type.GetGenericTypeDefinition().MakeGenericType(type.GenericTypeArguments).FullName ?? string.Empty);
            } else {
                symbol = _compilation.GetTypeByMetadataName(type.FullName ?? string.Empty);
            }

            if (symbol == null) {
                Logger.TraceLog($"{type.FullName} is missing full name", Color.Red);
                throw new NullReferenceException<ISymbol>(nameof(symbol));
            }
            
            _symbolDic.Add(type, symbol);
        }
        
        return new LocationResolver(type, symbol);
    }

    [RefactoringRequired("구현 필요")]
    public LocationResolver GetResolver(FieldInfo info) {
        return default;
    }

    [RefactoringRequired("구현 필요")]
    public LocationResolver GetResolver(PropertyInfo info) {
        return default;
    }
    
    [RefactoringRequired("최적화도 필요")]
    public LocationResolver GetResolver(MethodInfo info) {
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
                        _symbolDic.AutoAdd(info, methodSymbol);
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
}

public readonly ref struct LocationResolver {

    public MemberInfo Info { get; }
    public ISymbol Symbol { get; }

    public LocationResolver(MemberInfo info, ISymbol symbol) {
        Info = info;
        Symbol = symbol;
    }

    public void Deconstruct(out string path, out int line) {
        var location = GetLocation();
        path = location.path;
        line = location.line;
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