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

		var getType = ReflectionManager.GetSubClassTypes<UIBase>().FirstOrDefault(x => x == typeof(T));
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
			var executeType = ReflectionManager.GetInterfaceTypes<IUIOptionExecute>().First(x => x.TryGetCustomAttribute<UIOptionExecuteAttribute>(out var attribute) && attribute.type.Equals(type));
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

public static class UIComponentExtension {
	
	public static GameObject FindGameObject(this GameObject go, string objectName) => go.transform.FindTransform(objectName)?.gameObject;
	public static GameObject FindGameObject(this Transform transform, string objectName) => transform.FindTransform(objectName)?.gameObject;

	public static Transform FindTransform(this GameObject go, string objectName) => go.transform.FindTransform(objectName);
	public static Transform FindTransform(this Transform transform, string objectName) {
		if (transform.name == objectName) {
			return transform;
		}

		foreach (Transform tr in transform.transform) {
			if (tr.TryFindTransform(objectName, out var findTransform)) {
				return findTransform;
			}
		}

		return null;
	}

	public static bool TryFindTransform(this GameObject go, string objectName, out Transform findTransform) {
		findTransform = go.FindTransform(objectName);
		return findTransform != null;
	}
	
	public static bool TryFindTransform(this Transform transform, string objectName, out Transform findTransform) {
		findTransform = transform.FindTransform(objectName);
		return findTransform != null;
	}
	
	public static bool TryFindGameObject(this Transform transform, string objectName, out GameObject findGameObject) {
		findGameObject = transform.FindGameObject(objectName);
		return findGameObject != null;
	}
	
	public static bool TryFindGameObject(this GameObject go, string objectName, out GameObject findGameObject) {
		findGameObject = go.FindGameObject(objectName);
		return findGameObject != null;
	}
	
	public static T FindComponent<T>(this GameObject go, string objectName) => go.transform.FindComponent<T>(objectName);

	public static T FindComponent<T>(this Transform transform, string objectName) {
		var findObject = transform.FindGameObject(objectName);
		if (findObject == null) {
			Logger.TraceError($"{nameof(findObject)} is Null || {nameof(objectName)} = {objectName}");
			return default;
		}

		var findComponent = findObject.GetComponent<T>();
		if (findComponent == null) {
			Logger.TraceError($"{nameof(findComponent)} is Null || T = {typeof(T).FullName}");
			return default;
		}

		return findComponent;
	}

	public static bool TryFindComponent<T>(this GameObject go, string objectName, out T component) => go.transform.TryFindComponent(objectName, out component);
	
	public static bool TryFindComponent<T>(this Transform transform, string objectName, out T component) {
		component = transform.FindComponent<T>(objectName);
		return component != null;
	}
}

public enum UIGroupType {
	NONE,
	EARLY_OPEN,						// Open 우선
}