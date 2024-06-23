using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

[Service(DEFAULT_SERVICE_TYPE.INIT_MAIN_THREAD)]
public class UnityMainThreadDispatcherService : IService {

    private int _mainThreadId;
    
    private MainThreadDispatcherObject _threadObject;
    private SynchronizationContext _context;

    private bool _isServing;

    bool IService.IsServing() => _isServing;

    void IService.Init() {
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        
        if (_threadObject != null) {
            Object.Destroy(_threadObject);
        }

        _threadObject = MainThreadDispatcherObject.Create();
        if (_threadObject == null) {
            Logger.TraceError($"{nameof(_threadObject)} is Null");
        }

        _context = SynchronizationContext.Current;
    }

    void IService.Start() { }
    void IService.Stop() { }
    void IService.Remove() => Object.Destroy(_threadObject);

    public void Enqueue(Action action, THREAD_TYPE type) {
        switch (type) {
            case THREAD_TYPE.UNITY:
                _threadObject.Enqueue(action);
                break;
            case THREAD_TYPE.CONTEXT:
                _context.Post(_ => action.Invoke(), null);
                break;
        }
    }

    public void Enqueue(IEnumerator enumerator) => _threadObject.Enqueue(enumerator);

    public bool IsMainThread() => _mainThreadId == Thread.CurrentThread.ManagedThreadId;

    private class MainThreadDispatcherObject : MonoBehaviour, IDisposable {

        private readonly Object _queueLock = new();
        private readonly ConcurrentQueue<Action> _workQueue = new();
        private readonly ConcurrentQueue<IEnumerator> _coroutineQueue = new();

        private bool _isRunning;

        public static MainThreadDispatcherObject Create() {
            var go = new GameObject(nameof(MainThreadDispatcherObject)) {
                hideFlags = HideFlags.HideAndDontSave
            };
            
            DontDestroyOnLoad(go);
            return go.AddComponent<MainThreadDispatcherObject>();
        }

        private void OnDestroy() => Dispose();

        private void Update() {
            lock (_queueLock) {
                if (_isRunning) {
                    if (_workQueue.TryDequeue(out var action)) {
                        action?.Invoke();
                    }

                    if (_coroutineQueue.TryDequeue(out var enumerator)) {
                        StartCoroutine(enumerator);
                    }
                }
            }
        }

        public void Enqueue(Action action) {
            lock (_queueLock) {
                _workQueue.Enqueue(action);
            }
        }

        public void Enqueue(IEnumerator enumerator) {
            lock (_coroutineQueue) {
                _coroutineQueue.Enqueue(enumerator);
            }
        }

        public void Dispose() {
            lock (_queueLock) {
                _workQueue.Clear();
            }

            lock (_coroutineQueue) {
                StopAllCoroutines();
                _coroutineQueue.Clear();
            }
            
            GC.SuppressFinalize(this);
        }
    }
}

public enum THREAD_TYPE {
    NONE,
    UNITY,
    CONTEXT,
}
