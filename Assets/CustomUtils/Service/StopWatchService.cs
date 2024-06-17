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

    public void StartWatch([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch)) {
            watch.Restart();
        } else {
            watch = new Stopwatch();
            watch.Start();
            _watchDic.Add(caller, watch);
        }
    }

    public void StopWatch([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch)) {
            watch.Stop();
            Logger.TraceLog($"{caller} => {watch.ElapsedTicks} tick || {watch.ElapsedMilliseconds}  milSec|| {watch.Elapsed}", Color.green);
        } else {
            Logger.TraceLog($"{caller} is Not Started", Color.yellow);
        }
    }

    public void StartWatchAverage([CallerMemberName] string caller = null) {
        _countDic.AutoAccumulateAdd(caller, 1);
        StartWatch(caller);
    }

    public void StopWatchAverage([CallerMemberName] string caller = null) {
        if (_watchDic.TryGetValue(caller, out var watch)) {
            watch.Stop();

            _averageDic.AutoAccumulateAdd(caller, watch.ElapsedTicks);

            if (_countDic.TryGetValue(caller, out var count) && _averageDic.TryGetValue(caller, out var total)) {
                Logger.TraceLog($"{caller} => {watch.ElapsedTicks} tick || {watch.ElapsedMilliseconds} milSec || {watch.Elapsed} || Avg = {total / count} tick", Color.magenta); 
            }
        }
    }
}
