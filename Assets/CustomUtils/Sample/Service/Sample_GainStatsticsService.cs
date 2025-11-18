using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

[Service(DEFAULT_SERVICE_TYPE.PLAY_DURING_AFTER_INIT)]
[Obsolete]
public class Sample_GainStatsticsService : IService {

    private float _recordingTick;

    private ReactiveDictionary<(Enum type, string id), double> _gainHistoryDic = new();

    private bool _isServing;

    private IObservable<DictionaryAddEvent<(Enum type, string id), double>> _addGainStream;
    private IObservable<DictionaryReplaceEvent<(Enum type, string id), double>> _replaceGainStream;
    private IObservable<Unit> _resetGainStream;

    private List<IDisposable> _disposableList = new();
    private IDisposable _recordingDisposable;

    public delegate void UpdateGainHandler(Enum type, string id, double count);
    public SafeDelegate<UpdateGainHandler> OnUpdateGain;

    public delegate void ResetGainHandler();
    public SafeDelegate<ResetGainHandler> OnResetGain;

    public delegate void UpdateTickHandler(float tick);
    public SafeDelegate<UpdateTickHandler> OnUpdateTick;

    bool IService.IsServing() => _isServing;

    void IService.Init() {
        _addGainStream = _gainHistoryDic.ObserveAdd();
        _replaceGainStream = _gainHistoryDic.ObserveReplace();
        _resetGainStream = _gainHistoryDic.ObserveReset();
    }

    void IService.Start() {
        _disposableList.Add(_addGainStream.Subscribe(addEvent => OnUpdateGain.Handler?.Invoke(addEvent.Key.type, addEvent.Key.id, addEvent.Value)));
        _disposableList.Add(_replaceGainStream.Subscribe(replaceEvent => OnUpdateGain.Handler?.Invoke(replaceEvent.Key.type, replaceEvent.Key.id, replaceEvent.NewValue)));
        _disposableList.Add(_resetGainStream.Subscribe(_ => OnResetGain.Handler?.Invoke()));

        _recordingDisposable = Observable.EveryLateUpdate().Where(_ => true).Subscribe(_ => {
            try {
                _recordingTick += Time.deltaTime;
                if (_recordingTick >= 86400f) {
                    ResetHistory();
                }
            } catch (OverflowException ex) {
                Logger.Warning(ex);
                ResetHistory();
            }
            finally {
                OnUpdateTick.Handler?.Invoke(_recordingTick);
            }
        }, onError: _ => {
            Service.RestartService<Sample_GainStatsticsService>();
        });

        _isServing = true;
    }

    void IService.Stop() {
        if (_disposableList != null) {
            _disposableList.ForEach(x => x.Dispose());
            _disposableList.Clear();
        }

        _recordingDisposable?.Dispose();

        OnUpdateGain.Clear();
        OnResetGain.Clear();
        OnUpdateTick.Clear();

        _isServing = false;
    }

    public void AddInfo(AssetInfo info) {
        if (info == null) {
            Logger.TraceError($"{nameof(info)} is Null");
            return;
        }

        try {
            _gainHistoryDic.AutoAccumulateAdd((info.type, info.id), info.count);
        } catch (OverflowException ex) {
            Logger.Warning($"{info.type} || {info.id}");
            Logger.Warning(ex);
            _gainHistoryDic[(info.type, info.id)] = double.MaxValue;
        }
    }

    public List<(Enum type, string id, double count)> GetHistory() => _gainHistoryDic.Select(x => (x.Key.type, x.Key.id, x.Value)).ToList();

    public void ResetHistory() {
        _recordingTick = 0;
        _gainHistoryDic.Clear();
    }

    public float GetRecordingTick() => _recordingTick;

    public class AssetInfo {
        
        public Enum type;
        public string id;
        public long count;

        public AssetInfo(Enum type, string id, long count) {
            this.type = type;
            this.id = id;
            this.count = count;
        }
    }
}