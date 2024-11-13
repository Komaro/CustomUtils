using Newtonsoft.Json;

public abstract record TcpJsonPacket : ITcpPacket {

    [JsonProperty("id")]
    public uint sessionId;

    public byte[] ToBytes() {
        var json = JsonConvert.SerializeObject(this);
        return json.ToBytes();
    }

    public T ToPopulate<T>(string json) where T : TcpJsonPacket {
        JsonConvert.PopulateObject(json, this);
        return this as T;
    } 

    public abstract bool IsValid();
}

public record TcpJsonSessionConnect : TcpJsonPacket {

    public override bool IsValid() => sessionId > 0;

    public TcpJsonSessionConnect() { }
    public TcpJsonSessionConnect(uint sessionId) => this.sessionId = sessionId;
    public TcpJsonSessionConnect(TcpSession session) : this(session.ID) { }
}

public record TcpJsonSessionConnectResponse : TcpJsonPacket {

    [JsonProperty("ia")]
    public bool isActive;

    public override bool IsValid() => true;
}

public record TcpJsonTestRequest : TcpJsonPacket {

    [JsonProperty("rt")]
    public string requestText;
    
    public override bool IsValid() => string.IsNullOrEmpty(requestText) == false;
}

public record TcpJsonTestResponse : TcpJsonPacket {

    [JsonProperty("rt")]
    public string responseText;

    public override bool IsValid() => true;
}

public record TcpJsonDisconnectRequest : TcpJsonPacket {

    [JsonProperty("ds")]
    public int delaySeconds;

    public override bool IsValid() => true;
}