using System;
using System.Collections.Generic;
using UnityEngine;

public record AssetBundleChecksumInfo {

    public DateTime generateTime;
    public string target;
    public Dictionary<string, uint> crcDic = new();
    public Dictionary<string, string> hashDic = new();

    public bool TryGetChecksum(string assetBundle, out (uint crc, Hash128? hash) info) {
        info = GetChecksum(assetBundle);
        return info != (0, null);
    }

    public (uint crc, Hash128? hash) GetChecksum(string assetBundle) {
        if (crcDic.TryGetValue(assetBundle, out var crc) && hashDic.TryGetValue(assetBundle, out var hash)) {
            return (crc, Hash128.Parse(hash));
        }

        return (0, null);
    }
}