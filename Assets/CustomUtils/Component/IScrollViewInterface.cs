using System;
using System.Collections.Generic;
using UnityEngine;

public interface IScrollView {

    void ForceInit();
    void SetItemPrefab(string prefab);
    void SetItemPrefab(GameObject itemPrefab);
    void Clear();
    void SetData<T>(List<T> dataList, bool isResetPosition = false);
    void RefreshVisible();
    void InsertItem<T>(int index, T data, bool isMove = false);
    void RemoveItem(int index);
    void UpdateItem(int index);
    void UpdateItem(int index, object info);
    void UpdateItem<T>(int index, Func<T, T> updateFunc);
    void SwitchFitDrag();
    void SwitchDynamicGravity();
    void SetEnableDrag(bool isEnable);
    public void SetOnSelect(Action<int, object> onSelect);
    public void SetOnVisibleSelect(Action<int, IScrollViewItem> onVisibleSelect);
    void SelectItem(int index);
    void SelectItem<T>(Predicate<T> match);
    void SelectVisibleItem(int index);
    void SelectVisibleItem(IScrollViewItem item);
    bool IsSelectItem(IScrollViewItem item);
    int GetSelectItemIndex();
    void SetSelectItemIndex(int index);
    void MoveTo(int index, float duration, Action onDone);
    void MoveTo<T>(Predicate<T> match, int collectionValue, float duration, Action onDone);
    void MoveToWithPadding(int index, float duration, Action onDone);
    void MoveToWithPadding<T>(Predicate<T> match, int collectionValue, float duration, Action onDone);
    float GetPaddingCollectionOffset();
    int GetIndex(object info);
    int GetIndex<T>(Predicate<T> match);
    RectTransform GetRect();
    RectTransform GetContent();
    RectTransform GetViewPort();
    IScrollViewConfig GetConfig();
    void SetActive(bool isActive);
}

public interface IScrollViewParam {
    float GetCollectionCellItemSize();
}

public interface IScrollViewItem {
    void _Init();
    int GetIndex();
}

public interface IScrollViewsItemHolder {
    void SetData(object ob);
    void Select();
    void Select(bool isSelected);
    void Select(int selectIndex);
    bool IsMathItem(IScrollViewItem item);
    IScrollViewItem GetItem();
    void Clear();
    bool IsVisible();
    RectTransform GetRectTransform();
    bool IsMatchIndex(int index);
    int GetItemIndex();
}

public interface IScrollViewConfig { }
