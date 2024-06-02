using System.IO;
using System.Net;
using UnityEngine;

public abstract class HttpServeModule {

    protected SimpleHttpWebServer server;

    public void AddServer(SimpleHttpWebServer server) => this.server = server;
    public abstract bool Serve(HttpListenerContext context);
}

public class AssetBundleServeModule : HttpServeModule {
    
    public override bool Serve(HttpListenerContext context) {
        var response = context.Response;
        var path = Path.Combine(server.GetTargetDirectory(), context.Request.RawUrl.TrimStart('/'));
        if (File.Exists(path)) {
            Logger.TraceLog($"Serve || {path}", Color.magenta);
            var buffer = File.ReadAllBytes(path);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        } else {
            return false;
        }
        
        return true;
    }
}