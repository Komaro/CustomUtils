using System;
using UnityEngine;
using UniRx;

[Service(DEFAULT_SERVICE_TYPE.NONE)]
public class TimeUpdateService : IService {

    private IObservable<long> _updateStream;
    private IDisposable _updateDisposable;
    
    public delegate void UpdateHandler(float tick);
    private event UpdateHandler OnUpdateHandler;
    public SafeDelegate<UpdateHandler> OnUpdate;

    void IService.Init() => _updateStream = Observable.EveryEndOfFrame();

    void IService.Start() {
        _updateDisposable = _updateStream.Subscribe(_ => {
            OnUpdateHandler?.Invoke(Time.deltaTime);
        });
    }

    void IService.Stop() {
        _updateDisposable.Dispose();
        _updateDisposable = null;
    }

    void IService.Remove() => OnUpdate.Clear();
}