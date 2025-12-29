using System;
using System.Collections.Generic;

public class TimeCacheService : IService {

    private readonly Dictionary<Enum, DateTime> _requestTimeTableDic = new();

    void IService.Init() { }
    void IService.Start() { }
    void IService.Stop() => _requestTimeTableDic.Clear();
    
    public bool CheckAndUpdateElapsed<TEnum>(TEnum type, float seconds = 300f) where TEnum : Enum {
        if (_requestTimeTableDic.TryGetValue(type, out var time)) {
            if ((DateTime.Now - time).TotalSeconds > seconds) {
                _requestTimeTableDic[type] = DateTime.Now;
                return true;
            }

            return false;
        }

        _requestTimeTableDic.Add(type, DateTime.Now);
        return true;
    }
    
    public DateTime GetRequestTime(Enum type) => _requestTimeTableDic.TryGetValue(type, out var time) ? time : DateTime.MinValue;
    public DateTime GetRequestTime<TEnum>(TEnum type) where TEnum : Enum => _requestTimeTableDic.TryGetValue(type, out var time) ? time : DateTime.MinValue;

    public void SetRequestTime(Enum type) => SetRequestTime(type, DateTime.Now);
    public void SetRequestTime<TEnum>(TEnum type) where TEnum : Enum => SetRequestTime(type, DateTime.Now); 
    
    public void SetRequestTime(Enum type, DateTime time) => _requestTimeTableDic.AutoAdd(type, time);
    public void SetRequestTime<TEnum>(TEnum type, DateTime time) where TEnum : Enum => _requestTimeTableDic.AutoAdd(type, time); 
    
    public void RemoveRequestTime(Enum type) => _requestTimeTableDic.AutoRemove(type);
    public void RemoveRequestTime<TEnum>(TEnum type) where TEnum : Enum => _requestTimeTableDic.AutoRemove(type); 

    public bool IsOverSeconds(Enum type, float seconds = 300f) => IsOverSeconds(type, seconds, DateTime.Now);
    public bool IsOverSeconds<TEnum>(TEnum type, float seconds = 300) where TEnum : Enum => IsOverSeconds(type, seconds, DateTime.Now);

    private bool IsOverSeconds(Enum type, float seconds, DateTime time) => _requestTimeTableDic.TryGetValue(type, out var requestTime) && (time - requestTime).TotalSeconds > seconds;
    private bool IsOverSeconds<TEnum>(TEnum type, float seconds, DateTime time) where TEnum : Enum => _requestTimeTableDic.TryGetValue(type, out var requestTime) && (time - requestTime).TotalSeconds > seconds;
}
