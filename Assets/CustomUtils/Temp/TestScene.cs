using UnityEngine;

public class TestScene : MonoBehaviour {

    public void Start() {
        var bytes = Service.GetService<SecurityService>().GetNativeKey<DefaultSecurityModule>();
        bytes.ThrowIfNull();
        if (bytes.Length <= 0) {
            Logger.TraceError($"{nameof(bytes)} length is zero");
        }
        
        Logger.TraceLog(bytes.ToStringCollection(b => b.ToHex()));
        Logger.TraceLog(bytes.ToStringCollection(b => b.ToString()));
    }
}
