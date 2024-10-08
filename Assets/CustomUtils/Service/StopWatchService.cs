using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class StopWatchService : IService {

    private Dictionary<string, Stopwatch> _watchDic = new();

    private Dictionary<string, double> _countDic = new();
    private Dictionary<string, double> _averageDic = new();

    void IService.Start() => _watchDic.Clear();
    void IService.Stop() => _watchDic.Clear();
    void IService.Refresh() => _watchDic.Clear();

    public void Start([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch)) {
            watch.Restart();
        } else {
            watch = new Stopwatch();
            watch.Start();
            _watchDic.Add(caller, watch);
        }
    }

    public void Stop([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch)) {
            watch.Stop();
            Logger.TraceLog($"{caller} => {watch.ElapsedTicks} tick || {watch.ElapsedMilliseconds}  milSec|| {watch.Elapsed}", Color.green);
        } else {
            Logger.TraceLog($"{caller} is Not Started", Color.yellow);
        }
    }

    public void StartAverage([CallerMemberName] string caller = null) {
        _countDic.AutoAccumulateAdd(caller, 1);
        Start(caller);
    }

    public void RecordAverage([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch)) {
            watch.Stop();
            _averageDic.AutoAccumulateAdd(caller, watch.ElapsedTicks);
        }
    }

    public void StopAverage([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch)) {
            watch.Stop();

            _averageDic.AutoAccumulateAdd(caller, watch.ElapsedTicks);

            if (_countDic.TryGetValue(caller, out var count) && _averageDic.TryGetValue(caller, out var total)) {
                Logger.TraceLog($"{caller} => {watch.ElapsedTicks} tick || {watch.ElapsedMilliseconds} milSec || {watch.Elapsed} || Avg = {total / count} tick", Color.magenta); 
            }
        }
    }

    public void LogAverage([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch) && _countDic.TryGetValue(caller, out var count) && _averageDic.TryGetValue(caller, out var total)) {
            Logger.TraceLog($"{caller} => {watch.ElapsedTicks} tick || {watch.ElapsedMilliseconds} milSec || {watch.Elapsed} || Avg = {total / count} tick", Color.magenta);
        }
    }

    public double GetAverage([CallerMemberName] string caller = null) => _watchDic.TryGetValue(caller, out var watch) && _countDic.TryGetValue(caller, out var count) && _averageDic.TryGetValue(caller, out var total) ? total / count : 0d;
}
