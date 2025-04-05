
using System;
using System.Collections.Generic;
using UnityEngine;

[TestRequired]
public class SchedulerService : IService {

    private UpdateObject _updateObject;
    
    private readonly List<SchedulerTask> _taskList = new();

    void IService.Init() {
        _updateObject = Service.GetService<MonoUpdateService>().Get();
        _updateObject.ThrowIfNull(nameof(_updateObject));
    }
    
    void IService.Start() {
        _updateObject.AttachUpdate(OnUpdate);
        _updateObject.StartUpdate();
    }

    void IService.Stop() => _updateObject.StopUpdate();

    void IService.Remove() {
        _taskList.Clear();
        Service.GetService<MonoUpdateService>().Release(_updateObject);
    }

    public void AttachTask(float delay, Action callback) {
        callback.ThrowIfNull(nameof(callback));
        _taskList.Add(new SchedulerTask {
            delay = delay,
            callback = callback
        });
    }

    private void OnUpdate() {
        var now = Time.time;
        foreach (var task in _taskList) {
            if (now >= task.startTime + task.delay) {
                task.callback.Invoke();
                task.cancel = true;
            }
        }

        _taskList.RemoveAll(task => task.cancel);
    }
    
    private record SchedulerTask {

        public readonly float startTime = Time.time;
        public float delay;
        public bool cancel;
        public Action callback;
    }
}


