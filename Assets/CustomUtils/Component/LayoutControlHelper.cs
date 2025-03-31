using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public interface ILayoutRecursive {

    void SelectItem(LayoutItem item);
    void SelectInfo(object info);
}

public interface ILayoutItem {

    void Init();
    void SetData(object data);
    void SetActive(bool isActive);
    void SetSelect(bool isSelect);
    bool IsMatchingInfo(object info);
    bool TryGetRecursiveUI(out ILayoutRecursive ui);
    GameObject GetGameObject();
}

/// <summary>
/// Layout에 들어가는 Item을 컨트롤하는 컴포넌트
/// </summary>
// TODO. Notify 구조 개선을 통해 반응형 처리가 가능하도록 개선할 수 있는지 검
[RequireComponent(typeof(LayoutGroup))]
[DisallowMultipleComponent]
public class LayoutControlHelper : MonoBehaviour, ILayoutRecursive {

    private LayoutGroup _layoutGroup;
    public LayoutGroup LayoutGroup { get { return _layoutGroup; } }

    private List<LayoutItem> _itemList = new();
    private IList _infoList;
    
    private string _prefabName;
    private GameObject _itemPrefab;

    private RectTransform _itemRect;
    private RectTransform ItemRect => _itemRect ??= _itemPrefab.GetComponent<RectTransform>();

    private RectTransform _layoutRect;
    public RectTransform LayoutRect => _layoutRect ??= gameObject.GetComponent<RectTransform>();

    private Action<LayoutItem> _onSelectCallback;
    
    private bool _isInitialized = false;

    private void Awake() {
        _layoutGroup = GetComponent<LayoutGroup>();
        _isInitialized = true;
    }

    public void SetItemPrefab(GameObject prefab) => _itemPrefab = prefab;

    public void SetItemPrefab(string prefab) {
        _prefabName = prefab;
        SetItemPrefab(Service.GetService<ResourceService>().Get(prefab));
    }

    public void Clear() {
        _infoList?.Clear();
        _itemList?.ForEach(x => x.SetActive(false));
    }

    [TestRequired]
    public void SetData(IList infoList) {
        if (_itemPrefab == null) {
            Logger.TraceError($"{nameof(_itemPrefab)} is Null");
            _itemList.ForEach(x => x.SetActive(false));
            return;
        }

        _infoList = infoList;
        _itemList.Sync(_infoList, CreateFunc);
        if (_itemList.Count < _infoList.Count) {
            Logger.TraceError("List Count Not Matched");
            return;
        }
        
        _itemList.SyncForEach(_infoList, DataAction, ClearAction);
        // _infoList.ISyncForEach(_itemList, DataAction, ClearAction);
    }

    public void SetData(IList infoList, Action<bool> onChangeCount) {
        var isChangeCount = _infoList == null || _infoList.Count != infoList.Count;
        SetData(infoList);
        onChangeCount?.Invoke(isChangeCount);
    }

    public LayoutItem InsertItemFirst(object info, bool isClean = false) => InsertItem(0, info, isClean);
    public LayoutItem InsertItemLast(object info, bool isClean = false) => InsertItem(GetInfoCount(), info, isClean);

    public LayoutItem InsertItem(int index, object info, bool isClean = false) {
        _infoList ??= new List<object>();

        index = Math.Clamp(index, 0, GetInfoCount());
        _infoList.Insert(index, info);
        _itemList.Sync(_infoList, CreateFunc);

        if (isClean) {
            _itemList.SyncForEach(_infoList, DataAction, ClearAction);
            // _infoList.ISyncForEach(_itemList, DataAction, ClearAction);
        } else {
            for (var i = index; i < _itemList.Count; i++) {
                if (i < _infoList.Count) {
                    DataAction(_infoList[i], _itemList[i]);
                } else {
                    ClearAction(_itemList[i]);
                }
            }
        }

        index = GetCollectionIndex(index);
        if (index >= _itemList.Count) {
            Logger.TraceError($"{nameof(index)} is Invalid || Collection {nameof(index)} = {index} || ItemList.Count = {_itemList?.Count} || InfoList.Count = {_infoList?.Count}");
            return null;
        }
        
        return _itemList[index];
    }

    public TItem InsertItemFirst<TItem>(object info, bool isClean = false) where TItem : LayoutItem => InsertItem<TItem>(0, info, isClean);
    public TItem InsertItemLast<TItem>(object info, bool isClean = false) where TItem : LayoutItem => InsertItem<TItem>(_infoList?.Count ?? 0, info, isClean);
    public TItem InsertItem<TItem>(int index, object info, bool isClean = false) where TItem : LayoutItem => InsertItem(index, info, isClean) as TItem;

    public void RemoveItemFirst(bool isItemSort = false) => RemoveItem(0, isItemSort);
    public void RemoveItemLast(bool isItemSort = false) => RemoveItem(_infoList?.Count - 1 ?? 0, isItemSort);

    public void RemoveItem(object info, bool isOnlyActiveSet = false) {
        if (_itemList.TryFindIndex(out var index, x => x.IsMatchingInfo(info))) {
            RemoveItem(index, isOnlyActiveSet);
        }
    }

    public void RemoveItem<TInfo>(Func<TInfo, bool> matchFunc, bool isOnlyActiveSet = false) {
        if (_infoList == null) {
            return;
        }

        for (var index = 0; index < _infoList.Count; index++) {
            if (_infoList[index] is TInfo info && matchFunc.Invoke(info)) {
                RemoveItem(index, isOnlyActiveSet);
            }
        }
    }

    public void RemoveItem(int index, bool isOnlyActiveSet = false) {
        if (_infoList is not { Count: > 0 }) {
            return;
        }

        index = GetCollectionIndex(index);
        _infoList.RemoveAt(index);

        if (isOnlyActiveSet) {
            _itemList[index].SetActive(false);
        } else {
            _itemList.SyncForEach(_infoList, DataAction, ClearAction);
            // _infoList.ISyncForEach(_itemList, DataAction, ClearAction);
        }
    }

    public void UpdateItem(object info, bool isCreate = false) {
        if (_itemList.TryFindIndex(out var index, x => x != null && x.IsActive() && x.IsMatchingInfo(info))) {
            index = GetCollectionIndex(index);
            if (index >= 0 && index < _infoList?.Count) {
                _itemList[index].SetData(info);
                _infoList[index] = info;
            }
        } else {
            if (isCreate) {
                InsertItemLast(info);
            }
        }
    }

    public void UpdateItem(int index, object info) {
        if (_infoList == null) {
            return;
        }
        
        index = GetCollectionIndex(index);
        if (_itemList.TryFirst(index, out var item) && index > 0 && index < _infoList.Count) {
            item.SetData(info);
            _infoList[index] = info;
        }
    }

    public void UpdateItem<TInfo>(int index, Func<TInfo, TInfo> updateFunc) {
        if (_infoList is not { Count: > 0 }) {
            return;
        }
        
        index = GetCollectionIndex(index);
        if (_itemList.TryFirst(index, out var item) && index >= 0 && index < _infoList.Count && _infoList[index] is TInfo info) {
            _infoList[index] = updateFunc.Invoke(info);
            item.SetData(_infoList[index]);
        }
    }

    public void UpdateItem<TInfo>(int index, Action<TInfo> updateAction) {
	    if (_infoList is not { Count: > 0 }) {
		    return;
	    }
        
	    index = GetCollectionIndex(index);
	    if (_itemList.TryFirst(index, out var item) && index >= 0 && index < _infoList.Count && _infoList[index] is TInfo info) {
		    updateAction?.Invoke(info);
		    item.SetData(info);
	    }
    }

    public void BatchUpdateInfo<TInfo>(Action<TInfo> batchUpdateAction, bool isRefresh = true) {
        if (_infoList is not { Count: > 0 }) {
            return;
        }

        if (TryGetCastInfos<TInfo>(out var castInfos)) {
            castInfos.ForEach(batchUpdateAction.Invoke);
            if (isRefresh) {
                _itemList.SyncForEach(_infoList, (item, info) => item.SetData(info));
                // _infoList.ISyncForEach(_itemList, (info, item) => item.SetData(info));
            }
        }
    }

    public void BatchUpdateInfo<TInfo>(Func<TInfo, bool> batchUpdateAction) {
        if (_infoList is not { Count: > 0 }) {
            return;
        }

        _itemList.SyncForEach(_infoList, (item, info) => {
            if (info is TInfo castInfo && batchUpdateAction.Invoke(castInfo)) {
                item.SetData(castInfo);
            }
        });
        
        // _infoList.ISyncForEach(_itemList, (info, item) => {
        //     if (info is TInfo castInfo && (batchUpdateAction?.Invoke(castInfo) ?? false)) {
        //         item.SetData(info);
        //     }
        // });
    }

    public void BatchActionItem<TItem>(Action<TItem> batchAction) where TItem : LayoutItem{
        if (_itemList is not { Count: > 0 }) {
            return;
        }

        GetItemList<TItem>()?.ForEach(x => batchAction?.Invoke(x));
    }

    public void Sort<TInfo>(Comparison<TInfo> comparison) {
        if (TryGetCastInfos<TInfo>(out var castInfos)) {
            var castList = castInfos.Select((x, index) => new KeyValuePair<int, TInfo>(index, x)).ToList();
            castList.Sort((x, y) => comparison?.Invoke(x.Value, y.Value) ?? 0);
            _infoList = castList.ConvertAll(x => x.Value);
            Sort(castList);
        }
    }

    public void Sort<TInfo>(IComparer<TInfo> comparer) {
        if (TryGetCastInfos<TInfo>(out var castInfos)) {
            var castList = castInfos.Select((x, index) => new KeyValuePair<int, TInfo>(index, x)).OrderBy(x => x.Value, comparer).ToList();
            _infoList = castList.ConvertAll(x => x.Value);
            Sort(castList);
        }
    }

    public void Sort<TInfo, TKey>(Func<TInfo, TKey> selector) {
        if (TryGetCastInfos<TInfo>(out var castInfos)) {
            var castList = castInfos.Select((x, index) => new KeyValuePair<int, TInfo>(index, x)).OrderBy(x => selector.Invoke(x.Value)).ToList();
            _infoList = castList.ConvertAll(x => x.Value);
            Sort(castList);
        }
    }

    public void Sort<TInfo, TKey>(bool isDescending, Func<TInfo, TKey> selector) {
        if (TryGetCastInfos<TInfo>(out var castInfos)) {
            var castList = castInfos.Select((x, index) => new KeyValuePair<int, TInfo>(index, x)).ToList();
            castList = isDescending ? castList.OrderByDescending(x => selector.Invoke(x.Value)).ToList() : castList.OrderBy(x => selector.Invoke(x.Value)).ToList();
            _infoList = castList.Select(x => x.Value).ToList();
            Sort(castList);
        }
    }

    private void Sort<TInfo>(IReadOnlyList<KeyValuePair<int, TInfo>> sortInfoList) {
        var newList = new List<LayoutItem>();
        if (sortInfoList.Count <= _itemList.Count) {
            for (var i = 0; i < _itemList.Count; i++) {
                if (i >= sortInfoList.Count) {
                    for (; i < _itemList.Count; i++) {
                        _itemList[i].SetActive(false);
                        newList.Add(_itemList[i]);
                    }

                    break;
                }

                newList.Add(sortInfoList[i].Key < _itemList.Count ? _itemList[sortInfoList[i].Key] : _itemList[i]);
            }
            
            newList.IndexForEach((x, index) => x.transform.SetSiblingIndex(index));

            _itemList = newList;
        }
    }

    public void SetOnSelect(Action<LayoutItem> onSelect) => _onSelectCallback = onSelect;

    public LayoutItem SelectFirst() {
        var item = _itemList.First();
        if (item != null) {
            SelectItem(item);
        }

        return item;
    }

    public void SelectItem(LayoutItem item) {
        if (_itemList is not { Count: > 0 }) {
            Logger.TraceError($"{nameof(_itemList)} is Null or Count Zero");
            return;
        }

        _itemList.ForEach(x => {
            if (x == item) {
                x.SetSelect(true);
                _onSelectCallback?.Invoke(x);
            } else {
                x.SetSelect(false);
            }

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
            if (x.IsMatchingInfo(info)) {
                x.SetSelect(true);
                _onSelectCallback?.Invoke(x);
            } else {
                x.SetSelect(false);
            }

            if (x.TryGetRecursiveUI(out var ui)) {
                ui.SelectInfo(info);
            }
        });
    }

    private LayoutItem CreateFunc() {
        try {
            var instance = Instantiate(_itemPrefab, gameObject.transform, false);
            if (instance != null && instance.TryGetComponent<LayoutItem>(out var item)) {
                // instance.name = $"{nameof(LayoutItem)}_{_itemList?.Count ?? 0}";
                item.Init(this);
                return item;
            }
        } catch (Exception e) {
            Logger.TraceError($"{_itemPrefab.name} || {e}");
        }
        
        return null;
    }
    
    [TempMethod]
    [RefactoringRequired(10)]
    private void DataAction(LayoutItem item, object info) => DataAction(info, item);

    private void DataAction(object info, LayoutItem item) {
        try {
            if (item != null) {
                item.SetActive(true);
                item.SetData(info);
            }
        } catch (Exception e) {
            Logger.TraceError(e);
        }
    }

    private void ClearAction(LayoutItem item) {
        try {
            item.SetActive(false);
        } catch (Exception e) {
            Logger.TraceError(e);
        }
    }

    public LayoutControlHelper ItemForEach(Action<LayoutItem> action) {
        _itemList.ForEach(action);
        return this;
    }

    public bool TryGetLayout<TLayout>(out TLayout layout) where TLayout : LayoutGroup {
        layout = GetLayout<TLayout>();
        return layout != default;
    }

    public TLayout GetLayout<TLayout>() where TLayout : LayoutGroup {
        if (_layoutGroup == null) {
            Logger.TraceError($"{nameof(_layoutGroup)} is Null");
            return default;
        }

        if (_layoutGroup is TLayout castLayout) {
            return castLayout;
        }

        Logger.TraceError($"{typeof(TLayout).Name} is Missing LayoutGroup Type");
        return default;
    }

    public bool TryGetItem<TItem>(int index, out TItem item) where TItem : LayoutItem {
        item = GetItem<TItem>(index);
        return item != null;
    }

    public bool TryGetItem<TItem>(object info, out TItem item) where TItem : LayoutItem {
        item = GetItem<TItem>(info);
        return item != null;
    }

    public TItem GetItem<TItem>(int index) where TItem : LayoutItem {
        if (_itemList is not { Count: > 0 }) {
            return default;
        }

        return _itemList[GetCollectionIndex(index)] as TItem;
    }

    public TItem GetItem<TItem>(object info) where TItem : LayoutItem {
        if (_itemList is not { Count: > 0 }) {
            return default;
        }

        return _itemList.Find(x => x.IsMatchingInfo(info)) as TItem;
    }

    public bool TryGetInfo<TInfo>(int index, out TInfo info) where TInfo : class {
        info = GetInfo<TInfo>(index);
        return info != null;
    }

    public TInfo GetInfo<TInfo>(int index) where TInfo : class {
        index = GetCollectionIndex(index);
        return _infoList[index] as TInfo;
    }

    public bool TryGetIndex(object info, out int index) {
        index = GetIndex(info);
        return index > -1;
    }

    public int GetIndex(object info) {
        if (_infoList == null) {
            return -1;
        }
        
        for (var i = 0; i < _infoList.Count; i++) {
            if (_infoList[i].Equals(info)) {
                return i;
            }
        }

        return -1;
    }

    public bool TryGetItemAndIndex<TItem>(object info, out (int index, TItem item) data) where TItem : LayoutItem {
        data = GetItemAndIndex<TItem>(info);
        return data.index > -1 && data.item != default;
    }

    public (int index, TItem item) GetItemAndIndex<TItem>(object info) where TItem : LayoutItem {
        if (_infoList == null) {
            return (-1, default);
        }
        
        for (var i = 0; i < _infoList.Count; i++) {
            if (_infoList[i].Equals(info)) {
                return (i, _itemList[i] as TItem);
            }
        }

        return (-1, default);
    }

    /// <summary>
    /// index 아이템의 정규화된 위치 (0 ~ 1)
    /// </summary>
    /// <param name="index">info index</param>
    /// <returns></returns>
    public float GetNormalizePosition(int index, int collectionOffset = 0) {
        if (_itemPrefab != null) {
            var itemSize = ItemRect.sizeDelta;
            switch (_layoutGroup) {
                case GridLayoutGroup gridLayout:
                    var gridItemSize = gridLayout.startAxis switch {
                        GridLayoutGroup.Axis.Horizontal => gridLayout.cellSize.y + gridLayout.spacing.y,
                        GridLayoutGroup.Axis.Vertical => gridLayout.cellSize.x + gridLayout.spacing.x,
                        _ => 0f,
                    };

                    var layoutSize = gridLayout.startAxis switch {
                        GridLayoutGroup.Axis.Horizontal => LayoutRect.sizeDelta.y,
                        GridLayoutGroup.Axis.Vertical => LayoutRect.sizeDelta.x,
                        _ => 0f,
                    };

                    return 1 - Mathf.Lerp(0f, 1f, gridItemSize * (index / (float)gridLayout.constraintCount) / layoutSize);
                case HorizontalLayoutGroup horizontalLayout: // Not Test
                    return 1 - Mathf.Lerp(0f, 1f, ((itemSize.x + horizontalLayout.spacing) * index) / (LayoutRect.sizeDelta.x - collectionOffset));
                case VerticalLayoutGroup verticalLayout: // Not Test
                    return 1 - Mathf.Lerp(0f, 1f, ((itemSize.y + verticalLayout.spacing) * index) / LayoutRect.sizeDelta.y - collectionOffset);
                default:
                    return 1;
            }
        }

        return 1;
    }

    private int GetCollectionIndex(int index) => Math.Clamp(index, 0, GetLastIndex());
    public int GetInfoCount() => _infoList?.Count ?? 0;
    public int GetLastIndex() => Math.Max(0, GetInfoCount() - 1);
    
    public List<TItem> GetItemList<TItem>() where TItem : LayoutItem => _itemList as List<TItem> ?? _itemList.Cast<TItem>().ToList();
    public List<LayoutItem> GetItemList() => _itemList;
    
    public bool TryGetCastInfoList<TInfo>(out List<TInfo> castList) => (castList = GetCastInfoList<TInfo>()).Equals(CollectionUtil.List.Empty<TInfo>()) == false;
    public List<TInfo> GetCastInfoList<TInfo>() => _infoList == null ? CollectionUtil.List.Empty<TInfo>() : _infoList as List<TInfo> ?? _infoList.ToList<TInfo>();

    public bool TryGetCastInfos<TInfo>(out IEnumerable<TInfo> castInfos) => (castInfos = GetCastInfos<TInfo>()).Equals(Enumerable.Empty<TInfo>()) == false;
    public IEnumerable<TInfo> GetCastInfos<TInfo>() => _infoList == null ? Enumerable.Empty<TInfo>() : _infoList as IEnumerable<TInfo> ?? _infoList.Cast<TInfo>();

    public void SetActive(bool isActive) => gameObject.SetActive(isActive);
    
    public bool IsInitialized() => _isInitialized && LayoutRect.sizeDelta != Vector2.zero;
}

public abstract class LayoutItem : MonoBehaviour, ILayoutItem {

    protected LayoutControlHelper layoutHelper;

    public void Init(LayoutControlHelper layoutHelper) {
        this.layoutHelper = layoutHelper;
        Init();
    }

    public abstract void Init();
    public abstract void SetData(object data);
    public virtual void SetActive(bool isActive) => gameObject?.SetActive(isActive);
    public virtual void SetSelect(bool isSelect) { }
    public virtual bool IsMatchingInfo(object info) => false;

    public virtual bool TryGetRecursiveUI(out ILayoutRecursive ui) {
        ui = null;
        return false;
    }

    public GameObject GetGameObject() => gameObject;
    public bool IsActive() => gameObject != null && gameObject.activeSelf;

    /// <summary>
    /// <para>false, true => 1</para>
    /// <para>true, false => -1</para>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public int ActiveCompareTo(LayoutItem item) => -IsActive().CompareTo(item.IsActive());
}
