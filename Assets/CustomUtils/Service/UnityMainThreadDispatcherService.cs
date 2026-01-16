using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

[Service(DEFAULT_SERVICE_TYPE.START_MAIN_THREAD)]
public class UnityMainThreadDispatcherService : IService {
    
    private int _mainThreadId;
    
    private MainThreadDispatcherObject _threadObject;
    private SynchronizationContext _context;

    private bool _isServing;

    bool IService.IsServing() => _isServing;

    void IService.Init() {
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;

        if (_threadObject) {
            Object.Destroy(_threadObject);
        }
        
        _threadObject = MainThreadDispatcherObject.Create();
        if (_threadObject) {
            Object.DontDestroyOnLoad(_threadObject);
        } else {
            Logger.TraceError($"{nameof(_threadObject)} is Null");
        }
        
        _context = SynchronizationContext.Current;
    }

    void IService.Start() => _threadObject?.Run();
    void IService.Stop() => _threadObject?.Stop();
    void IService.Remove() => Object.Destroy(_threadObject);

    public void Enqueue(Action action, THREAD_TYPE type) {
        switch (type) {
            case THREAD_TYPE.UNITY:
                _threadObject.Enqueue(action);
                break;
            case THREAD_TYPE.CONTEXT:
                Enqueue(action);
                break;
        }
    }

    public void Enqueue(Action action) => _context.Post(_ => action.Invoke(), null);
    public void Enqueue(IEnumerator enumerator) => _threadObject.Enqueue(enumerator);

    public bool IsMainThread() => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
    
    private class MainThreadDispatcherObject : MonoBehaviour {
        
        private readonly Queue<Action> _workQueue = new();
        private readonly Queue<IEnumerator> _coroutineQueue = new();

        private bool _isRunning;

        public static MainThreadDispatcherObject Create() => new GameObject(nameof(MainThreadDispatcherObject)) { hideFlags = HideFlags.HideAndDontSave }.AddComponent<MainThreadDispatcherObject>();

        private void OnDestroy() {
            _workQueue.Clear();
            
            StopAllCoroutines();
            _coroutineQueue.Clear();
        }

        private void Update() {
            if (_isRunning) {
                if (_workQueue.TryDequeue(out var action)) {
                    action?.Invoke();
                }

                if (_coroutineQueue.TryDequeue(out var enumerator)) {
                    StartCoroutine(enumerator);
                }
            }
        }

        public void Run() => _isRunning = true;
        public void Stop() => _isRunning = false;

        public void Enqueue(Action action) => _workQueue.Enqueue(action);
        public void Enqueue(IEnumerator enumerator) => _coroutineQueue.Enqueue(enumerator);
    }
}

public enum THREAD_TYPE {
    NONE,
    UNITY,
    CONTEXT,
}