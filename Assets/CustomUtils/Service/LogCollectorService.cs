using System.Collections.Generic;
using UnityEngine;

public class LogCollectorService : IService {

    private List<string> _logList;
    private HashSet<LogType> _filterSet;

    private int _maxLog = 500;

    private bool _isServing;

    private readonly object _lock = new();
    
    void IService.Init() {
        _logList = new List<string>(_maxLog / 2);
        _filterSet = new HashSet<LogType>(EnumUtil.GetValueList<LogType>());
    }

    void IService.Start() {
        if (_isServing == false) {
            Application.logMessageReceived += OnLogMessageReceived;
            _isServing = true;
        }
    }

    void IService.Stop() {
        Application.logMessageReceived -= OnLogMessageReceived;
        _isServing = false;
    }
    
    void IService.Refresh() {
        ClearFilter();
        ClearLog();
    }

    void IService.Remove() {
        ClearFilter();
        ClearLog();
    }

    public void ClearLog() {
        lock (_lock) {
            _logList.Clear();
        }
    }
    
    public List<string> Copy() {
        lock (_lock) {
            return new List<string>(_logList);
        }
    }

    public void SetFilter(params LogType[] filters) {
        lock (_lock) {
            _filterSet.Clear();
            filters.ForEach(filter => _filterSet.Add(filter));
        }
    }

    public void AddFilter(LogType filter) {
        lock (_lock) {
            _filterSet.Add(filter);
        }
    }

    public void RemoveFilter(LogType filter) {
        lock (_lock) {
            _filterSet.Remove(filter);
        }
    }

    public void ClearFilter() {
        lock (_lock) {
            _filterSet.Clear();
        }
    }
    
    private void OnLogMessageReceived(string condition, string trace, LogType type) {
        lock (_lock) {
            if (_filterSet.Contains(type)) {
                _logList.Add($"[{type}] {condition}");
            }
        }
    }
}