
using System;
using System.Collections.Generic;
using UnityEngine;

public class SchdulerTask {

    public float startTime;
    public float delay;
    public bool cancel;
    public Action callback;
}

public class SchedulerService : IService {

    private SchedulerObject _schedulerObject;

    void IService.Init() {
        _schedulerObject ??= new GameObject(nameof(SchedulerService)).GetOrAddComponent<SchedulerObject>();
        _schedulerObject.ThrowIfUnexpectedNull(nameof(_schedulerObject));
    }
    
    void IService.Start() {
    

    }

    void IService.Stop() {
        
        
    }

    public void Schedule(float delay, Action callback) {
        _schedulerObject.Attach(new SchdulerTask {
            startTime = Time.time,
            delay = delay,
            callback = callback
        });
    }

    private class SchedulerObject : MonoBehaviour, IDisposable {

        private List<SchdulerTask> _taskList = new();

        private void Awake() => hideFlags = HideFlags.HideAndDontSave;

        private void Update() {
            var now = Time.time;
            foreach (var task in _taskList) {
                if (now >= task.startTime + task.delay) {
                    task.callback?.Invoke();
                    task.cancel = true;
                }
            }

            _taskList.RemoveAll(task => task.cancel);
        }

        public void Dispose() => _taskList.Clear();

        public void Attach(SchdulerTask task) => _taskList.Add(task);
    }
}