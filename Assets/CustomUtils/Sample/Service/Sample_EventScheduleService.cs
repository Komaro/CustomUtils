using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

[Service(DEFAULT_SERVICE_TYPE.PLAY_DURING)]
public class Sample_EventScheduleService : IService {

    private TimeSyncService _timeSyncService;
    
    // Convertible to UniRx
    private GameObject _rootObject;
    private Sample_CoroutineObjet _coroutineObject;
    //

    private bool _isServing;

    public delegate void StartEventHandler(int id);
    private SafeDelegate<StartEventHandler> OnStartEventHandler;
    public event StartEventHandler OnStartEvent {
        add { OnStartEventHandler += value; _eventSchedulerList?.ForEach(scheduler => scheduler.OnStartEvent += value); }
        remove { OnStartEventHandler -= value; _eventSchedulerList?.ForEach(scheduler => scheduler.OnStartEvent -= value); }
    }

    public delegate void UpdateEventTimeHandler(int id, TimeSpan remainTime);
    private SafeDelegate<UpdateEventTimeHandler> OnUpdateEventTimeHandler;
    public event UpdateEventTimeHandler OnUpdateEventTime {
        add { OnUpdateEventTimeHandler += value; _eventSchedulerList?.ForEach(scheduler => scheduler.OnUpdateEventTime += value); }
        remove { OnUpdateEventTimeHandler -= value; _eventSchedulerList?.ForEach(scheduler => scheduler.OnUpdateEventTime -= value); }
    }

    public delegate void TimeOutEventHandler(int id);
    private SafeDelegate<TimeOutEventHandler> OnTimeOutEventHandler;
    public event TimeOutEventHandler OnTimeOutEvent {
        add { OnTimeOutEventHandler += value; _eventSchedulerList?.ForEach(scheduler => scheduler.OnTimeOutEvent += value); }
        remove { OnTimeOutEventHandler -= value; _eventSchedulerList?.ForEach(scheduler => scheduler.OnTimeOutEvent -= value); }
    }

    public delegate void TomorrowStartHandler();
    public SafeDelegate<TomorrowStartHandler> OnTomorrowStart;

    private List<Sample_EventScheduler> _eventSchedulerList = new();

    private List<Sample_EventInfo> _sampleEventInfoList = new();

    bool IService.IsServing() => _isServing;

    void IService.Init() {
        if (_rootObject != null) {
            if (_coroutineObject != null)
                _coroutineObject.Stop();

            Object.Destroy(_rootObject, 0.0f);
        }

        _rootObject = new GameObject { hideFlags = HideFlags.HideAndDontSave };
        _coroutineObject = _rootObject.AddComponent<Sample_CoroutineObjet>();
        
        // Input Sample EventInfo
        if (Service.TryGetService(out _timeSyncService)) {
            _sampleEventInfoList.Add(new Sample_EventInfo(0, _timeSyncService.GetUTCTime()));
            _sampleEventInfoList.Add(new Sample_EventInfo(1, _timeSyncService.GetUTCTime().AddMinutes(1)));
            _sampleEventInfoList.Add(new Sample_EventInfo(2, _timeSyncService.GetUTCTime().AddHours(1)));
            _sampleEventInfoList.Add(new Sample_EventInfo(3, _timeSyncService.GetUTCTime().AddDays(1)));
        }
    }

    void IService.Start() => Start();

    private void Start() {
        foreach (var info in _sampleEventInfoList) {
            if (info == null)
                continue;

            StartEvent(info);
        }
        
        if (_coroutineObject != null) {
            _coroutineObject.OnUpdateTime += OnUpdateEventTimer;
            _coroutineObject.OnTomorrowStartTime += OnTomorrowStartTimer;
        }

        _isServing = true;
    }

    void IService.Stop() {                
        _coroutineObject?.Stop();
        _eventSchedulerList.SafeClear(x => x.Stop());
        
        if (_coroutineObject != null) {
            _coroutineObject.OnUpdateTime -= OnUpdateEventTimer;
            _coroutineObject.OnTomorrowStartTime -= OnTomorrowStartTimer;
        }
        
        _isServing = false;
    }

    void IService.Refresh() {
        foreach (var info in _sampleEventInfoList) {
            if (_eventSchedulerList.TryFind(out var scheduler, x => x.IsMatch(info))) {
                scheduler.Refresh(info);
            }
        }
    }

    public void StartEvent(Sample_EventInfo info) {
        if (_eventSchedulerList.TryFind(out var scheduler, x => x.IsMatch(info))) {
            scheduler.Refresh(info);
        } else {
            scheduler = CreateScheduler(info);
            if (scheduler.GetRecord() != null) {
                _eventSchedulerList.Add(scheduler);
            } else {
                Logger.TraceError($"{nameof(scheduler)} is Invalid || {info.id}");
                return;
            }
        }
        
        scheduler.Start();
    }

    private bool TryCreateScheduler(Sample_EventInfo info, out Sample_EventScheduler scheduler) {
        scheduler = CreateScheduler(info);
        return scheduler?.GetRecord() != null;
    }
    
    private Sample_EventScheduler CreateScheduler(Sample_EventInfo info) {
        var scheduler = new Sample_EventScheduler(info);
        scheduler.OnStartEvent += OnStartEventHandler;
        scheduler.OnUpdateEventTime += OnUpdateEventTimeHandler;
        scheduler.OnTimeOutEvent += OnTimeOutEventHandler;
        return scheduler;
    }
    
    public bool TryGetValidEventRecord(int eventId, out Sample_Event record) {
        record = GetValidEventRecord(eventId);
        return record != null;
    }

    public Sample_Event GetValidEventRecord(int eventId) => _eventSchedulerList?.Find(x => x.IsMatch(eventId))?.GetRecord();

    public bool TryGetValidEventRecordList(out List<Sample_Event> recordList) {
        recordList = GetValidEventRecordList();
        recordList?.RemoveAll(x => x == null);
        return recordList != null && recordList.Count > 0;
    }
    
    public List<Sample_Event> GetValidEventRecordList() => GetValidSchedulerList()?.Select(x => x.GetRecord()).ToList();

    public bool TryGetValidEventInfo(int eventId, out Sample_EventInfo info) {
        info = GetValidEventInfo(eventId);
        return info != null;
    }
    
    public Sample_EventInfo GetValidEventInfo(int eventId) => _eventSchedulerList?.Find(x => x.IsMatch(eventId))?.GetInfo();

    public bool TryGetValidEventInfoList(out List<Sample_EventInfo> infoList) {
        infoList = GetValidEventInfoList();
        return infoList != null && infoList.Count > 0;
    }

    public List<Sample_EventInfo> GetValidEventInfoList() => GetValidSchedulerList().Select(x => x.GetInfo()).ToList();
    public List<Sample_EventScheduler> GetValidSchedulerList() => _eventSchedulerList?.FindAll(x => x.IsValidTime());

    private void OnUpdateEventTimer(float tick) {
        foreach (var scheduler in _eventSchedulerList) {
            scheduler.Update();
        }
    }

    private void OnTomorrowStartTimer() {
        foreach (var info in _sampleEventInfoList) {
            // Refresh Daily Info
        }

        OnTomorrowStart.handler?.Invoke();
    }
    
    #region [DEBUG]

    private Dictionary<int, DateTime> _cacheResetEndTimeDic = new();

    public void AddDummyEvent(Sample_EventInfo info) => StartEvent(info);

    public void ChangeEventEndTime(int eventId, int days, int hours, int minutes, int seconds) {
        if (_eventSchedulerList.TryFind(out var scheduler, x => x.IsMatch(eventId))) {
            if (_cacheResetEndTimeDic.TryGetValue(eventId, out var time)) {
                if (time == DateTime.MinValue)
                    _cacheResetEndTimeDic[eventId] = scheduler.GetInfo().shopEndDate;
            } else {
                _cacheResetEndTimeDic.Add(eventId, scheduler.GetInfo().shopEndDate);
            }
            
            scheduler.GetInfo().shopEndDate = scheduler.GetInfo().shopEndDate.AddDays(days).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
        }
    }
    
    public void ResetEventEndTime() {
        foreach (var pair in _cacheResetEndTimeDic) {
            if (_eventSchedulerList.TryFind(out var scheduler, x => x.IsMatch(pair.Key)) && pair.Value != DateTime.MinValue) {
                scheduler.GetInfo().shopEndDate = pair.Value;
                scheduler.Refresh();
            }
        }

        _cacheResetEndTimeDic.Clear();
        Start();
    }

    public List<Sample_EventScheduler> GetSchedulersList() => _eventSchedulerList.FindAll(x => x.IsValid());
    public void ResetRemainTomorrowTick() => _coroutineObject.SetRemainTick((float)(_timeSyncService.GetUTCTommorowStartTime() - _timeSyncService.GetUTCTime()).TotalSeconds);
    public float GetRemainTomorrowTick() => _coroutineObject.GetRemainTick();
    public void SetRemainTomorrowTick(float tick) => _coroutineObject.SetRemainTick(tick);

    #endregion
    
    public class Sample_Event {
        
        public int ID;
    }
    
    public class Sample_EventInfo {
        
        public int id;
        public DateTime shopEndDate;

        public Sample_EventInfo(int id, DateTime shopEndDate) {
            this.id = id;
            this.shopEndDate = shopEndDate;
        }
        
        public bool IsValidEvent() => shopEndDate > Service.GetService<TimeSyncService>().GetUTCTime();
    }

    public class Sample_EventScheduler {

        private Sample_EventInfo _info;
        private Sample_Event _record;
        
        private bool _eventTimeOut;
        private TimeSpan _remainEventTime;
        
        public SafeDelegate<StartEventHandler> OnStartEvent;
        public SafeDelegate<UpdateEventTimeHandler> OnUpdateEventTime;
        public SafeDelegate<TimeOutEventHandler> OnTimeOutEvent;

        public Sample_EventScheduler(Sample_EventInfo info) {
            if (info == null) {
                Logger.TraceError($"{nameof(info)} is Null");
                return;
            }

            if (info.IsValidEvent() == false) {
                _eventTimeOut = true;
                _remainEventTime = TimeSpan.Zero;
                return;
            }

            _info = info;
            _eventTimeOut = false;
            _remainEventTime = _info.shopEndDate - Service.GetService<TimeSyncService>().GetUTCTime();
        }

        public void Refresh() => Refresh(_info);

        public void Refresh(Sample_EventInfo info) {
            if (IsMatch(info)) {
                _info = info;

                if (_info.IsValidEvent()) {
                    _eventTimeOut = false;
                    _remainEventTime = TimeSpan.Zero;
                    return;
                }
                
                _eventTimeOut = false;
                _remainEventTime = _info.shopEndDate - Service.GetService<TimeSyncService>().GetUTCTime();
            }
        }

        public void Start() {
            if (IsValid()) {
                OnStartEvent.handler?.Invoke(_info.id);
            }
        }
        
        public void Update() {
            if (IsInvalid()) {
                return;
            }

            _remainEventTime = _info.shopEndDate - Service.GetService<TimeSyncService>().GetUTCTime();
            OnUpdateEventTime.handler?.Invoke(_info.id, _remainEventTime);
            if (_remainEventTime.TotalSeconds <= 0) {
                _eventTimeOut = true;
                OnTimeOutEvent.handler?.Invoke(_info.id);
            }
        }

        public void Stop(){
            OnStartEvent.Clear();
            OnUpdateEventTime.Clear();
            OnTimeOutEvent.Clear();
        }

        public Sample_EventInfo GetInfo() => _info;
        public Sample_Event GetRecord() => _record;

        public bool IsMatch(int id) => _info != null && _info.id == id;
        public bool IsMatch(Sample_EventInfo info) => _info != null && info != null && _info.id == info.id;
        public bool IsMatch(Sample_Event record) => _record != null && record != null && _record.ID == record.ID;

        public bool IsValid() => _info != null && _record != null && _eventTimeOut == false;
        
        public bool IsInvalid() => _eventTimeOut || _info == null || _record == null;
        
        public bool IsValidTime() => _info.IsValidEvent();

        #region [DEBUG]
        
        public TimeSpan GetRemainTime() => _remainEventTime;

        #endregion
    }
    
    protected class Sample_CoroutineObjet : MonoBehaviour {
        
        public delegate void UpdateTimeHandler(float tick);
        public SafeDelegate<UpdateTimeHandler> OnUpdateTime;
        
        public delegate void FixedUpdateTimeHandler(float tick);
        public SafeDelegate<FixedUpdateTimeHandler> OnFixedUpdateTime;

        public delegate void TomorrowTimeHandler();
        public SafeDelegate<TomorrowTimeHandler> OnTomorrowStartTime;

        private float _remainTick;

        private void Awake() {
            DontDestroyOnLoad(this);
            RefreshTick();
        }
        
        private void Update() {
            _remainTick -= Time.unscaledDeltaTime;
            if (_remainTick <= 0) {
                OnTomorrowStartTime.handler?.Invoke();
                RefreshTick();
            }

            OnUpdateTime.handler?.Invoke(Time.deltaTime);
        }

        private void FixedUpdate() => OnFixedUpdateTime.handler?.Invoke(Time.fixedDeltaTime);
        private void OnDestroy() => Stop();

        public void Stop() {
            OnUpdateTime.Clear();
            OnFixedUpdateTime.Clear();
            _remainTick = 0;
        }
        
        private void RefreshTick() {
            if (Service.TryGetService<TimeSyncService>(out var service)) {
                _remainTick = (float)(service.GetUTCTommorowStartTime() - service.GetUTCTime()).TotalSeconds;
            }
        }

        #region [DEBUG]

        public float GetRemainTick() => _remainTick;
        public void SetRemainTick(float tick) => _remainTick = tick;

        #endregion
    }
}
