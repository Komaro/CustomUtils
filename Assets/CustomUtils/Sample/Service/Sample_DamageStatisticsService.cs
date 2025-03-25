using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Random = UnityEngine.Random;

[Service(DEFAULT_SERVICE_TYPE.PLAY_DURING)]
public class Sample_DamageStatisticsService : IService {

    private List<(int stage, Dictionary<int, double> damageDic)> _statisticsHistoryList = new();
    
    private ReactiveDictionary<int, double> _damageDic = new();

    private IObservable<DictionaryAddEvent<int, double>> _addSkillStream;
    private IObservable<DictionaryReplaceEvent<int, double>> _damageReplaceStream;
    
    private List<IDisposable> _disposableList = new();
    
    private bool _isServing;

    public delegate void AddSkillHandler(int id, double value);
    public SafeDelegate<AddSkillHandler> OnAddSkill;

    public delegate void ReplaceCountHandler(int id, double value);
    public SafeDelegate<AddSkillHandler> OnReplaceCount;

    bool IService.IsServing() => _isServing;

    void IService.Init() {
        _addSkillStream = _damageDic.ObserveAdd();
        _damageReplaceStream = _damageDic.ObserveReplace();
    }

    void IService.Start() {
        _disposableList.Add(_addSkillStream.Subscribe(addEvent => OnAddSkill.handler?.Invoke(addEvent.Key, addEvent.Value)));
        _disposableList.Add(_damageReplaceStream.Subscribe(replaceEvent => OnReplaceCount.handler?.Invoke(replaceEvent.Key, replaceEvent.NewValue - replaceEvent.OldValue)));
        
        _isServing = true;
    }

    void IService.Stop() {
        _disposableList.ForEach(x => x.Dispose());
        _disposableList.Clear();
        
        _isServing = false;
    }

    void IService.Remove() {
        OnAddSkill.Clear();
        OnReplaceCount.Clear();
        
        _statisticsHistoryList.Clear();
        _damageDic.Clear();
    }

    public void AddDamage(int skillId, double damage) => _damageDic.AutoAccumulateAdd(skillId, damage);

    public void Clear() {
        if (_damageDic.Count <= 0) {
            _damageDic.Clear();
            return;
        }
        
        if (_statisticsHistoryList.Count > 10) {
            _statisticsHistoryList.RemoveAt(0);
        }

        var sampleRandomStage = Random.Range(0, 10000);
        _statisticsHistoryList.Add((sampleRandomStage, _damageDic.ToDictionary(x => x.Key, x => x.Value)));
        _damageDic.Clear();
    }

    public double GetCurrentTotalDamage() => _damageDic.Sum(x => x.Value);
    public IDictionary<int, double> GetCurrentDamage() => _damageDic;

    public List<(int stage, Dictionary<int, double> damageDic)> GetHistory() => _statisticsHistoryList;
    public List<(int stage, Dictionary<int, double> damageDic)> GetHistory(Predicate<(int stage, Dictionary<int, double> damageDic)> match) => _statisticsHistoryList.FindAll(match);

    public bool TryGetFirstHistory(out Dictionary<int, double> outDic) {
        outDic = GetFirstHistory();
        return outDic != null;
    }
    
    public Dictionary<int, double> GetFirstHistory() => GetHistory(0);

    public bool TryGetLastHistory(out Dictionary<int, double> outDic) {
        outDic = GetLastHistory();
        return outDic != null;
    }
    
    public Dictionary<int, double> GetLastHistory() => GetHistory(_statisticsHistoryList.Count - 1);
    
    public bool TryGetHistory(int index, out Dictionary<int, double> outDic) {
        outDic = GetHistory(index);
        return outDic != null;
    }
    
    public Dictionary<int, double> GetHistory(int stage) => _statisticsHistoryList.TryFirst(stage, out var history) ? history.damageDic : default;
    public bool IsClear() => _damageDic.Count <= 0;
}
