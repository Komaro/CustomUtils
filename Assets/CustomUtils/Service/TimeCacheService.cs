using System;
using System.Collections.Generic;

public class TimeCacheService : IService {

    private readonly Dictionary<Enum, DateTime> _requestTimeTableDic = new();

    void IService.Init() { }
    void IService.Start() { }
    void IService.Stop() => _requestTimeTableDic.Clear();
    
    public DateTime GetRequestTime(Enum type) => _requestTimeTableDic.ContainsKey(type) ? _requestTimeTableDic[type] : DateTime.MinValue;

    public void SetRequestTime(Enum type) => SetRequestTime(type, DateTime.Now);
    public void SetRequestTime(Enum type, DateTime time) => _requestTimeTableDic.AutoAdd(type, time);
    
    public void RemoveRequestTime(Enum type) => _requestTimeTableDic.AutoRemove(type);

    public bool IsSpanOverMinutes(Enum type, float minutes = 5f) => IsSpanOverMinutes(type, minutes, DateTime.Now);
    public bool IsSpanOverMinutes(Enum type, float minutes, DateTime time) => _requestTimeTableDic.TryGetValue(type, out var requestTime) && (time - requestTime).TotalMinutes > minutes;
}
