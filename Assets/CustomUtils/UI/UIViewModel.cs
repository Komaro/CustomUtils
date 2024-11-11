using System;
using System.Collections.Immutable;
using System.Linq;

public abstract class UIViewModel : IDisposable {

    protected bool isDisposed;
    
    protected readonly Lazy<ImmutableArray<NotifyField>> lazyNotifyFields;

    public delegate void NotifyModelChangeHandler(string fieldName, NotifyFieldChangedEventArgs args);
    public SafeDelegate<NotifyModelChangeHandler> OnModelChanged;
    
    private static readonly ImmutableHashSet<Type> NOTIFY_TYPE_SET = ReflectionProvider.GetSubClassTypes(typeof(NotifyField)).ToImmutableHashSet();
    
    public UIViewModel() {
        var notifyDic = GetType().GetFields().Where(info => NOTIFY_TYPE_SET.Contains(info.FieldType.GetGenericTypeDefinition())).ToDictionary(info => info, info => info.GetValue(this) as NotifyField);
        foreach (var (info, notifyField) in notifyDic) {
            notifyField.OnChanged += args => OnModelChanged.handler?.Invoke(info.Name, args);
        }
        
        lazyNotifyFields = new Lazy<ImmutableArray<NotifyField>>(() => GetType().GetFields().Where(info => NOTIFY_TYPE_SET.Contains(info.FieldType.GetGenericTypeDefinition())).Select(info => info.GetValue(this) as NotifyField).ToImmutableArray());
    }

    public void Dispose() {
        if (isDisposed == false) {
            Clear();
            isDisposed = true;
        }
    }

    public void Clear() => OnModelChanged.Clear();

    public virtual void RefreshAll() {
        if (IsAlreadyOnChanged()) {
            foreach (var notifyField in lazyNotifyFields.Value) {
                notifyField.Refresh();
            }
        }
    }

    public bool IsAlreadyOnChanged() => OnModelChanged.Count > 0;
}