using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SystemWatcherService : IService {

    private Dictionary<SystemWatcherServiceOrder, FileSystemWatcher> _fileSystemWatcherDic = new(new SystemWatcherServiceOrder());

    private bool _isServing;
    
    public bool IsServing() => _isServing;

    public void Start() {
        foreach (var watcher in _fileSystemWatcherDic.Values) {
            watcher.EnableRaisingEvents = true;
        }

        _isServing = true;
    }

    public void Stop() {
        foreach (var watcher in _fileSystemWatcherDic.Values) {
            watcher.EnableRaisingEvents = false;
        }
        
        _isServing = false;
    }

    public void Remove() => _fileSystemWatcherDic.SafeClear(watcher => watcher.Dispose());

    public SystemWatcherServiceOrder StartWatcher(SystemWatcherServiceOrder order) {
        if (order == null || order.Invalid()) {
            Logger.TraceError($"{nameof(order)} is Null or Invalid");
            return order;
        }

        if (_fileSystemWatcherDic.TryGetValue(order, out var watcher) && watcher.EnableRaisingEvents) {
            _fileSystemWatcherDic.SafeRemove(order, x => {
                x.EnableRaisingEvents = false;
                x.Dispose();
            });
        }

        watcher = _fileSystemWatcherDic.GetOrAddValue(order);
        watcher.BeginInit();
            
        watcher.Path = order.path;
        watcher.Filter = order.filter;
        watcher.NotifyFilter = order.filters;

        AddHandler(watcher, order.handler);
        AddHandler(watcher, order.errorHandler);
        AddHandler(watcher, order.renamedHandler);
            
        watcher.EndInit();
        watcher.EnableRaisingEvents = true;

        return order;
    }

    public SystemWatcherServiceOrder StopWatcher(SystemWatcherServiceOrder order) {
        if (order?.Invalid() == false && _fileSystemWatcherDic.TryGetValue(order, out var watcher)) {
            watcher.EnableRaisingEvents = false;
        }
        
        return order;
    }
    
    public void RemoveWatcher(SystemWatcherServiceOrder order) {
        if (order?.Invalid() == false) {
            _fileSystemWatcherDic.SafeRemove(order, watcher => {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            });
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

    private void AddHandler(FileSystemWatcher watcher, ErrorEventHandler handler) {
        watcher.Error -= handler;
        watcher.Error += handler;
    }

    private void AddHandler(FileSystemWatcher watcher, RenamedEventHandler handler) {
        watcher.Renamed -= handler;
        watcher.Renamed += handler;
    }
}

public record SystemWatcherServiceOrder : IEqualityComparer<SystemWatcherServiceOrder> {
    
    public string path;
    public string filter;
    public NotifyFilters filters = NotifyFilters.LastWrite | NotifyFilters.FileName;
    
    public FileSystemEventHandler handler;
    public ErrorEventHandler errorHandler;
    public RenamedEventHandler renamedHandler;

    public SystemWatcherServiceOrder() { }

    public SystemWatcherServiceOrder(string path, FileSystemEventHandler handler) {
        this.path = path;
        this.handler = handler;
    }
    
    public SystemWatcherServiceOrder(string path, string filter, FileSystemEventHandler handler) : this(path, handler) => this.filter = filter;

    public bool Invalid() {
        if (string.IsNullOrEmpty(path)) {
            return true;
        }
        
        return false;
    }

    public bool Equals(SystemWatcherServiceOrder xOrder, SystemWatcherServiceOrder yOrder) {
        if (xOrder == null || yOrder == null) {
            return false;
        }

        return xOrder.path.EqualsFast(yOrder.path) && xOrder.filter.EqualsFast(yOrder.filter);
    }
    
    public int GetHashCode(SystemWatcherServiceOrder order) => HashCode.Combine(order.path, order.filter);
}