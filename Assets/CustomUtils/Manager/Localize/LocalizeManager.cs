using System;
using System.Collections.Generic;
using UniRx;

public interface ILocalizeProvider {
    public Dictionary<string, string> GetLocalizeDic(Enum languageType);
}

public class LocalizeManager : Singleton<LocalizeManager> {

    private HashSet<Enum> _languageSet = new();
    private ReactiveProperty<Enum> _languageType = new();
    
    private Dictionary<string, string> _cacheDic = new();

    private ReactiveProperty<ILocalizeProvider> _bindProvider;

    public LocalizeManager() {
        var enumTypes = ReflectionManager.GetAttributeEnumTypes<LanguageAttribute>();
        if (enumTypes != null) {
            foreach (var type in enumTypes) {
                foreach (var value in Enum.GetValues(type)) {
                    if (value is Enum languageType) {
                        _languageSet.Add(languageType);
                    }
                }
            }
        }
        
        _languageType.Subscribe(languageType => {
            if (_languageType.Value.Equals(languageType) == false && _bindProvider != null) {
                Load();
            }
        });
    }
    
    public void ChangeLocalizeProvider(ILocalizeProvider provider) {
        if (provider == null) {
            Logger.TraceError($"{nameof(provider)} is Null");
            return;
        }

        _bindProvider.Value = provider;
    }
    
    public void ChangeLanguageType(Enum type) {
        if (_languageSet.Contains(type) == false) {
            Logger.TraceError($"{nameof(type)} is Not Contains {nameof(_languageSet)}");
            return;
        }
        
        _languageType.Value = type;
    }
    
    private void Load() {
        if (_languageType.Value != null && _bindProvider != null) {
            _cacheDic.Clear();
            _cacheDic = _bindProvider.Value.GetLocalizeDic(_languageType.Value);
        }
    }

    public bool TryGet(string key, out string text) {
        text = Get(key);
        return string.IsNullOrEmpty(text) == false;
    }
    
    public string Get(string key) {
        if (_cacheDic.TryGetValue(key, out var text)) {
            return text;
        }
        
        Logger.TraceLog($"{key} is Missing Key");
        return string.Empty;
    }

    public bool TryGetFormat(out string text, string key, params object[] args) {
        if (args.Length <= 0) {
            Logger.TraceError($"{nameof(args)} Count is Zero");
            text = string.Empty;
            return false;
        }

        if (TryGet(key, out text) == false) {
            return false;
        }

        text = string.Format(text, args);
        return true;

    }
    
    public string GetFormat(string key, params object[] args) => TryGet(key, out var format) ? string.Format(format, args) : string.Empty;
}

[AttributeUsage(AttributeTargets.Enum)]
public class LanguageAttribute : Attribute { }
