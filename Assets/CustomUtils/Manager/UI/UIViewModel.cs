﻿using System;
using System.Collections.Immutable;

public abstract class UIViewModel : IDisposable {

    protected bool isDisposed;
    
    private static readonly ImmutableHashSet<Type> NOTIFY_TYPE_SET = ReflectionProvider.GetSubClassTypes(typeof(NotifyField)).ToImmutableHashSet();

    public delegate void NotifyModelChangeHandler(string fieldName, NotifyFieldChangedEventArgs args);
    public SafeDelegate<NotifyModelChangeHandler> OnModelChanged;

    public UIViewModel() {
        foreach (var fieldInfo in GetType().GetFields()) {
            if (NOTIFY_TYPE_SET.Contains(fieldInfo.FieldType.GetGenericTypeDefinition()) && fieldInfo.GetValue(this) is NotifyField notifyField) {
                notifyField.OnChanged += args => OnModelChanged.handler?.Invoke(fieldInfo.Name, args);
            }
        }
    }

    public void Dispose() {
        if (isDisposed == false) {
            Clear();
            isDisposed = true;
        }
    }

    public void Clear() => OnModelChanged.Clear();
    
    public bool IsAlreadyOnChanged() => OnModelChanged.Count > 0;
}