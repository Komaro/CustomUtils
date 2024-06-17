using System.Collections.Generic;
using UnityEngine;

public class LogCollectorService : IService {

    private List<string> _logList;
    private HashSet<LogType> _filterSet;

    private int _maxLog = 500;

    private bool _isServing;

    void IService.Init() {
        _logList = new List<string>(_maxLog / 2);
        _filterSet = new HashSet<LogType>(EnumUtil.GetValues<LogType>());
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

    public void ClearLog() {
        lock (_logList) {
            _logList.Clear();
        }
    }
    
    public List<string> Copy() {
        lock (_logList) {
            return new List<string>(_logList);
        }
    }

    public void SetFilter(params LogType[] filters) {
        lock (_filterSet) {
            _filterSet.Clear();
            filters.ForEach(filter => _filterSet.Add(filter));
        }
    }

    public void AddFilter(LogType filter) {
        lock (_filterSet) {
            _filterSet.Add(filter);
        }
    }

    public void RemoveFilter(LogType filter) {
        lock (_filterSet) {
            _filterSet.Remove(filter);
        }
    }
    
    private void OnLogMessageReceived(string condition, string trace, LogType type) {
        lock (_logList) {
            if (_filterSet.Contains(type)) {
                _logList.Add($"[{type}] {condition}");
            }
        }
    }
}
