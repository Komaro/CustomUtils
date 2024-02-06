using System;
using UnityEngine;
using System.Linq;
using UniRx;

[ServiceAttribute(SERVICE_TYPE.NONE)]
public class TimeUpdateService : IService {

    private IObservable<long> _updateStream;
    private IDisposable _updateDisposable;
    
    public delegate void UpdateHandler(float tick);
    private event UpdateHandler OnUpdateHandler;
    public event UpdateHandler OnUpdate {
        add {
            if (OnUpdateHandler == null || OnUpdateHandler.GetInvocationList().Contains(value) == false) 
                OnUpdateHandler += value;
        }

        remove => OnUpdateHandler -= value;
    }

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

    public void Remove() {
        if (OnUpdateHandler != null) {
            foreach (var bindEvent in OnUpdateHandler.GetInvocationList()) {
                OnUpdate -= (UpdateHandler)bindEvent;
            }
        }
    }
}