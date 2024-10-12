using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class UIManager : Singleton<UIManager> {

	private GameObject _root;
	private Dictionary<Type, UIBase> _cachedUIDic = new();
	
	protected Dictionary<Type, List<UIOptionAttribute>> _optionDic = new();
	protected Dictionary<Enum, IUIOptionExecute> _optionExecuteDic = new();

	public void Init() {
		/*
		
		*/
	}
	
	public T GetUI<T>() where T : UIBase {
		if (_root == null) {
			Logger.TraceError($"${nameof(_root)} is Null");
			return null;
		}
		
		if (_cachedUIDic.TryGetValue(typeof(T), out var ui) && ui != null) {
			return ui as T;
		}

		var getType = ReflectionProvider.GetSubClassTypes<UIBase>().FirstOrDefault(x => x == typeof(T));
		if (getType == null) {
			Logger.TraceError($"{nameof(getType)} is Null");
			return null;
		}

		var (prefab, anchor, _) = getType.GetCustomAttribute<UIAttribute>();
		if (string.IsNullOrEmpty(prefab)) {
			Logger.TraceError($"{nameof(prefab)} is Null or Empty");
			return null;
		}
		
		var go = Service.GetService<ResourceService>().Get(prefab);
		if (go == null) {
			Logger.TraceError($"Missing Prefabs || {prefab}");
			return null;
		}
		
		go.SetActive(false);
		go.transform.SetParent(_root.transform, false);
		
		ui = go.GetComponent<T>();
		if (ui == null) {
			Logger.TraceLog($"Missing Component {nameof(T)}. Please Add Component");
			ui = go.AddComponent<T>();
		}
		
		ui.Init();
		_cachedUIDic.Add(typeof(T), ui);

		return (T) ui;
	}

	public void OpenUI<T>(object info) where T : UIBase {
		var ui = GetUI<T>();
		if (ui == null) {
			Logger.TraceError($"{nameof(ui)} is Null");
			return;
		}

		if (ContainsUIType(ui, UIGroupType.EARLY_OPEN)) {
			ui.Open();
			ui.SetData(info);
		} else {
			ui.SetData(info); 
			ui.Open();
		}
	}

	public void OpenUI<T>() where T : UIBase {
		var ui = GetUI<T>();
		if (ui == null) {
			Logger.TraceError($"{nameof(ui)} is Null");
			return;
		}
		
		ui.Open();
	}

	public void CloseUI<T>() where T : UIBase {
		var ui = GetUI<T>();
		if (ui == null) {
			Logger.TraceError($"{nameof(ui)} is Null");
			return;
		}
		
		ui.Close();
	}

	public void CloseUI(UIGroupType type) {
		foreach (var ui in GetGroupUI(type)) {
			ui.Close();
		}
	}

	public bool TryGetIsActiveUI<T>(out T ui) where T : UIBase {
		ui = null;
		if (_cachedUIDic.TryGetValue(typeof(T), out var cachedUi)) {
			ui = cachedUi as T;
		}
		
		return ui?.IsActive() ?? false;
	}

	protected IUIOptionExecute GetOptionExecute(Enum type) {
		if (_optionExecuteDic.TryGetValue(type, out var execute) == false) {
			var executeType = ReflectionProvider.GetInterfaceTypes<IUIOptionExecute>().First(x => x.TryGetCustomAttribute<UIOptionExecuteAttribute>(out var attribute) && attribute.type.Equals(type));
			if (executeType != null) {
				if (Activator.CreateInstance(executeType) is IUIOptionExecute newExecute) {
					execute = newExecute;
					_optionExecuteDic.Add(type, newExecute);
				}
			}
		}

		return execute;
	}

	public bool IsActiveUI<T>() where T : UIBase => _cachedUIDic.TryGetValue(typeof(T), out var ui) && ui.IsActive();
	public bool IsActiveUI(UIGroupType type) => GetGroupUI(type).Any(x => x.IsActive());
	public IEnumerable<UIBase> GetGroupUI(UIGroupType type) => _cachedUIDic.Values.Where(x => ContainsUIType(x, type));
	public List<UIBase> GetGroupUIList(UIGroupType type) => GetGroupUI(type)?.ToList();
	private bool ContainsUIType(UIBase ui, UIGroupType type) => ui?.GetType().GetCustomAttribute<UIAttribute>()?.groups?.Contains(type) ?? false;
}

public enum UIGroupType {
	NONE,
	EARLY_OPEN,						// Open 우선
}