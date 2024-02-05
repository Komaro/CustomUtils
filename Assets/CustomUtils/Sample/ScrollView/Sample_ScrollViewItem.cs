// This code will not work without the OSA(Optimized ScrollView Adapter) Asset.
// https://assetstore.unity.com/packages/tools/gui/optimized-scrollview-adapter-68436

/*
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using UnityEngine;

/// <summary>
/// GridView 용 Item 생성시 Views 오브젝트가 반드시 존재하여야 함.
/// </summary>
/// <see cref="CellViewsHolder">참고</see>
[DisallowMultipleComponent]
public abstract class Sample_ScrollViewItem : MonoBehaviour, IScrollViewItem {

    protected IScrollView scrollView;
    protected AbstractViewsHolder holder;
    
    public virtual void Init(AbstractViewsHolder holder) {
        scrollView = GetComponentInParent<IScrollView>(true);
        this.holder = holder;
        _Init();
    }
    
    /// <summary>
    /// 아이템 내부 컴포넌트 초기화
    /// </summary>
    public abstract void _Init();
    /// <summary>
    /// 업데이트 데이터 처리
    /// </summary>
    /// <param name="infoObject">업데이트에 필요한 데이터 오브젝트. 자유롭게 구현</param>
    public abstract void SetData(object infoObject);
    /// <summary>
    /// 현재 Info에 연동된 Item 중 Select 나머지는 Deselect 한다.
    /// </summary>
    public void Select() => scrollView.SelectItem(holder.ItemIndex);
    /// <summary>
    /// 현재 View 상에서 보이는 Item 중 Select 나머지는 Deselect 한다.
    /// </summary>
    public void SelectVisible() => scrollView.SelectVisibleItem(this);
    /// <summary>
    /// 각 Item별 공통 Select 구현 (중복)
    /// </summary>
    public virtual void OnSelect() { Logger.TraceWarning($"{nameof(OnSelect)} not override"); }
    /// <summary>
    /// 각 Item별 공통 Select 구현
    /// </summary>
    /// <param name="isSelected"></param>
    public virtual void OnSelect(bool isSelected) { Logger.TraceWarning($"{nameof(OnSelect)} not override"); }
    /// <summary>
    /// 현재 Item이 Select Item 인지 확인
    /// </summary>
    protected bool IsSelectItem() => scrollView.IsSelectItem(this);
    /// <summary>
    /// 현재 Item의 데이터 Index
    /// </summary>
    /// <returns></returns>
    public int GetIndex() => holder.ItemIndex;
    /// <summary>
    /// Scroll View Clear 시 각 Item 공통 처리
    /// </summary>
    public virtual void OnClear() { }
}
*/
