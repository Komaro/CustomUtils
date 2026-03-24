using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;

public class DownloadTestRunner {

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        Service.StartService<DownloadService>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        Service.RemoveService<DownloadService>();
    }
    
    // Temp Test Code
    [Test]
    public void UnsafeDownloadTest() {
        var service = Service.GetService<DownloadService>();
        Assert.IsNotNull(service);

        var path = "";
        service.Download(new JsonDownloadHandler_Unsafe<JsonData>(path), (result, handler) => {
            if (result == UnityWebRequest.Result.Success && handler.TryGetContent(out var data)) {
                Logger.TraceLog(data.ToStringAllFields());
            }
        });
    }

    [Test]
    public async Task SharedHttpTest() {
        await SharedHttp.GetHead("TestFile.json");
        var bytes = await SharedHttp.GetAsync("TestFile.json");
        
        Logger.TraceLog(bytes.GetRawString());
        Logger.TraceLog(bytes.GetString());
    }
}

public class JsonData {
    
    public string[] prefixes;
    public string[] voiceKeys;
}

public class JsonDownloadHandler_Unsafe<TReturn> : DownloadHandlerModule_Unsafe<TReturn> where TReturn : class {

    public JsonDownloadHandler_Unsafe(string url) : base(url) { }

    public override bool TryGetContent(out TReturn content) => (content = GetContent()) != null;

    public override TReturn GetContent() {
        try {
            return JsonConvert.DeserializeObject<TReturn>(GetText());
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }
}

public static class SharedHttp {
    
    public readonly static HttpClient Shared = new() {
        BaseAddress = new Uri("http://localhost:8000/")
    };

    public static async Task<HttpResponseHeaders> GetHead(string uri) {
        using var response = await Shared.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
        response.EnsureSuccessStatusCode();
        
        foreach (var header in response.Headers) {
            Logger.TraceLog($"{header.Key} || {header.Value.ToStringCollection(", ")}");
        }

        return response.Headers;
    }

    public static async Task<byte[]> GetAsync(string uri) {
        using var response = await Shared.GetAsync(uri);
        await using var stream = await response.Content.ReadAsStreamAsync();
        
        var buffer = MemoryPool<byte>.Shared.Rent(1024 * 4).Memory;
        var readBytes = 0;
        using var memoryStream = new MemoryStream();
        while ((readBytes = await stream.ReadAsync(buffer)) > 0) {
            await memoryStream.WriteAsync(buffer[..readBytes]);
        }

        return memoryStream.ToArray();
    }
}
