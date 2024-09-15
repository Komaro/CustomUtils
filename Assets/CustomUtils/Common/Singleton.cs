using System;

public class Singleton<T> where T : class, new() {
    
    private static T _instance;
    public static T instance => _instance ??= new T();
    public static T inst => instance;
}

[RequiresAttributeImplementation(typeof(SingletonParamAttribute))]
public abstract class SingletonWithParameter<T> where T : class {
    
    private static T _instance;

    public static T instance {
        get {
            if (_instance == null) {
                var type = typeof(T);
                if (type.TryGetCustomAttribute<SingletonParamAttribute>(out var attribute)) {
                    _instance = Activator.CreateInstance(typeof(T), attribute.args) as T;
                }
            }

            return _instance;
        }
    }

    public static T inst => instance;
}

[RequiresAttributeImplementation(typeof(SingletonParamAttribute))]
public abstract class SingletonWithParameter<TBase, TInstance> where TInstance : class {

    private static TInstance _instance;

    public static TInstance instance {
        get {
            if (_instance == null) {
                var type = typeof(TBase);
                if (type.TryGetCustomAttribute<SingletonParamAttribute>(out var attribute)) {
                    _instance = Activator.CreateInstance(typeof(TInstance), attribute.args) as TInstance;
                }
            }

            return _instance;
        }
    }

    public static TInstance inst => instance;
}

public class SingletonParamAttribute : Attribute {

    public readonly object[] args;
    
    public SingletonParamAttribute(params object[] args) => this.args = args ?? Array.Empty<object>();
}