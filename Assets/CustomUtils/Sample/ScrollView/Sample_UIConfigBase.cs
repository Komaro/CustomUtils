// This code will not work without the OSA(Optimized ScrollView Adapter) Asset.
// https://assetstore.unity.com/packages/tools/gui/optimized-scrollview-adapter-68436

/*
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Sample_UIConfigBase : IScrollViewConfig {

    [Tooltip("DefaultItemSize Value 크기로 계산(부정확함). DynamicFitScroll 보다 우선순위가 낮음.")]
    [SerializeField] [FormerlySerializedAs("FitScroll")]
    private bool _fitScroll = true;
    public bool IsFitScroll { get => _fitScroll; set => _fitScroll = value; }

    [Tooltip("Item RectTransform 크기로 계산. FitScroll 보다 우선순위가 높음")]
    [SerializeField] [FormerlySerializedAs("DynamicFitScroll")]
    private bool _dynamicFitScroll = true;
    public bool IsDynamicFitScroll { get => _dynamicFitScroll; set => _dynamicFitScroll = value; }
    
    [Tooltip("다중 Select를 허용.")]
    [SerializeField] [FormerlySerializedAs("MultipleSelect")]
    private bool _isMultipleSelect = false;
    public bool IsMultipleSelect { get => _isMultipleSelect; set => _isMultipleSelect = value; }

    private HashSet<int> _selectSet = new();
    public HashSet<int> SelectSet => _selectSet;

    [Tooltip("현재 보여지는 아이템이 스크롤 뷰의 크기보다 큰 경우 Gravity 값을 전환.")]
    [SerializeField] [FormerlySerializedAs("DynamicGravity")]
    private bool _isDynamicGravity = false;
    public bool IsDynamicGravity { get => _isDynamicGravity; set => _isDynamicGravity = value; }
    
    [Tooltip("Gravity가 변경되는 타입.")]
    [SerializeField] [FormerlySerializedAs("DynamicGravityType")] [UIConfigBoolCheckFold("DynamicGravity")]
    private E_DYNAMIC_GRAVITY_TYPE _dynamicGravityType;
    public E_DYNAMIC_GRAVITY_TYPE DynamicGravityType { get => _dynamicGravityType; set => _dynamicGravityType = value; }
}

[Serializable]
public class UIConfig : Sample_UIConfigBase {
    
    [Tooltip("UpdateViewsHolder 시에 ScheduleComputeVisibilityTwinPass 을 허용함. SelectItem 이 정상 동작하기 위해선 해당 옵션 체크 해제")]
    [SerializeField] [FormerlySerializedAs("ScheduleCompute")]
    private bool _isScheduleCompute;
    public bool IsScheduleCompute { get => _isScheduleCompute; set => _isScheduleCompute = value; }
}

[Serializable]
public class UIGridConfig : Sample_UIConfigBase {

    [Tooltip("현재 보여지는 Group의 갯수가 하나일 때와 그 보다 많은 경우 정렬 처리를 동적으로 처리함.")]
    [SerializeField] [FormerlySerializedAs("DynamicAlignment")]
    private bool _isDynamicAlignment;
    public bool IsDynamicAlignment { get => _isDynamicAlignment; set => _isDynamicAlignment = value; }
    
    [Tooltip("그룹이 하나인 경우의 정렬.")]
    [SerializeField] [FormerlySerializedAs("SingleLineAlignment")] [UIConfigBoolCheckFold("DynamicAlignment")]
    private TextAnchor _singleLineAlignment;
    public TextAnchor SingleLineAlignment { get => _singleLineAlignment; set => _singleLineAlignment = value; }

    [Tooltip("그룹이 하나보다 많은 경우의 정렬.")]
    [SerializeField] [FormerlySerializedAs("MultipleLineAlignment")] [UIConfigBoolCheckFold("DynamicAlignment")]
    private TextAnchor _multipleLineAlignment;
    public TextAnchor MultipleLineAlignment { get => _multipleLineAlignment; set => _multipleLineAlignment = value; }
}

public enum E_DYNAMIC_GRAVITY_TYPE {
    START_TO_CENTER,
    START_TO_END,

    CENTER_TO_START,
    CENTER_TO_END,
    
    END_TO_START,
    END_TO_CENTER,
}*/
