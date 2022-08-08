using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public interface IUILayoutRecursive {
	void SelectItem(UILayoutItem item);
	void SelectInfo(object info);
}

/// <summary>
/// Layout에 들어가는 Item을 컨트롤하는 컴포넌트
/// </summary>
[RequireComponent(typeof(LayoutGroup))]
[DisallowMultipleComponent]
public class UILayoutControlHelper : MonoBehaviour, IUILayoutRecursive {

    private LayoutGroup _layoutGroup;
    
    private List<UILayoutItem> _itemList = new();
    private IList _infoList;

    private string _prefabName;

    private void Awake() {
        _layoutGroup = GetComponent<LayoutGroup>();
    }

    public void SetItemPrefab(string prefab) => _prefabName = prefab;
    public void Clear() {
        _infoList?.Clear();
        _itemList?.ForEach(x => x.SetActive(false));
    }

    public void SetData(IList infoList) {
        if (string.IsNullOrEmpty(_prefabName)) {
            Logger.TraceError($"{nameof(_prefabName)} is Null or Empty");
            _itemList.ForEach(x => x.SetActive(false));
            return;
        }
        
        _infoList = infoList;
		_itemList.ISync(_infoList, CreateFunc);
		if (_itemList.Count < _infoList.Count) {
			Logger.TraceError("List Count Not Matched");
			return;
		}

		_infoList.ISyncForEach(_itemList, DataAction, ClearAction);
    }

    public void InsertItemFirst(object info, bool isClean = false) => InsertItem(0, info, isClean);
    public void InsertItemLast(object info, bool isClean = false) => InsertItem(_infoList?.Count ?? 0, info, isClean);

    public void InsertItem(int index, object info, bool isClean = false) {
        _infoList ??= new List<object>();

        index = Math.Clamp(index, 0, _infoList.Count);
        _infoList.Insert(index, info);
        _itemList.ISync(_infoList, CreateFunc);

        if (isClean) {
            _infoList.ISyncForEach(_itemList, DataAction, ClearAction);
        } else {
            for (var i = index; i < _itemList.Count; i++) {
                if (i < _infoList.Count) {
                    DataAction(_infoList[i], _itemList[i]);  
                } else {
                    ClearAction(_itemList[i]);
                }
            }
        }
    }

    public void RemoveItemFirst(bool isItemSort = false) => RemoveItem(0, isItemSort);
    public void RemoveItemLast(bool isItemSort = false) => RemoveItem(_infoList?.Count - 1 ?? 0, isItemSort);

    public void RemoveItem(object info, bool isItemSort = false) {
        if (_itemList.TryFindIndex(x => x.IsMatchingInfo(info), out var index)) {
            RemoveItem(index, isItemSort);
        }
    }
    
    public void RemoveItem(int index, bool isItemSort = false) {
        _infoList ??= new List<object>();

        index = Math.Clamp(index, 0, _infoList.Count);
        _infoList.RemoveAt(index);

        if (isItemSort) {
            _itemList[index].SetActive(false);
            _itemList.Sort((x, y) => x.ActiveCompareTo(y));
        } else {
            _infoList.ISyncForEach(_itemList, DataAction, ClearAction);    
        }
    }

    public void UpdateItem(object info) {
        UILayoutItem matchItem = null;
        _itemList.ForEach(x => {
            if (x.IsMatchingInfo(info)) {
                matchItem = x;
            }
        });

        if (matchItem != null) {
            matchItem.SetData(info);
        } else {
            InsertItemLast(info);
        }
    }

    public void SelectFirst() {
		var item = _itemList.First();
		if (item != null) {
			SelectItem(item);	
		}
	}
	
	public void SelectItem(UILayoutItem item) {
		if (_itemList is not { Count: > 0 }) {
			Logger.TraceError($"{nameof(_itemList)} is Null or Count Zero");
			return;
		}
		
		_itemList.ForEach(x => {
			x.SetSelect(x == item);
			if (x.TryGetRecursiveUI(out var ui)) {
				ui.SelectItem(item);
			}
		});
	}

	public void SelectInfo(object info) {
		if (_itemList is not { Count: > 0 }) {
			Logger.TraceError($"{nameof(_itemList)} is Null or Count Zero");
			return;
		}

		_itemList.ForEach(x => {
			x.SetSelect(x.IsMatchingInfo(info));
			if (x.TryGetRecursiveUI(out var ui)) {
				ui.SelectInfo(info);
			}
		});
	}

	private UILayoutItem CreateFunc() {
		var item = ResourceManager.inst.Get<UILayoutItem>(_prefabName);
		item.Init();
		return item;
	}

    private UILayoutItem CreateDummyFunc() {
        var item = CreateFunc();
        item.gameObject.SetActive(false);
        return item;
    }
	
	private void DataAction(object info, UILayoutItem item) {
		item.SetActive(true);
		item.SetData(info);
    }

	private void ClearAction(UILayoutItem item) => item.SetActive(false);

    public T GetLayout<T>() where T : LayoutGroup {
        if (_layoutGroup == null) {
            Logger.TraceError($"{nameof(_layoutGroup)} is Null");
            return default;
        }

        if (_layoutGroup is T castLayout) {
            return castLayout;
        }
        
        Logger.TraceError($"{nameof(T)} is Missing LayoutGroup Type");
        return default;
    }
}

public abstract class UILayoutItem : MonoBehaviour {
    
	public abstract void Init();
	public abstract void SetData(object data);
	public virtual void SetActive(bool isActive) => gameObject.SetActive(isActive);
 	public virtual void SetSelect(bool isSelect) { }
    public virtual bool IsMatchingInfo(object info) => false;
    public virtual bool TryGetRecursiveUI(out IUILayoutRecursive ui) {
	    ui = null;
	    return false;
    }
    
    public GameObject GetGameObject() => gameObject;
    public bool IsActive() => gameObject.activeSelf;
    /// <summary>
    /// <para>false, true => 1</para>
    /// <para>true, false => -1</para>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public int ActiveCompareTo(UILayoutItem item) => -IsActive().CompareTo(item.IsActive());
}
