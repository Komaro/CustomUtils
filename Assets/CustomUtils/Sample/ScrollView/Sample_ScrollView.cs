// This code will not work without the OSA(Optimized ScrollView Adapter) Asset.
// https://assetstore.unity.com/packages/tools/gui/optimized-scrollview-adapter-68436

/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Sample_ScrollView : OSA<Sample_ScrollViewParams, Sample_ItemViewsHolder>, IScrollView {

    private RectTransform _rect;
    private RectTransform _content;
    private RectTransform _viewPort;

    protected IList _dataList;
    public IList p_dataList => _dataList;

    private List<Sample_ItemViewsHolder> _itemHolderList = new();

    private int _selectItemIndex = -1;
    private Action<int, object> _onSelectCallback;
    private Action<int, IScrollViewItem> _onVisibleSelectCallback;

    protected int _paddingCollectIndex;
    
    protected override void Awake() {
        if (IsInitialized == false) {
            _Params.Content = GetContent();
            _Params.Viewport = GetViewPort();
        }
    }

    private new IEnumerator Start() {
        yield return null;

        if (gameObject.transform is RectTransform RT && RT.IsStretch()) {
            var fixSize = RT.rect.size;
            var fixPosition = RT.localPosition;
            RT.SetAnchor(AnchorPresets.MIDDLE_CENTER);
            RT.sizeDelta = fixSize;
            RT.localPosition = fixPosition;
        }

        base.Start();
    }

    protected override void OnInitialized() {
        var padding = _Params.Orientation switch {
            BaseParams.OrientationEnum.HORIZONTAL => _Params.ContentPadding.left + _Params.ContentPadding.right,
            BaseParams.OrientationEnum.VERTICAL => _Params.ContentPadding.top + _Params.ContentPadding.bottom,
            _ => 0,
        };
        
        _paddingCollectIndex = (int) Math.Ceiling(padding / _Params.DefaultItemSize);

        base.OnInitialized();
    }

    public void ForceInit() {
        if (IsInitialized == false) {
            _Params.Content = GetContent();
            _Params.Viewport = GetViewPort();
            Init();
        }
    }

    public void SetItemPrefab(string prefab) => SetItemPrefab(ResourcesManager.Instance.LoadPrefab(prefab));

    public void SetItemPrefab(GameObject itemPrefab) {
        if (itemPrefab == null) {
            Logger.TraceError($"{nameof(itemPrefab)} is Null");
            return;
        }

        _Params.SetPrefab(itemPrefab);
    }

    public void Clear() {
        _itemHolderList?.ForEach(x => x.Clear());
        if (_Params.optimization.KeepItemsPoolOnEmptyList == false) {
            _itemHolderList?.Clear();
        }
        
        _selectItemIndex = -1;
        
        _dataList?.Clear();
        if (IsInitialized) {
            ResetItems(_dataList?.Count ?? 0);
        }
    }

    /// <summary>
    /// 다음 프레임 작업때 스크롤 뷰에 데이터를 설정
    /// </summary>
    /// <param name="dataList"></param>
    /// <param name="isResetPosition"></param>
    /// <typeparam name="T"></typeparam>
    public void SetData<T>(List<T> dataList, bool isResetPosition = false) {
        dataList ??= new List<T>();
        _dataList = dataList;
        
        _selectItemIndex = -1;
        if (_Params.Config.IsMultipleSelect) {
            _Params.Config.SelectSet.Clear();
        }
        
        if (IsInitialized == false) {
            Observable.EveryLateUpdate().SkipWhile(_ => IsInitialized == false).First().Subscribe(_ => {
                ResetItems(_dataList.Count);
                if (isResetPosition) {
                    MoveTo(0);
                }
            });
        } else {
            ResetItems(_dataList.Count);
            if (isResetPosition) {
                MoveTo(0);
            }
        }
    }

    public void SetData<T>(List<T> dataList, Action callback, bool isResetPosition = false) {
        if (IsInitialized == false) {
            Observable.EveryLateUpdate().SkipWhile(_ => IsInitialized == false).First().Subscribe(_ => {
                SetData(dataList, isResetPosition);
                callback?.Invoke();
            });
        } else {
            SetData(dataList, isResetPosition);
            callback?.Invoke();
        }
    }

    public void RefreshVisible() {
        if (_Params.Config.IsScheduleCompute) {
            throw new OSAException($"{_Params.Config.IsScheduleCompute} is true. Must be false for {nameof(UpdateItem)} to work.");
        }
    
        if (_dataList == null || _dataList.Count <= 0) {
            Logger.TraceError($"{nameof(_dataList)} is Null or Empty");
            return;
        }

        _VisibleItems.ForEach(UpdateViewsHolder);
    }

    protected override Sample_ItemViewsHolder CreateViewsHolder(int itemIndex) {
        if (_Params.TryGetPrefab(out var prefab)) {
            var instance = new Sample_ItemViewsHolder();
            _itemHolderList.Add(instance);
            instance.Init(prefab, GetContent(), itemIndex);
            instance.root.name += $"[{instance}]";
            return instance;
        }

        Logger.TraceError($"{nameof(prefab)} is Null");
        return null;
    }

    protected override void UpdateViewsHolder(Sample_ItemViewsHolder newOrRecycled) {
        if (_dataList is not { Count: > 0 } || _dataList.Count < newOrRecycled.ItemIndex) {
            return;
        }

        var data = _dataList[newOrRecycled.ItemIndex];
        if (data == null) {
            return;
        }

        newOrRecycled.SetData(data);
        newOrRecycled.MarkForRebuild();
        if (_Params.Config.IsScheduleCompute) {
            ScheduleComputeVisibilityTwinPass(true);
        }
    }

    public void InsertItemFirst<T>(T data, bool isMove = false) => InsertItem(0, data, isMove);
    public void InsertItemLast<T>(T data, bool isMove = false) => InsertItem(GetDataCount(), data, isMove);

    public virtual void InsertItem<T>(int index, T data, bool isMove = false) {
        _dataList ??= new List<T>();

        index = Math.Clamp(index, 0, GetDataCount());
        
        _dataList.Insert(index, data);
        InsertItem(index);

        if (isMove) {
            MoveTo(index);
        }
    }

    protected void InsertItem(int index) {
        if (InsertAtIndexSupported) {
            InsertItems(index, 1);
        } else {
            ResetItems(_dataList?.Count ?? 0);
        }
    }

    public override void InsertItemWithViewsHolder(Sample_ItemViewsHolder viewsHolder, int atIndex, bool contentPanelEndEdgeStationary) {
        _itemHolderList.Insert(atIndex, viewsHolder);
        base.InsertItemWithViewsHolder(viewsHolder, atIndex, contentPanelEndEdgeStationary);
    }

    public void RemoveItemFirst() => RemoveItem(0);
    public void RemoveItemLast() => RemoveItem(GetDataCount());

    public virtual void RemoveItem(int index) {
        if (_dataList is not { Count: > 0 }) {
            return;
        }

        index = GetCollectionIndex(index);

        _dataList.RemoveAt(index);

        if (RemoveFromIndexSupported) {
            RemoveItems(index, 1);
        } else {
            ResetItems(GetDataCount());
        }
    }

    public override void RemoveItemWithViewsHolder(Sample_ItemViewsHolder viewsHolder, bool stealViewsHolderInsteadOfRecycle, bool contentPanelEndEdgeStationary) {
        _itemHolderList.Remove(viewsHolder);
        base.RemoveItemWithViewsHolder(viewsHolder, stealViewsHolderInsteadOfRecycle, contentPanelEndEdgeStationary);
    }

    public override void ChangeItemsCount(ItemCountChangeMode changeMode, int itemsCount, int indexIfInsertingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false) {
        base.ChangeItemsCount(changeMode, itemsCount, indexIfInsertingOrRemoving, contentPanelEndEdgeStationary, keepVelocity);
        SwitchFitDrag();
        SwitchDynamicGravity();
    }

    public void UpdateItem(int index) {
        if (_Params.Config.IsScheduleCompute) {
            throw new OSAException($"{_Params.Config.IsScheduleCompute} is true. Must be false for {nameof(UpdateItem)} to work.");
        }
    
        var holder = GetItemViewsHolderIfVisible(index);
        if (holder != null) {
            UpdateViewsHolder(holder);
        }
    }
    
    public void UpdateItem(int index, object info) {
        index = GetCollectionIndex(index);
        if (_dataList != null && _dataList.Count > index) {
            _dataList[index] = info;
            UpdateItem(index);
        }
    }

    public void UpdateItem<T>(int index, Func<T, T> updateFunc) {
        index = GetCollectionIndex(index);
        if (TryGetCastInfoList<T>(out var castList) && castList.Count > index) {
            castList[index] = updateFunc.Invoke(castList[index]);
            UpdateItem(index); 
        }
    }
    
    public void UpdateSelectItem() => UpdateItem(GetCollectionIndex(GetSelectItemIndex()));

    public void SwitchFitDrag() {
        if (_Params.Config.IsDynamicFitScroll) {
            if (GetDataCount() > 0) {
                if (IsFitScrollView() == false) {
                    SetEnableDrag(true);
                    return;
                }
            }
            
            SetEnableDrag(false);
        } else if (_Params.Config.IsFitScroll) {
            // 실질적으로 동작하지 않음
            SetEnableDrag(_Params.GetCollectionCellItemSize() * GetDataCount() <= _InternalState.vpSize == false);
        }
    }

    public void SwitchDynamicGravity() {
        if (_Params.Config.IsDynamicGravity) {
            var isFit = IsFitScrollView();
            _Params.Gravity = _Params.Config.DynamicGravityType switch {
                E_DYNAMIC_GRAVITY_TYPE.START_TO_CENTER => isFit ? BaseParams.ContentGravity.CENTER : BaseParams.ContentGravity.START,
                E_DYNAMIC_GRAVITY_TYPE.START_TO_END => isFit ? BaseParams.ContentGravity.END : BaseParams.ContentGravity.START,
                E_DYNAMIC_GRAVITY_TYPE.CENTER_TO_START => isFit ? BaseParams.ContentGravity.START : BaseParams.ContentGravity.CENTER,
                E_DYNAMIC_GRAVITY_TYPE.CENTER_TO_END => isFit ? BaseParams.ContentGravity.END : BaseParams.ContentGravity.CENTER,
                E_DYNAMIC_GRAVITY_TYPE.END_TO_START => isFit ? BaseParams.ContentGravity.START : BaseParams.ContentGravity.END,
                E_DYNAMIC_GRAVITY_TYPE.END_TO_CENTER => isFit ? BaseParams.ContentGravity.CENTER : BaseParams.ContentGravity.END,
                _ => _Params.Gravity
            };
            
            _Params.UpdateContentPivotFromGravityType();
        }
    }

    private bool IsFitScrollView() {
        var vpSize = GetCollectionVpSize();
        var addItemSize = GetCollectionItemSize();
        var visibleSize = 0f;
        for (var i = 0; i < GetDataCount(); i++) {
            visibleSize += addItemSize;
            if (visibleSize > vpSize) {
                return false;
            }
        }
        
        return true;
    }
    
    protected double GetCollectionVpSize() {
        switch (_Params.Orientation) {
            case BaseParams.OrientationEnum.VERTICAL:
                return _InternalState.vpSize - _Params.ContentPadding.top - _Params.ContentPadding.bottom;
            case BaseParams.OrientationEnum.HORIZONTAL:
                return _InternalState.vpSize - _Params.ContentPadding.left - _Params.ContentPadding.right;
        }

        return _InternalState.vpSize;
    }
    
    protected float GetCollectionItemSize() {
        if (_itemHolderList.Count > 0 && _itemHolderList.First().TryGetRectTransform(out var rectTransform)) {
            var rect = rectTransform.rect;
            return _Params.Orientation switch {
                BaseParams.OrientationEnum.HORIZONTAL => rect.width + _Params.ContentSpacing,
                BaseParams.OrientationEnum.VERTICAL => rect.height + _Params.ContentSpacing,
                _ => _Params.GetCollectionCellItemSize(),
            };
        }

        return _Params.GetCollectionCellItemSize();
    }

    public void SetEnableDrag(bool isEnable) {
        _Params.DragEnabled = isEnable;
        _Params.ScrollEnabled = isEnable;
    }

    public void SetOnSelect(Action<int, object> onSelect) => _onSelectCallback = onSelect;
    public void SetOnVisibleSelect(Action<int, IScrollViewItem> onVisibleSelect) => _onVisibleSelectCallback = onVisibleSelect;

    public void SelectItem(int index) {
        if (_dataList == null || _dataList.Count <= 0) {
            return;
        }

        SetSelectItemIndex(index);
        
        var isVisible = false;
        foreach (var holder in _itemHolderList) {
            if (holder == null) {
                continue;
            }

            SelectItem(holder);
            if (holder.ItemIndex == _selectItemIndex) {
                isVisible = true;
            }
        }

        if (isVisible == false && _dataList != null && _dataList.Count > _selectItemIndex) {
            if (_onSelectCallback != null)
                _onSelectCallback?.Invoke(_selectItemIndex, _dataList[_selectItemIndex]);
        }
    }

    public void SelectItem<T>(Predicate<T> match) {
        if (TryGetCastInfoList<T>(out var castList) && castList.TryFindIndex(match, out var index)) {
            SelectItem(index);
        }
    }

    public void SelectVisibleItem(int index) {
        if (_dataList == null || _dataList.Count <= 0) {
            return;
        }
        
        SetSelectItemIndex(index);
        _itemHolderList?.ForEach(holder => {
            if (holder == null) {
                return;
            }
            
            SelectItem(holder);
        });
    }
    
    private void SelectItem(IScrollViewsItemHolder holder) {
        if (holder.IsVisible() == false) {
            return;
        }

        if (holder.IsMatchIndex(_selectItemIndex)) {
            _onVisibleSelectCallback?.Invoke(holder.GetItemIndex(), holder.GetItem());
        }

        if (_Params.Config.IsMultipleSelect) {
            holder.Select(_Params.Config.SelectSet.Contains(holder.GetItemIndex()));
        } else {
            holder.Select(_selectItemIndex);
        }
    }

    public void SelectVisibleItem(IScrollViewItem item) => SelectVisibleItem(item.GetIndex());
    public bool IsSelectItem(IScrollViewItem item) => IsSelectItem(item.GetIndex());
    public bool IsSelectItem(int index) => _Params.Config.IsMultipleSelect ? _Params.Config.SelectSet.Contains(index) : _selectItemIndex == index;
    public int GetSelectItemIndex() => _selectItemIndex;
    public void SetSelectItemIndex(int index) {
        index = GetCollectionIndex(index);
        if (_Params.Config.IsMultipleSelect) {
            if (_Params.Config.SelectSet.Contains(index)) {
                _Params.Config.SelectSet.Remove(index);
            } else {
                _Params.Config.SelectSet.Add(index);
            }
        }
        
        _selectItemIndex = index;
    }

    public void MoveToFirst(float duration = 1f, Action onDone = null) => MoveTo(0, duration, onDone);
    public void MoveToLast(float duration = 1f, Action onDone = null) => MoveTo(GetDataCount() - 1, duration, onDone);

    public void MoveTo(int index, float duration = 1f, Action onDone = null) {
        if (GetDataCount() <= 0) {
            onDone?.Invoke();
            return;
        }
        
        index = GetCollectionIndex(index);
        if (IsInitialized == false) {
            Observable.EveryLateUpdate().SkipWhile(_ => IsInitialized == false).First().Subscribe(_ => SmoothScrollTo(index, duration, onDone: onDone));
        } else {
            SmoothScrollTo(GetCollectionIndex(index), duration, onDone: onDone);
        }
    }
    
    public void MoveTo<T>(Predicate<T> match, int collectionValue = 0, float duration = 1f, Action onDone = null) {
        var index = GetCastInfoList<T>()?.FindIndex(x => match?.Invoke(x) ?? false) ?? 0;
        MoveTo(index + collectionValue, duration, onDone);
    }

    public void MoveToWithPadding(int index, float duration = 1f, Action onDone = null) {
        index = GetCollectionIndex(index);
        if (IsInitialized == false) {
            Observable.EveryLateUpdate().SkipWhile(_ => IsInitialized == false).First().Subscribe(_ => SmoothScrollTo(index, duration, GetPaddingCollectionOffset(), onDone: onDone));
        } else {
            SmoothScrollTo(index, duration, GetPaddingCollectionOffset(), onDone: onDone);
        }
    }

    public void MoveToWithPadding<T>(Predicate<T> match, int collectionValue = 0, float duration = 1f, Action onDone = null) {
        var index = GetCastInfoList<T>()?.FindIndex(match) ?? 0;
        MoveToWithPadding(index + collectionValue, duration, onDone);
    }

    public float GetPaddingCollectionOffset() {
        if (_InternalState == null || _Params == null) {
            return 0f;
        }

        return _Params.Orientation switch {
            BaseParams.OrientationEnum.HORIZONTAL => _Params.Gravity switch {
                BaseParams.ContentGravity.START => _Params.ContentPadding.left / (float)_InternalState.vpSize,
                BaseParams.ContentGravity.END => _Params.ContentPadding.right / (float)_InternalState.vpSize,
                _ => 0f,
            },
            BaseParams.OrientationEnum.VERTICAL => _Params.Gravity switch {
                BaseParams.ContentGravity.START => _Params.ContentPadding.top / (float)_InternalState.vpSize,
                BaseParams.ContentGravity.END => _Params.ContentPadding.bottom / (float)_InternalState.vpSize,
                _ => 0f,
            },
            _ => 0f,
        };
    }

    public bool TryGetItem<T>(int index, out T item) where T : UIScrollViewItem {
        item = GetItem<T>(index);
        return item != null;
    }

    public bool TryGetIndex(object info, out int index) {
        index = GetIndex(info);
        return index > -1;
    }

    public int GetIndex(object info) {
        if (_dataList is not { Count: > 0 }) {
            return -1;
        }
        
        for (var i = 0; i < _dataList.Count; i++) {
            if (_dataList[i].Equals(info)) {
                return i;
            }
        }

        return -1;
    }

    public int GetIndex<T>(Predicate<T> match) {
        if (_dataList is not { Count: > 0 }) {
            return -1;
        }

        if (TryGetCastInfoList<T>(out var castList)) {
            return castList.FindIndex(x => match?.Invoke(x) ?? false);
        }

        return -1;
    }
    
    public void RefreshWithEmpty(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false) {
        if (GetDataCount() <= 0 && _dataList is { Count: > 0 } || _dataList?.Count != GetDataCount()) {
            if (IsInitialized == false) {
                Observable.EveryLateUpdate().SkipWhile(_ => IsInitialized == false).First().Subscribe(_ => ResetItems(_dataList?.Count ?? 0));
            } else {
                ResetItems(_dataList?.Count ?? 0);
            }
            return;
        }

        if (IsInitialized == false) {
            Observable.EveryLateUpdate().SkipWhile(_ => IsInitialized == false).First().Subscribe(_ => base.Refresh(contentPanelEndEdgeStationary, keepVelocity));
        } else {
            base.Refresh(contentPanelEndEdgeStationary, keepVelocity);
        }
    }
    
    public bool TryGetCastInfoList<T>(out List<T> castList) {
        castList = GetCastInfoList<T>();
        return castList != null;
    }

    public List<T> GetCastInfoList<T>() {
        if (_dataList == null) {
            return null;
        }
        
        return _dataList as List<T> ?? _dataList.CastList<T>();
    }

    public bool TryGetCastInfos<T>(out IEnumerable<T> castInfos) {
        castInfos = GetCastInfos<T>();
        return castInfos != null;
    }

    public IEnumerable<T> GetCastInfos<T>() {
        if (_dataList == null) {
            return null;
        }

        return _dataList as IEnumerable<T> ?? _dataList.Cast<T>();
    }

    private int GetCollectionIndex(int index) => Math.Clamp(index, 0, Math.Max(0, GetDataCount() - 1));
    public T GetItem<T>(int index) where T : UIScrollViewItem => GetItemViewsHolder(index)?.GetItem() as T;
    public List<T> GetVisibleItemList<T>() where T : UIScrollViewItem => _VisibleItems.ConvertAll(x => x.GetItem() as T);
    public int GetDataCount() => _ItemsDesc?.itemsCount ?? 0;
    public int GetPaddingCollectIndex() => _paddingCollectIndex;
    public int GetVisibleFirstIndex() => _VisibleItems?.FirstOrDefault()?.ItemIndex ?? 0;
    public int GetVisibleLastIndex() => _VisibleItems?.LastOrDefault()?.ItemIndex ?? 0;
    public RectTransform GetRect() => _rect ??= transform.GetRectTransform();
    public RectTransform GetContent() => _content ??= GameObjectUtil.FindComponent<RectTransform>(gameObject, "Content");
    public RectTransform GetViewPort() => _viewPort ??= GameObjectUtil.FindComponent<RectTransform>(gameObject, "Viewport");
    public UIConfigBase GetConfig() => _Params?.Config;
    public void SetActive(bool isActive) => gameObject.SetActive(isActive);
}

[Serializable]
public class Sample_ScrollViewParams : BaseParams, IScrollViewParam {

    private GameObject _itemPrefab;
    
    public void SetPrefab(GameObject itemPrefab) {
        if (itemPrefab == null) {
            Logger.TraceLog($"{nameof(itemPrefab)} is Null");
            return;
        }

        _itemPrefab = itemPrefab;
        if (_itemPrefab.GetComponent<ContentSizeFitter>() == null && _itemPrefab.TryGetComponent<RectTransform>(out var rect)) {
            var size = rect.sizeDelta;
            DefaultItemSize = Orientation switch {
                OrientationEnum.VERTICAL => size.y,
                OrientationEnum.HORIZONTAL => size.x,
                _ => DefaultItemSize
            };
        }
    }

    public GameObject GetPrefab() => _itemPrefab;

    public bool TryGetPrefab(out GameObject itemPrefab) {
        itemPrefab = _itemPrefab;
        return itemPrefab != null;
    }
    
    public float GetCollectionCellItemSize() => DefaultItemSize + ContentSpacing;

    #region Config
    
    [SerializeField]
    private UIConfig _uiConfig = new();
    public UIConfig Config { get => _uiConfig; set => _uiConfig = value; }

    #endregion
}

public class Sample_ItemViewsHolder : BaseItemViewsHolder, IScrollViewsItemHolder {

    private RectTransform _rectTransform;
    private UIScrollViewItem _item;

    public override void CollectViews() {
        base.CollectViews();
        _rectTransform = root.GetComponent<RectTransform>();
        _item = root.GetComponent<UIScrollViewItem>();
        _item.Init(this);
    }

    public void SetData(object ob) => _item.SetData(ob);

    public void Select() {
        if (_item != null && _item.isActiveAndEnabled) {
            _item.OnSelect();
        }
    }
    
    public void Select(bool isSelected) {
        if (_item != null && _item.isActiveAndEnabled) {
            _item.OnSelect(isSelected);
        }
    }

    public void Select(int selectIndex) {
        if (_item != null && _item.isActiveAndEnabled) {
            _item.OnSelect(_item.GetIndex() == selectIndex);
        }
    }

    public bool IsMathItem(IScrollViewItem item) => _item == (UIScrollViewItem)item;
    public IScrollViewItem GetItem() => _item;
    public void Clear() => _item?.OnClear();
    public bool IsVisible() => _item != null && _item.isActiveAndEnabled;
    public bool IsMatchIndex(int index) => ItemIndex == index;

    public bool TryGetRectTransform(out RectTransform rectTransform) {
        rectTransform = GetRectTransform();
        return rectTransform != null;
    }
    
    public RectTransform GetRectTransform() => _rectTransform;

    public int GetItemIndex() => ItemIndex;
}
*/
