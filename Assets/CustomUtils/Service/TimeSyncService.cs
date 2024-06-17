using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class TimeSyncService : IService {

    private const string NTP_SERVER_URL = "pool.ntp.org";
    private const int NTP_SERVER_PROT = 123;
    private const int RECEIVE_TIME_OUT = 3000;
    
    private DateTime _utcSyncTime;

    void IService.Start() => Sync();
    void IService.Stop() { }

    public void Sync() {
        var ntpData = new byte[48];
        ntpData[0] = 0x1B;

        var addresses = Dns.GetHostEntry(NTP_SERVER_URL).AddressList;
        if (addresses.Length <= 0) {
            Logger.TraceError($"{nameof(addresses)} Length is Zero");
            return;
        }
        
        var ipEndPoint = new IPEndPoint(addresses[0], NTP_SERVER_PROT);
        try {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(ipEndPoint);
            socket.ReceiveTimeout = RECEIVE_TIME_OUT;

            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();
        } catch (Exception e) {
            Logger.Error(e.Message);
            return;
        }

        ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | ntpData[43];
        ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | ntpData[47];
        _utcSyncTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)(intPart * 1000 + fractPart * 1000 / 0x100000000L));
        Logger.TraceLog($"Time Sync || {_utcSyncTime}");
    }
    
    public DateTime GetUTCTime() => _utcSyncTime == DateTime.MinValue ? DateTime.UtcNow : _utcSyncTime;
    
    public DateTime GetUTCTommorowStartTime() {
        var tomorrow = GetUTCTime().AddDays(1);
        return new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 0, 0, 0, DateTimeKind.Utc);
    }
    
    public DateTime GetKtcTime() => GetUTCTime().AddHours(9);

    public IEnumerator TimeSyncCoroutine(float delay) {
        while (true) {
            yield return new WaitForSeconds(delay);
            Sync();
        }
    }
}
