
using Newtonsoft.Json;

public abstract class TcpJsonPacket : ITcpPacket {

    public byte[] ToBytes() {
        var json = JsonConvert.SerializeObject(this);
        return json.ToBytes();
    }
    
    public abstract bool IsValid();
}

public class TcpJsonRequestSessionPacket : TcpJsonPacket {

    [JsonProperty("sid")]
    public uint sessionId;

    public TcpJsonRequestSessionPacket(uint sessionId) => this.sessionId = sessionId;

    public override bool IsValid() => sessionId > 0;
}

public class TcpJsonResponseSessionPacket : TcpJsonPacket {

    [JsonProperty("ia")]
    public bool isActive;
    
    public TcpJsonResponseSessionPacket(bool isActive) => this.isActive = isActive;
    
    public override bool IsValid() => true;
}