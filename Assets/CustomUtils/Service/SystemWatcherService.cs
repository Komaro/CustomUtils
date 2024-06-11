using System.Collections.Generic;
using System.IO;
using UnityEngine;

// TODO. 재설계 필요
public class SystemWatcherService : IService {

    private Dictionary<string, FileSystemWatcher> _fileWatcherDic = new();

    private bool _isServing;
    
    public bool IsServing() => _isServing;

    public void Start() {
        foreach (var watcher in _fileWatcherDic.Values) {
            watcher.EnableRaisingEvents = true;
        }

        _isServing = true;
    }

    public void Stop() {
        foreach (var watcher in _fileWatcherDic.Values) {
            watcher.EnableRaisingEvents = false;
        }
        
        _isServing = false;
    }

    public void Remove() => _fileWatcherDic.SafeClear(x => x.Dispose());

    public void AddWatcher(SystemWatcherServiceOrder order) {
        if (order == null || order.Invalid()) {
            Logger.TraceError($"{nameof(order)} is Null or Invalid");
            return;
        }

        if (_fileWatcherDic.TryGetValue(order.CreatePath(), out var watcher) == false) {
            watcher = _fileWatcherDic.GetOrAddValue(order.CreatePath());
            watcher.BeginInit();
            
            watcher.Path = order.path;
            watcher.Filter = order.filter;
            watcher.NotifyFilter = order.filters;
            AddHandler(watcher, order.handler);
            
            watcher.EndInit();
            watcher.EnableRaisingEvents = true;
        } else {
            Logger.TraceLog($"Already {nameof(SystemWatcherServiceOrder)} || {order.path}", Color.yellow);
            watcher.EnableRaisingEvents = true;
        }
    }

    public void StartWatcher(string path) {
        if (_fileWatcherDic.TryGetValue(path, out var watcher)) {
            watcher.EnableRaisingEvents = true;
        }
    }

    public void StopWatcher(SystemWatcherServiceOrder order) {
        if (order.Invalid() == false) {
            StopWatcher(order.CreatePath());
        }
    }
    
    public void StopWatcher(string path) {
        if (_fileWatcherDic.TryGetValue(path, out var watcher)) {
            watcher.EnableRaisingEvents = false;
        }
    }

    public void RemoveWatcher(SystemWatcherServiceOrder order) {
        if (order.Invalid() == false) {
            RemoveWatcher(order.CreatePath());
        }
    }

    public void RemoveWatcher(string path) {
        if (_fileWatcherDic.TryGetValue(path, out var watcher)) {
            watcher.Dispose();
            _fileWatcherDic.Remove(path);
        }
    }
    
    private void AddHandler(FileSystemWatcher watcher, FileSystemEventHandler handler) {
        watcher.Changed -= handler;
        watcher.Changed += handler;
        
        watcher.Created -= handler;
        watcher.Created += handler;

        watcher.Deleted -= handler;
        watcher.Deleted += handler;
    }
}

public record SystemWatcherServiceOrder {
    
    public string path;
    public string filter;
    public NotifyFilters filters;
    public FileSystemEventHandler handler;

    public bool Invalid() {
        if (string.IsNullOrEmpty(path)) {
            return true;
        }
        
        return false;
    }

    public virtual string CreatePath() => $"{path}_{filter}";
}