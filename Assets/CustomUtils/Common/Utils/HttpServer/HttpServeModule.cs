using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;

public abstract class HttpServeModule {

    protected SimpleHttpServer server;

    public void AddServer(SimpleHttpServer server) => this.server = server;
    public abstract bool Serve(HttpListenerContext context);
    public virtual void Close() { }
}

public class AssetBundleDistributionServeModule : HttpServeModule {

    private const int bufferSize = 1024 * 32;
    
    public override bool Serve(HttpListenerContext context) {
        var path = Path.Combine(server.GetTargetDirectory(), context.Request.RawUrl.TrimStart('/'));
        if (File.Exists(path)) {
            Logger.TraceLog($"Serve || {context.Request.HttpMethod} || {path}", Color.Magenta);
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                context.Response.ContentLength64 = fileStream.Length;
                if (context.Request.HttpMethod == HttpMethod.Head.Method) {
                    return false;
                }
                
                Span<byte> buffer = new byte[bufferSize];
                using (var outputStream = context.Response.OutputStream) {
                    var bytesLength = 0;
                    while ((bytesLength = fileStream.Read(buffer)) > 0) {
                        outputStream.Write(buffer[..bytesLength]);
                    }
                }
            }
            return true;
        }
        
        Logger.TraceLog($"Not Exists {nameof(path)} || {path}", Color.Red);
        return false;
    }
}