using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UniRx;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
 
public abstract class JsonConfig : IDisposable {

    [JsonIgnore] protected DateTime lastSaveTime;
    [JsonIgnore] public ref DateTime LastSaveTime => ref lastSaveTime;
    
    [JsonIgnore] protected bool isDisposed;

    public JsonConfig() => lastSaveTime = DateTime.Now;

    ~JsonConfig() {
        if (isDisposed == false) {
            Dispose();
        }
    }

    public abstract void Dispose();
    
    public virtual void Save(string path) {
        lastSaveTime = DateTime.Now;
        JsonUtil.SaveJson(path, this);
    }

    public bool TryClone<T>(out T config) where T : JsonConfig, new() {
        config = Clone<T>();
        return config != null;
    }

    public virtual T Clone<T>() where T : JsonConfig, new() {
        if (Clone(typeof(T)) is T cloneConfig) {
            return cloneConfig;
        }

        return null;
    }

    public bool TryClone(Type type, out object config) {
        config = Clone(type);
        return config != null;
    }

    public virtual object Clone(Type type) {
        if (type == null) {
            Logger.TraceError($"{nameof(type)} is Null");
            return null;
        }

        if (type.IsAbstract == false) {
            try {
                var cloneConfig = Activator.CreateInstance(type);
                if (cloneConfig is JsonAutoConfig == false) {
                    return null;
                }

                foreach (var info in type.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
                    info.SetValue(cloneConfig, info.GetValue(this));
                }

                return cloneConfig;
            } catch (Exception ex) {
                Logger.TraceError(ex);
                throw;
            }
        }

        return null;
    }

    public abstract bool IsNull();
}

public abstract class JsonCoroutineAutoConfig : JsonConfig {

    [JsonIgnore]
    protected string savePath;
    
    [JsonIgnore] 
    protected bool saveFlag;

    [JsonIgnore]
    private EditorCoroutine _coroutine;

    // TODO. Constants화 필요
    private static readonly ImmutableHashSet<Type> NOTIFY_TYPE_SET = ReflectionProvider.GetSubTypesOfType(typeof(NotifyField)).ToImmutableHashSet();

    public override void Dispose() {
        if (IsNull() == false) {
            StopAutoSave();
        }

        isDisposed = true;
    }
    
    public override T Clone<T>() {
        StopAutoSave();
        return base.Clone<T>();
    }

    public override object Clone(Type type) {
        StopAutoSave();
        
        if (type == null) {
            Logger.TraceError($"{nameof(type)} is Null");
            return null;
        }

        if (type.IsAbstract == false) {
            try {
                var cloneConfig = Activator.CreateInstance(type);
                if (cloneConfig is not JsonCoroutineAutoConfig) {
                    return null;
                }

                // TODO. 참조형에 대한 처리를 추가할 필요가 있음
                foreach (var info in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                    info.SetValue(cloneConfig, info.GetValue(this));
                }

                return cloneConfig;
            } catch (Exception ex) {
                Logger.TraceError(ex);
                throw;
            }
        }

        return null;
    }
    
    public virtual void StartAutoSave(string savePath) {
        if (_coroutine != null) {
            EditorCoroutineUtility.StopCoroutine(_coroutine);
            _coroutine = null;
        }

        this.savePath = savePath;
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            return;
        }
        
        _coroutine = EditorCoroutineUtility.StartCoroutine(SaveCoroutine(), this);
    }
    
    
    private IEnumerator SaveCoroutine() {
        var observerList = new List<FieldObserver>();
        foreach (var info in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(info => info.IsDefined<JsonIgnoreAttribute>() == false)) {
            var func = DynamicMethodProvider.GetFieldValueFunc(GetType(), info);
            observerList.Add(info.FieldType.GetBaseTypes().Contains(typeof(IEnumerable))
                ? new FieldObserver(info.GetValue(this), () => func.Invoke(this), new EnumerableEqualityComparer())
                : new FieldObserver(info.GetValue(this), () => func.Invoke(this)));
        }
        
        while (true) {
            yield return new EditorWaitForSeconds(5f);
            foreach (var fieldObserver in observerList) {
                if (fieldObserver.Observe()) {
                    saveFlag = true;
                    break;
                }
                
                yield return null;
            }
            
            if (saveFlag) {
                saveFlag = false;
                Save(savePath);
            }
        }
    }

    public virtual void StopAutoSave() {
        if (_coroutine != null) {
            EditorCoroutineUtility.StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    protected virtual void OnPlayModeStateChanged(PlayModeStateChange state) {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode) {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StartAutoSave(savePath);
        }
    }

    private class FieldObserver {

        private object _value;
        private readonly Func<object> _getter;
        private readonly IEqualityComparer _comparer;

        public FieldObserver(object value, Func<object> getter) {
            _value = value;
            _getter = getter;
        }
        
        public FieldObserver(object value, Func<object> getter, IEqualityComparer comparer) : this(value, getter) => _comparer = comparer;

        public bool Observe() {
            var value = _getter.Invoke();
            if (_comparer != null) {
                if (_comparer.Equals(_value, value) == false) {
                    _value = value;
                    return true;
                }
            } else {
                if (Equals(_value, value) == false) {
                    _value = value;
                    return true;
                }
            }

            return false;
        }
    }
    
    
    #region [Comparer]
 
    private class EnumerableEqualityComparer : IEqualityComparer {

        bool IEqualityComparer.Equals(object x, object y) {
            if (x is not IEnumerable xEnumerable || y is not IEnumerable yEnumerable) {
                return false;
            }

            var xEnumerator = xEnumerable.GetEnumerator();
            xEnumerator.Reset();

            var yEnumerator = yEnumerable.GetEnumerator();
            yEnumerator.Reset();

            while (xEnumerator.MoveNext()) {
                if (yEnumerator.MoveNext() == false || xEnumerator.Current?.Equals(yEnumerator.Current) == false) {
                    return false;
                }
            }

            if (yEnumerator.MoveNext()) {
                return false;
            }

            return true;
        }

        public int GetHashCode(object obj) => obj.GetHashCode();
    }
    #endregion
}


// static 필드에서 new()로 생성하지 말 것.
// TODO. UniRx 제거하기 위해선 주기적으로 saveFlag의 값을 확인하는 외부 감시자가 또 필요함. 일반적으로 이런 작업은 Observable이 처리하나 UniRx를 제거하기 위해선 별개의 기능 구현이 필요. 그러나 UniRx 없이 구현은 현실적으로 어려움. 내부 구현만으로는 필드를 관찰하는 구현이 비대할 것으로 보임
public abstract class JsonAutoConfig : JsonConfig {
    
    [JsonIgnore] protected string savePath;
    [JsonIgnore] protected readonly List<IDisposable> disposableList = new();
    [JsonIgnore] protected IDisposable intervalDisposable;
    [JsonIgnore] protected bool saveFlag;

    [JsonIgnore] private readonly ArrayComparer ARRAY_COMPARER = new();
    [JsonIgnore] private readonly CollectionComparer COLLECTION_COMPARER = new();
    
    public override T Clone<T>() {
        Dispose();
        return base.Clone<T>();
    }

    public override object Clone(Type type) {
        Dispose();
        return base.Clone(type);
    }
    
    public virtual void StartAutoSave(string savePath) {
        if (IsAutoSaving()) {
            StopAutoSave();
        }

        this.savePath = savePath;
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            return;
        }

        var intervalObservable = Observable.Interval(TimeSpan.FromSeconds(5f)).Publish();
        intervalDisposable = intervalObservable.Connect();
        
        RegisterFieldWatchers(intervalObservable);
        RegisterSaveTrigger();
    }

    protected virtual void RegisterFieldWatchers(IObservable<long> intervalObservable) {
        foreach (var info in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(info => info.IsDefined<JsonIgnoreAttribute>() == false)) {
            var func = DynamicMethodProvider.GetFieldValueFunc(GetType(), info);
            if (info.FieldType.IsArray) {
                disposableList.Add(intervalObservable.Select(_ => func.Invoke(this) as Array)
                    .DistinctUntilChanged(ARRAY_COMPARER)
                    .Subscribe(_ => saveFlag = true));
            } else if (info.FieldType.IsGenericCollectionType()) {
                disposableList.Add(intervalObservable.Select(_ => func.Invoke(this) is ICollection collection ? collection.CloneEnumerator() : null)
                    .DistinctUntilChanged(COLLECTION_COMPARER)
                    .Subscribe(_ => saveFlag = true));
            } else {
                disposableList.Add(intervalObservable.DistinctUntilChanged(_ => func.Invoke(this)).Subscribe(_ => saveFlag = true));
            }
        }
    }

    protected virtual void RegisterSaveTrigger() {
        disposableList.Add(Observable.Interval(TimeSpan.FromSeconds(5f)).Subscribe(_ => {
            if (saveFlag) {
                saveFlag = false;
                Save(savePath);
            }
        }));
    }

    public virtual void StopAutoSave() {
        disposableList?.SafeClear(x => x.Dispose());
        intervalDisposable?.Dispose();
    }

    ~JsonAutoConfig() => Dispose();
    
    public override void Dispose() {
        if (IsNull() == false) {
            StopAutoSave();
        }
    }

    public virtual bool IsAutoSaving() => disposableList.Any();
    
    protected virtual void OnPlayModeStateChanged(PlayModeStateChange state) {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode) {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StartAutoSave(savePath);
        }
    }

    #region [Comparer]

    private class ArrayComparer : IEqualityComparer<Array> {

        public bool Equals(Array x, Array y) {
            if (x == null || y == null) {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(Array array) => array.GetHashCode();
    }
    
    private class CollectionComparer : IEqualityComparer<IEnumerator> {

        public bool Equals(IEnumerator x, IEnumerator y) {
            if (x == null || y == null) {
                return false;
            }
            
            x.Reset();
            y.Reset();

            // Check count zero
            if (x.MoveNext() == false || y.MoveNext() == false) {
                return false;
            }

            do {
                if (x.Current?.Equals(y.Current) == false) {
                    return false;
                }
            } while (x.MoveNext() && y.MoveNext());

            return true;
        }

        public int GetHashCode(IEnumerator enumerator) {
            unchecked {
                var hash = 17;
                while (enumerator.MoveNext()) {
                    hash = hash * 31 + enumerator.Current?.GetHashCode() ?? 0;
                }
                
                return hash;
            }
        }
    }
    
    #endregion
}