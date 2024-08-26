using System;
using System.Collections.Concurrent;
using System.Linq;

public class GameDBService : IService {

    private GameDBProvider _provider;
    private ConcurrentDictionary<Type, object> _dbDic = new();

    void IService.Init() {
        var providerType = ReflectionProvider.GetSubClassTypes<GameDBProvider>().OrderBy(x => x.TryGetCustomAttribute<PriorityAttribute>(out var attribute) ? attribute.priority : 999).FirstOrDefault();
        if (providerType == null || SystemUtil.TryCreateInstance(out _provider, providerType) == false) {
            Logger.TraceError($"Failed to create and initialize {nameof(GameDBProvider)}. Creating {nameof(NullGameDBProvider)} instead.");
            _provider = new NullGameDBProvider();
        }
        
        var dbTypeSet = ReflectionProvider.GetSubClassTypeDefinitions(typeof(GameDB<,>)).ToHashSet();
        _provider.Init(dbTypeSet);
        
        foreach (var type in dbTypeSet) {
            if (SystemUtil.TryCreateInstance(out var db, type, _provider)) {
                _dbDic.AutoAdd(type, db);
            }
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