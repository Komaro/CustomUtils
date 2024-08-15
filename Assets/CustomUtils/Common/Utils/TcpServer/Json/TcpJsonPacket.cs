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

public record TcpJsonConnectSessionPacket : TcpJsonPacket {

    public override bool IsValid() => sessionId > 0;
}

public record TcpJsonResponseSessionPacket : TcpJsonPacket {

    [JsonProperty("ia")]
    public bool isActive;

    public override bool IsValid() => true;
}

public record TcpJsonRequestTestPacket : TcpJsonPacket {

    [JsonProperty("rt")]
    public string requestText;
    
    public override bool IsValid() => string.IsNullOrEmpty(requestText) == false;
}

public record TcpJsonResponseTestPacket : TcpJsonPacket {

    [JsonProperty("rt")]
    public string responseText;

    public override bool IsValid() => true;
}