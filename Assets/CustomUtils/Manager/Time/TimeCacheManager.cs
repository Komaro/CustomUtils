using System;
using System.Collections.Generic;

public class TimeCacheManager : Singleton<TimeCacheManager> {
    
    // Polishing
    private readonly Dictionary<Enum, DateTime> _requestTimeTableDic = new Dictionary<Enum, DateTime>();

    public void Clear(Enum type) => _requestTimeTableDic.Remove(type);
    //    
    
    private readonly Dictionary<TimeCacheType, DateTime> _requestTimeTable = new Dictionary<TimeCacheType, DateTime>();

    public void Clear() => _requestTimeTable.Clear();
    public void Clear(TimeCacheType type) => _requestTimeTable.Remove(type);

    public void SetRequestTimeNow(TimeCacheType type) => SetRequestTime(type, DateTime.Now);
    public void SetRequestTime(TimeCacheType type, DateTime time) {
        if (_requestTimeTable.ContainsKey(type) == false) {
            _requestTimeTable.Add(type, time);
        } else {
            _requestTimeTable[type] = time;
        }
    }

    public bool IsSpanOverMinutes(TimeCacheType type) => IsSpanOverMinutes(type, 5f);
    public bool IsSpanOverMinutes(TimeCacheType type, float minutes) => IsSpanOverMinutes(type, minutes, DateTime.Now);
	
    public bool IsSpanOverMinutes(TimeCacheType type, float minutes, DateTime time) {
        if (_requestTimeTable.ContainsKey(type) == false) {
            return true;
        }
		
        var timeSpan = time - _requestTimeTable[type];
        return timeSpan.TotalMinutes > minutes;
    }
}


public enum TimeCacheType {
    NONE,
    TIME_SYNC,
}