using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class SystemWatcherService : IService {

    private Dictionary<SystemWatcherServiceOrder, FileSystemWatcher> _fileSystemWatcherDic = new(new SystemWatcherServiceOrder());
    private SynchronizationContext _context;

    private bool _isServing;

    bool IService.IsServing() => _isServing;
    void IService.Init() => _context = SynchronizationContext.Current;

    void IService.Start() {
        foreach (var watcher in _fileSystemWatcherDic.Values) {
            watcher.EnableRaisingEvents = true;
        }

        _isServing = true;
    }

    void IService.Stop() {
        foreach (var watcher in _fileSystemWatcherDic.Values) {
            watcher.EnableRaisingEvents = false;
        }
        
        _isServing = false;
    }

    void IService.Remove() => _fileSystemWatcherDic.SafeClear();

    public FileSystemWatcher this[SystemWatcherServiceOrder order] => _fileSystemWatcherDic.GetValueOrDefault(order);

    public SystemWatcherServiceOrder Start(SystemWatcherServiceOrder order) {
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

        watcher = _fileSystemWatcherDic.GetOrAdd(order);
        watcher.BeginInit();
        
        watcher.Path = order.path;
        watcher.Filter = order.filter;
        watcher.NotifyFilter = order.filters;
        watcher.IncludeSubdirectories = order.includeSubDirectories;
        
        AddHandler(watcher, order.handler);
        AddHandler(watcher, order.errorHandler);
        AddHandler(watcher, order.renamedHandler);
            
        watcher.EndInit();
        watcher.EnableRaisingEvents = true;
        
        return order;
    }

    public SystemWatcherServiceOrder Stop(SystemWatcherServiceOrder order) {
        if (order?.Invalid() == false && _fileSystemWatcherDic.TryGetValue(order, out var watcher)) {
            watcher.EnableRaisingEvents = false;
        }
        
        return order;
    }
    
    public void Remove(SystemWatcherServiceOrder order) {
        if (order?.Invalid() == false) {
            _fileSystemWatcherDic.SafeRemove(order, watcher => {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            });
        }
    }

    private void AddHandler(FileSystemWatcher watcher, FileSystemEventHandler handler) {
        watcher.Changed -= OnEvent(handler);
        watcher.Changed += OnEvent(handler);
        
        watcher.Created -= OnEvent(handler);
        watcher.Created += OnEvent(handler);

        watcher.Deleted -= OnEvent(handler);
        watcher.Deleted += OnEvent(handler);
    }

    private void AddHandler(FileSystemWatcher watcher, ErrorEventHandler handler) {
        watcher.Error -= OnError(handler);
        watcher.Error += OnError(handler);
    }

    private void AddHandler(FileSystemWatcher watcher, RenamedEventHandler handler) {
        watcher.Renamed -= OnRenamed(handler);
        watcher.Renamed += OnRenamed(handler);
    }
    
    private FileSystemEventHandler OnEvent(FileSystemEventHandler handler) => (ob, args) => _context.Post(_ => handler?.Invoke(ob, args), null);
    private ErrorEventHandler OnError(ErrorEventHandler handler) => (ob, args) => _context.Post(_ => handler?.Invoke(ob, args), null);
    private RenamedEventHandler OnRenamed(RenamedEventHandler handler) => (ob, args) => _context.Post(_ => handler?.Invoke(ob, args), null);
}

public record SystemWatcherServiceOrder : IEqualityComparer<SystemWatcherServiceOrder> {
    
    public string path;
    public string filter;
    public NotifyFilters filters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess;
    public bool includeSubDirectories = false;
    
    public FileSystemEventHandler handler;
    public ErrorEventHandler errorHandler;
    public RenamedEventHandler renamedHandler;

    public SystemWatcherServiceOrder() { }
    public SystemWatcherServiceOrder(string filter) => this.filter = filter;

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