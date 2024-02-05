// This code will not work without the OSA(Optimized ScrollView Adapter) Asset.
// https://assetstore.unity.com/packages/tools/gui/optimized-scrollview-adapter-68436

/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

[Serializable]
public class Sample_UIGridScrollView : GridAdapter<Sample_GridScrollViewParams, Sample_GridItemViewsHolder>, IScrollView {

    private RectTransform _rect;
    private RectTransform _content;
    private RectTransform _viewPort;

    protected IList _dataList;
    public IList p_dataList;

    private ReactiveCollection<CellGroupViewsHolder<Sample_GridItemViewsHolder>> _groupHolderList = new();

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
            BaseParams.OrientationEnum.HORIZONTAL => _Params.Grid.GroupPadding.left + _Params.Grid.GroupPadding.right,
            BaseParams.OrientationEnum.VERTICAL => _Params.Grid.GroupPadding.top + _Params.Grid.GroupPadding.bottom,
            _ => 0,
        };
        
        _paddingCollectIndex = (int) Math.Ceiling(padding / _Params.DefaultItemSize);
        
        if (_Params.Config.IsDynamicAlignment) {
            RebuildLayoutDueToScrollViewSizeChange();
        }
        
        base.OnInitialized();
    }

    public void ForceInit() {
        if (IsInitialized == false) {
            _Params.Content = GetContent();
            _Params.Viewport = GetViewPort();
            Init();
        }
    }

    public void SetItemPrefab(string itemPrefab) => SetItemPrefab(ResourcesManager.Instance.LoadPrefab(itemPrefab));

    public void SetItemPrefab(GameObject itemPrefab) {
        if (itemPrefab == null) {
            Logger.TraceError($"{nameof(itemPrefab)} is Null");
            return;
        }

        _Params.SetPrefab(itemPrefab);
    }

    public void Clear() {
        _groupHolderList?.SelectMany(x => x.ContainingCellViewsHolders).ForEach(x => x.Clear());
        if (_Params.optimization.KeepItemsPoolOnEmptyList == false) {
            _groupHolderList?.Clear();
        }
        
        _selectItemIndex = -1;
        
        _dataList?.Clear();
        if (IsInitialized) {
            ResetItems(_dataList?.Count ?? 0);
        }
    }

    protected override void OnCellViewsHolderCreated(Sample_GridItemViewsHolder cellVH, CellGroupViewsHolder<Sample_GridItemViewsHolder> cellGroup) {
        if (_groupHolderList.Contains(cellGroup) == false) {
            _groupHolderList.Add(cellGroup);
        }
    }

    public void SetData<T>(List<T> dataList, bool isResetPosition = false) {
        dataList ??= new List<T>();
        _dataList = dataList;
        
        _selectItemIndex = -1;
        if (_Params.Config.IsMultipleSelect) {
            _Params.Config.SelectSet.Clear();
        }
        
        if (IsInitialized == false) {
            Observable.EveryLateUpdate().SkipWhile(_ => IsInitialized == false).First().Subscribe(_ => {
                ResetItems(dataList.Count);
                if (isResetPosition && GetDataCount() > 0) {
                    ScrollTo(0);
                }
            });
        } else {
            ResetItems(dataList.Count);
            if (isResetPosition && GetDataCount() > 0) {
                ScrollTo(0);
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

    public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false) {
        _CellsCount = _dataList?.Count ?? 0;
        base.Refresh(contentPanelEndEdgeStationary, keepVelocity);
    }
    
    public void RefreshVisible() {
        if (_dataList == null || _dataList.Count <= 0) {
            Logger.TraceError($"{nameof(_dataList)} is Null or Empty");
            return;
        }

        _VisibleItems.ForEach(UpdateViewsHolder);
    }
    
    protected override void UpdateCellViewsHolder(Sample_GridItemViewsHolder viewsHolder) {
        if (_dataList.Count <= viewsHolder.ItemIndex) {
            return;
        }

        var data = _dataList[viewsHolder.ItemIndex];
        if (data == null) {
            return;
        }

        viewsHolder.SetData(data);
    }

    protected override void OnBeforeRecycleOrDisableCellViewsHolder(Sample_GridItemViewsHolder viewsHolder, int newItemIndex) => viewsHolder.views.localScale = Vector3.one;

    public void InsertItemLast<T>(T data) {
        _dataList ??= new List<T>();
        
        _dataList.Add(data);
        ResetItems(_dataList.Count);
    }

    public void InsertItemsLast<T>(List<T> dataList) {
        _dataList ??= new List<T>();
        
        _dataList.AddRange(dataList);
        ResetItems(_dataList.Count);
    }
    
    public void InsertItem<T>(int index, T data, bool isMove = false) {
        _dataList ??= new List<T>();

        _dataList.Insert(index, data);
        ResetItems(_dataList.Count);
    }

    public override void InsertItemWithViewsHolder(CellGroupViewsHolder<Sample_GridItemViewsHolder> viewsHolder, int atIndex, bool contentPanelEndEdgeStationary) {
        _groupHolderList.Insert(atIndex, viewsHolder);
        base.InsertItemWithViewsHolder(viewsHolder, atIndex, contentPanelEndEdgeStationary);
    }

    public void RemoveItem(int index) {
        if (_dataList == null) {
            Logger.TraceError($"{nameof(_dataList)} is Null");
            return;
        }

        if (index < 0 || index >= _dataList.Count) {
            Logger.TraceError($"{nameof(index)} Out Range || {index}");
            return;
        }

        _dataList.RemoveAt(index);

        ResetItems(_dataList.Count);
    }

    public override void RemoveItemWithViewsHolder(CellGroupViewsHolder<Sample_GridItemViewsHolder> viewsHolder, bool stealViewsHolderInsteadOfRecycle, bool contentPanelEndEdgeStationary) {
        _groupHolderList.Remove(viewsHolder);
        base.RemoveItemWithViewsHolder(viewsHolder, stealViewsHolderInsteadOfRecycle, contentPanelEndEdgeStationary);
    }

    public override void ChangeItemsCount(ItemCountChangeMode changeMode, int cellsCount, int indexIfAppendingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false) {
        base.ChangeItemsCount(changeMode, cellsCount, indexIfAppendingOrRemoving, contentPanelEndEdgeStationary, keepVelocity);
        SwitchFitDrag();
        SwitchDynamicGravity();
        SwitchDynamicAlignment();
    }

    public void UpdateItem(int index) {
        var holder = GetCellViewsHolderIfVisible(GetCollectionIndex(index));
        if (holder != null) {
            UpdateCellViewsHolder(holder);
        }
    }
    
    public void UpdateItem(int index, object info) {
        if (_dataList == null) {
            return;
        }
        
        index = GetCollectionIndex(index);
        if (_dataList.Count > index) {
            _dataList[index] = info;
            UpdateItem(index);
        }
    }

    public void UpdateItem<T>(int index, Func<T, T> updateFunc) {
        if (_dataList == null ||  updateFunc == null) {
            Logger.TraceError($"{nameof(_dataList)} or {nameof(updateFunc)} is Null");
            return;
        }
        
        index = GetCollectionIndex(index);
        if (TryGetCastInfoList<T>(out var castList) && castList.Count > index) {
            castList[index] = updateFunc.Invoke(castList[index]);
            UpdateItem(index);
        }
    }

    public void SwitchFitDrag() {
        if (_Params.Config.IsDynamicFitScroll) {
            if (GetGroupCount() > 0) {
                if (IsFitScrollView() == false) {
                    SetEnableDrag(true);
                    return;
                }
            }
            
            SetEnableDrag(false);
        } else if (_Params.Config.IsFitScroll) {
            SetEnableDrag(_Params.GetCollectionCellItemSize() * GetGroupCount() <= _InternalState.vpSize == false);
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

    private void SwitchDynamicAlignment() {
        if (_Params.Config.IsDynamicAlignment) {
            var targetAlignment = GetGroupCount() switch {
                > 1 => _Params.Config.MultipleLineAlignment,
                _ => _Params.Config.SingleLineAlignment,
            };

            if (_Params.Grid.AlignmentOfCellsInGroup != targetAlignment) {
                _Params.Grid.AlignmentOfCellsInGroup = targetAlignment;
                _Params.InitIfNeeded(this);
                _groupHolderList.ForEach(x => {
                    if (x.root.TryGetComponent<LayoutGroup>(out var layout)) {
                        layout.childAlignment = targetAlignment;
                    }
                });
            }
        }
    }

    private bool IsFitScrollView() {
        var vpSize = GetCollectionVpSize();
        var addItemSize = GetCollectionItemSize();
        var visibleSize = 0f;
        for (var i = 0; i < GetGroupCount(); i++) {
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
        if (_groupHolderList.Count > 0) {
            var groupHolder = _groupHolderList.FirstOrDefault();
            var cellHolder = groupHolder?.ContainingCellViewsHolders.FirstOrDefault();
            if (cellHolder != null && cellHolder.TryGetRectTransform(out var rectTransform)) {
                var rect = rectTransform.rect;
                return _Params.Orientation switch {
                    BaseParams.OrientationEnum.HORIZONTAL => rect.width + _Params.ContentSpacing,
                    BaseParams.OrientationEnum.VERTICAL => rect.height + _Params.ContentSpacing,
                    _ => _Params.GetCollectionCellItemSize(),
                };
            }
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
        if (_dataList is not { Count: > 0 }) {
            return;
        }
        
        SetSelectItemIndex(index);
        
        var isVisible = false;
        foreach (var group in _groupHolderList) {
            foreach (var holder in group.ContainingCellViewsHolders) {
                if (holder == null) {
                    return;
                }
                
                SelectItem(holder);
                if (holder.ItemIndex == _selectItemIndex) {
                    isVisible = true;
                }
            }
        }

        if (isVisible == false && _dataList != null && _dataList.Count > _selectItemIndex) {
            _onSelectCallback?.Invoke(_selectItemIndex, _dataList[_selectItemIndex]);
        }
    }

    public void SelectItem<T>(Predicate<T> match) {
        if (_dataList == null) {
            return;
        }

        if (TryGetCastInfoList<T>(out var castList) && castList.TryFindIndex(match, out var index)) {
            SelectItem(index);
        }
    }

    // TODO. 주석 처리 후 테스트 필요. 가능한 경우 위 SelectItem과 통합
    public void SelectVisibleItem(int index) {
        if (_dataList == null || _dataList.Count <= 0) {
            return;
        }
        
        SetSelectItemIndex(index);
        _groupHolderList.ForEach(x => {
            foreach (var holder in x.ContainingCellViewsHolders) {
                if (holder == null) {
                    return;
                }
                
                SelectItem(holder);
            }
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

    public void MoveTo(int index, float duration = 1f, Action onDone = null) {
        if (GetDataCount() <= 0) {
            onDone?.Invoke();
            return;
        }
        
        index = GetCollectionIndex(index);
        if (IsInitialized == false) {
            Observable.EveryLateUpdate().TakeWhile(_ => IsInitialized == false).First().Subscribe(_ => SmoothScrollTo(index, duration, onDone: onDone));
        } else {
            SmoothScrollTo(index, duration, onDone: onDone);
        }
    }

    public void MoveTo<T>(Predicate<T> match, int collectionValue, float duration = 1f, Action onDone = null) {
        var index = GetCastInfoList<T>()?.FindIndex(x => match?.Invoke(x) ?? false) ?? 0;
        MoveTo(index + collectionValue, duration, onDone);
    }

    public void MoveToSelect(float duration = 1f, Action onDone = null) {
        if (_selectItemIndex > -1) {
            MoveTo(_selectItemIndex, duration, onDone);
        }
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
        var index = GetCastInfoList<T>()?.FindIndex(x => match?.Invoke(x) ?? false) ?? 0;
        MoveToWithPadding(index + collectionValue, duration, onDone);
    }

    public void MoveToSelectWithPadding(float duration = 1f, Action onDone = null) {
        if (_selectItemIndex > -1) {
            MoveToWithPadding(_selectItemIndex, duration, onDone);
        }
    }
    
    public float GetPaddingCollectionOffset() {
        if (_Params == null || _InternalState == null) {
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

        if (_dataList is List<T> castList) {
            return castList.FindIndex(x => match?.Invoke(x) ?? false);
        }

        return -1;
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

    public void RebuildScrollViewSize() => RebuildLayoutDueToScrollViewSizeChange();
    private int GetCollectionIndex(int index) => Math.Clamp(index, 0, Math.Max(0, GetDataCount() - 1));
    public int GetGroupCount() => _ItemsDesc?.itemsCount ?? 0;
    public int GetDataCount() => GetItemsCount();
    public int GetPaddingCollectIndex() => _paddingCollectIndex;
    public RectTransform GetRect() => _rect ??= transform.GetRectTransform();
    public RectTransform GetContent() => _content != null ? _content : _content = GameObjectUtil.FindComponent<RectTransform>(gameObject, "Content");
    public RectTransform GetViewPort() => _viewPort != null ? _viewPort : _viewPort = GameObjectUtil.FindComponent<RectTransform>(gameObject, "Viewport");
    public IScrollViewConfig GetConfig() => _Params?.Config;
    public void SetActive(bool isActive) => gameObject.SetActive(isActive);
}

[Serializable]
public class Sample_GridScrollViewParams : GridParams, IScrollViewParam {

    private Vector2 _collectItemSize;
    private GameObject _itemPrefab;

    public override void InitIfNeeded(IOSA iAdapter) {
        base.InitIfNeeded(iAdapter);
        if (iAdapter.AsMonoBehaviour.transform.TryGetRectTransform(out var rect)) {
            var scrollViewSize = rect.sizeDelta;
            var fitGroupSize = GetFitGroupItemSize();
            var collectPaddingSize = Orientation switch {
                OrientationEnum.VERTICAL => (int)(scrollViewSize.x - fitGroupSize.x) / 2,
                OrientationEnum.HORIZONTAL => (int)(scrollViewSize.y - fitGroupSize.y) / 2,
                _ => 0,
            };

            if (collectPaddingSize > 0) {
                switch (Orientation) {
                    case OrientationEnum.VERTICAL:
                        ContentPadding.right = collectPaddingSize;
                        ContentPadding.left = collectPaddingSize;
                        break;
                    case OrientationEnum.HORIZONTAL:
                        ContentPadding.top = collectPaddingSize;
                        ContentPadding.bottom = collectPaddingSize;
                        break;
                }
            }
        }
    }

    public void SetPrefab(GameObject itemPrefab) {
        if (itemPrefab == null) {
            Logger.TraceError($"{nameof(itemPrefab)} is Null");
            return;
        }
        
        _itemPrefab = itemPrefab;
        if (itemPrefab.TryGetComponent<RectTransform>(out var rectTransform)) {
            if (itemPrefab.TryGetComponent<LayoutElement>(out var element) == false) {
                var rect = rectTransform.rect;
                element = itemPrefab.AddComponent<LayoutElement>();
                element.minHeight = rect.height;
                element.minWidth = rect.width;
            }
            
            _collectItemSize = new Vector2(element.minWidth, element.minHeight);
            Grid.CellPrefab = rectTransform;
        } else {
            Logger.TraceError($"{nameof(itemPrefab)} is Missing {nameof(RectTransform)}");
        }
    }
    
    public Vector2 GetFitGroupItemSize() {
        var orientationItemSize = Orientation switch {
            OrientationEnum.VERTICAL => _collectItemSize.x,
            OrientationEnum.HORIZONTAL => _collectItemSize.y,
            _ => DefaultItemSize,
        };
        
        var size = CurrentUsedNumCellsPerGroup * orientationItemSize + (CurrentUsedNumCellsPerGroup - 1) * Grid.SpacingInGroup;
        return Orientation switch {
            OrientationEnum.VERTICAL => new Vector2(size + Grid.GroupPadding.left + Grid.GroupPadding.right, _collectItemSize.y),
            OrientationEnum.HORIZONTAL => new Vector2(_collectItemSize.x, size + Grid.GroupPadding.top + Grid.GroupPadding.bottom),
            _ => _collectItemSize,
        };
    }
    
    public float GetCollectionCellItemSize() => Orientation switch {
        OrientationEnum.HORIZONTAL => DefaultItemSize + Grid.GroupPadding.left + Grid.GroupPadding.right + ContentSpacing,
        OrientationEnum.VERTICAL => DefaultItemSize + Grid.GroupPadding.top + Grid.GroupPadding.bottom + ContentSpacing,
        _ => DefaultItemSize,
    };

    #region Config
    
    [SerializeField]
    private UIGridConfig _uiConfig = new();
    public UIGridConfig Config { get => _uiConfig; set => _uiConfig = value; }

    #endregion
}

public class Sample_GridItemViewsHolder : CellViewsHolder, IScrollViewsItemHolder {

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
    public bool IsVisible() => _item != null && _item.isActiveAndEnabled && views.gameObject.activeSelf;
    public bool IsMatchIndex(int index) => ItemIndex == index;

    public bool TryGetRectTransform(out RectTransform rectTransform) {
        rectTransform = GetRectTransform();
        return rectTransform != null;
    }
    
    public RectTransform GetRectTransform() => _rectTransform;
    public int GetItemIndex() => ItemIndex;
}
*/
