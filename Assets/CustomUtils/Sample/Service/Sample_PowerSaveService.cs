using System;
using System.Collections;
using UnityEngine;
using UniRx;

[Service(DEFAULT_SERVICE_TYPE.PLAY_DURING, DEFAULT_SERVICE_TYPE.PLAY_FOCUS_DURING)]
public class Sample_PowerSaveService : IService {

    private float _waitTime;

    private IObservable<float> _serviceStream;
    private IDisposable _serviceDisposable;

    private bool _isServing;

    public bool IsServing() => _isServing;
    public void Init() => _serviceStream = Observable.FromMicroCoroutine<float>(TouchCoroutine).Where(_ => Application.isPlaying);

    public void Start() {
        _waitTime = 300f;   // Input Local Save Value
        if (_waitTime <= 0) {
            return;
        }
        
        _serviceDisposable = _serviceStream.Subscribe(_ => {
            try {
                OpenPowerSaveMode();
            } catch (Exception ex) {
                Logger.TraceError(ex);
                Service.RestartService<Sample_PowerSaveService>();
            }
        });

        _isServing = true;
    }
    
    public void Stop() {
        _serviceDisposable?.Dispose();
        _isServing = false;
    }

    private IEnumerator TouchCoroutine(IObserver<float> observer) {
        var deltaTime = 0f;
        while (true) {
            if (Input.anyKey) {
                deltaTime = 0f;
            }
            
            deltaTime += Time.unscaledDeltaTime;
            if (deltaTime >= _waitTime && IsActivePowerSaveMode() == false) {
                if (IsInvalidActive()) {
                    deltaTime = 0f;
                    continue;
                }
                
                observer.OnNext(deltaTime);
            }
            
            yield return null;
        }
    }
    
    public void OpenPowerSaveMode() {
        if (IsActivePowerSaveMode() == false) {
            // TODO. Run Power Save Mode
        }
    }
    
    private bool IsActivePowerSaveMode() {
        // TODO. Check Run Power SaveMode
        return false;
    }

    private bool IsInvalidActive() {
        // TODO. Check Valid Condition
        return true;
    }
}
