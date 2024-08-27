using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;

public class GameDBService : IService {

    private GameDBProvider _provider;
    private readonly ConcurrentDictionary<Type, object> _dbDic = new();
    
    void IService.Init() {
        var dbTypeSet = ReflectionProvider.GetSubClassTypeDefinitions(typeof(GameDB<,>)).ToHashSet();
        foreach (var providerType in ReflectionProvider.GetSubClassTypes<GameDBProvider>().OrderBy(x => x.TryGetCustomAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 99999)) {
            if (providerType == null || SystemUtil.TryCreateInstance(out _provider, providerType) == false) {
                Logger.TraceError($"Failed to create and initialize {nameof(GameDBProvider)} with {providerType?.Name}. Creating {nameof(NullGameDBProvider)} instead.");
                _provider = new NullGameDBProvider();
            }

            try {
                if (_provider.Init(dbTypeSet)) {
                    foreach (var type in dbTypeSet) {
                        if (SystemUtil.TryCreateInstance(out var db, type, _provider)) {
                            _dbDic.AutoAdd(type, db);
                        }
                    }
                    
                    Logger.TraceLog($"{nameof(GameDBService)} initialized successfully. The target provider is {providerType?.Name}");
                    break;
                }
            } catch (Exception ex) {
                Logger.TraceLog(ex, Color.Red);
            }
        }

        if (_provider == null) {
            Logger.TraceError($"Failed to initialize {nameof(GameDBService)}");
        }
    }

    void IService.Start() { }
    void IService.Stop() { }

    void IService.Remove() {
        _provider?.Clear();
        _dbDic.Clear();
    }

    public bool TryGet<T>(out T db) where T : class => (db = Get<T>()) != null;

    public T Get<T>() where T : class {
        if (_dbDic.TryGetValue(typeof(T), out var db)) {
            return db as T;
        }

        return null;
    }
}