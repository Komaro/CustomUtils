using System;
using System.Collections.Generic;

public class TimeCacheService : IService {

    private readonly Dictionary<Enum, DateTime> _requestTimeTableDic = new();

    void IService.Init() => _requestTimeTableDic.Clear();
    void IService.Start() { }
    void IService.Stop() => _requestTimeTableDic.Clear();
    
    public DateTime GetRequestTime(Enum type) {
        if (_requestTimeTableDic.ContainsKey(type)) {
            return _requestTimeTableDic[type];
        }

        return DateTime.MinValue;
    }

    public void SetRequestTime(Enum type) => SetRequestTime(type, DateTime.Now);
    public void SetRequestTime(Enum type, DateTime time) => _requestTimeTableDic.AutoAdd(type, time);
    
    public void RemoveRequestTime(Enum type) => _requestTimeTableDic.AutoRemove(type);

    public bool IsSpanOverMinutes(Enum type) => IsSpanOverMinutes(type, 5f);
    public bool IsSpanOverMinutes(Enum type, float minutes) => IsSpanOverMinutes(type, minutes, DateTime.Now);
    
    public bool IsSpanOverMinutes(Enum type, float minutes, DateTime time) {
        if (_requestTimeTableDic.TryGetValue(type, out var requestTime)) {
            var timeSpan = time - requestTime;
            return timeSpan.TotalMinutes > minutes;
        }

        return false;
    }

}
