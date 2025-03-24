
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

[RequiresAttributeImplementation(typeof(PriorityAttribute))]
public abstract class LocalizeServiceProvider {

    public abstract bool IsReady();
    public abstract Dictionary<string, string> Get(Enum languageType);
}

public class LocalizeService : IService {

    private NotifyProperty<LocalizeServiceProvider> _provider = new();
    private NotifyProperty<Enum> _languageType = new();
    
    private ImmutableHashSet<Enum> _languageSet;
    private ImmutableDictionary<string, string> _cacheDic;

    void IService.Init() {
        // _languageSet = ReflectionProvider.GetAttributeEnumTypes<LanguageEnumAttribute>().SelectMany(EnumUtil.GetValues).ToImmutableHashSet();
        // _languageSet = ReflectionProvider.GetAttributeEnumTypes<LanguageEnumAttribute>().SelectMany(type => EnumUtil.GetValues(type)).ToImmutableHashSet();
        _languageSet = ReflectionProvider.GetAttributeEnumTypes<LanguageEnumAttribute>().SelectMany(type => EnumUtil.GetValues(type).ToArray()).ToImmutableHashSet();
        foreach (var type in ReflectionProvider.GetSubTypesOfType<LocalizeServiceProvider>().OrderBy(type => type.GetOrderByPriority())) {
            if (SystemUtil.TryCreateInstance<LocalizeServiceProvider>(out var provider, type) && provider.IsReady()) {
                _provider.Value = provider;
            }
        }
        
        _provider.OnChanged += _ => Load();
        _languageType.OnChanged += _ => Load();
    }

    void IService.Start() {
        if (_provider.Value != null && _languageType.Value != null) {
            Load();
        }
    }

    void IService.Stop() {
        _cacheDic = _cacheDic.Clear();
    }

    void IService.Refresh() {
        if (_provider.Value != null && _languageType.Value != null) {
            Load();
        }
    }

    void IService.Remove() {
        _provider.Dispose();
        _languageType.Dispose();
        
        _languageSet = _languageSet.Clear();
    }

#if UNITY_6000_0_OR_NEWER
    
    public async Awaitable ChangeProviderAsync(LocalizeServiceProvider provider) {
        await Awaitable.MainThreadAsync();
        ChangeProvider(provider);
    }

    public async Awaitable ChangeLanguageTypeAsync(Enum languageType) {
        await Awaitable.MainThreadAsync();
        ChangeLanguageType(languageType);
    }
    
#endif
    
    public void ChangeProvider(LocalizeServiceProvider provider) => _provider.Value = provider;

    public void ChangeLanguageType(Enum languageType) {
        if (_languageSet.Contains(languageType) == false) {
            Logger.TraceError($"{nameof(languageType)} is missing enum set");
            return;
        }
        
        _languageType.Value = languageType;
    }

    private void Load() {
        if (_provider == null) {
            Logger.TraceError($"{nameof(_provider)} is null");
            return;
        }

        if (_languageType.Value == null) {
            Logger.TraceError($"{nameof(_languageType)} is null");
            return;
        }

        _cacheDic = _provider.Value.Get(_languageType).ToImmutableDictionary();
    }

    public bool TryGet(string key, out string value) => string.IsNullOrEmpty(value = Get(key)) == false;

    public string Get(string key) {
        if (_cacheDic.TryGetValue(key, out var value)) {
            return value;
        }
        
        Logger.TraceLog($"[{key}] is missing key", Color.red);
        return string.Empty;
    }
    
    public bool TryGet(out string value, string key, params object[] args) {
        if (args.Length <= 0) {
            Logger.TraceError($"{nameof(args)} count is zero");
            value = string.Empty;
            return false;
        }

        if (TryGet(key, out value) == false) {
            return false;
        }

        value = string.Format(value, args);
        return true;
    }
    
    public string Get(string key, params object[] args) => TryGet(key, out var format) ? string.Format(format, args) : string.Empty;
}

[AttributeUsage(AttributeTargets.Enum)]
public class LanguageEnumAttribute : Attribute { }