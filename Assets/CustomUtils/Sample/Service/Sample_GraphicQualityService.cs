using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;

[Service(DEFAULT_SERVICE_TYPE.PLAY_DURING_AFTER_INIT)]
public class Sample_GraphicQualityService : IService {

    private ReactiveProperty<SAMPLE_GRAPHIC_QUALITY_TYPE> _graphicQualityType = new ();
    private ReactiveProperty<SAMPLE_MAX_FRAME_TYPE> _maxFrameType = new();

    private ReactiveProperty<int> _renderInterval = new ();

    private List<IDisposable> _disposableList = new();

    private Dictionary<SAMPLE_GRAPHIC_QUALITY_TYPE, int> _qualityDic = new();

    void IService.Init() {
        // Input Local Save Value
        SetGraphicQualityType(SAMPLE_GRAPHIC_QUALITY_TYPE.HIGH); 
        SetMaxFrameType(SAMPLE_MAX_FRAME_TYPE.FRAME_60);
        SetRenderInterval(1);
        //
        
        QualitySettings.names.IndexForEach((x, index) => {
            if (EnumUtil.TryConvertAllCase<SAMPLE_GRAPHIC_QUALITY_TYPE>(x, out var type)) {
                _qualityDic.AutoAdd(type, index);
            }
        });
    }

    void IService.Start() {
        SwitchGraphicQuality(_graphicQualityType.Value);
        _disposableList.Add(_graphicQualityType.Subscribe(SwitchGraphicQuality));
        
        SwitchMaxFrame(_maxFrameType.Value);
        _disposableList.Add(_maxFrameType.Subscribe(SwitchMaxFrame));
        
        SwitchRenderInterval(_renderInterval.Value);
        _disposableList.Add(_renderInterval.Subscribe(SwitchRenderInterval));
    }

    void IService.Stop() {
        OnDemandRendering.renderFrameInterval = 1;
        _disposableList.ForEach(x => x.Dispose());
        _disposableList.Clear();
    }

    #region Set

    public void SetGraphicQualityType(SAMPLE_GRAPHIC_QUALITY_TYPE type) => _graphicQualityType.Value = type;
    public void SetMaxFrameType(SAMPLE_MAX_FRAME_TYPE type) => _maxFrameType.Value = type;
    public void SetRenderInterval(int interval) => _renderInterval.Value = interval;
    
    #endregion

    #region Switch

    private void SwitchGraphicQuality(SAMPLE_GRAPHIC_QUALITY_TYPE type) => SwitchGraphicRenderPipelineOption(type);

    private void SwitchGraphicRenderPipelineOption(SAMPLE_GRAPHIC_QUALITY_TYPE type) {
        if (_qualityDic.TryGetValue(type, out var qualityLevel) == false) {
            return;
        }

        if (QualitySettings.GetQualityLevel() == qualityLevel) {
            return;
        }
        
        QualitySettings.SetQualityLevel(qualityLevel);
    }
    
    private void SwitchMaxFrame(SAMPLE_MAX_FRAME_TYPE type) => Application.targetFrameRate = (int) type;

    private void SwitchRenderInterval(int interval) {
        if (_maxFrameType.Value == SAMPLE_MAX_FRAME_TYPE.FRAME_30 && interval > 1) {
            interval /= 2;
        }

        OnDemandRendering.renderFrameInterval = interval;
    }
    
    #endregion
}

public enum SAMPLE_GRAPHIC_QUALITY_TYPE {
    LOW,
    MID,
    HIGH,
}

public enum SAMPLE_MAX_FRAME_TYPE {
    FRAME_30 = 30,
    FRAME_60 = 60,
}