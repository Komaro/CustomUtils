using System;
using UnityEngine;
using System.Linq;
using UniRx;

[ServiceAttribute(DEFAULT_SERVICE_TYPE.NONE)]
public class TimeUpdateService : IService {

    private IObservable<long> _updateStream;
    private IDisposable _updateDisposable;
    
    public delegate void UpdateHandler(float tick);
    private event UpdateHandler OnUpdateHandler;
    public SafeDelegate<UpdateHandler> OnUpdate;

    public void Init() {
        _updateStream = Observable.EveryEndOfFrame();
    }

    public void Start() {
        _updateDisposable = _updateStream.Subscribe(_ => {
            OnUpdateHandler?.Invoke(Time.deltaTime);
        });
    }

    public void Stop() {
        _updateDisposable.Dispose();
        _updateDisposable = null;
    }

    public void Remove() => OnUpdate.Clear();
}