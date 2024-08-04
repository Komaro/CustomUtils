using System;
using System.IO;
using System.Net;
using System.Net.Http;
using UnityEngine;

public abstract class HttpServeModule {

    protected SimpleHttpServer server;

    public void AddServer(SimpleHttpServer server) => this.server = server;
    public abstract bool Serve(HttpListenerContext context);
    public virtual void Close() { }
}

public class AssetBundleServeModule : HttpServeModule {

    private const int bufferSize = 1024 * 32;
    
    public override bool Serve(HttpListenerContext context) {
        var response = context.Response;
        var path = Path.Combine(server.GetTargetDirectory(), context.Request.RawUrl.TrimStart('/'));
        if (File.Exists(path)) {
            Logger.TraceLog($"Serve || {context.Request.HttpMethod} || {path}", Color.magenta);
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                response.ContentLength64 = fileStream.Length;
                if (context.Request.HttpMethod == HttpMethod.Head.Method) {
                    return true;
                }
                
                Span<byte> buffer = new byte[bufferSize];
                using (var outputStream = response.OutputStream) {
                    var bytesLength = 0;
                    while ((bytesLength = fileStream.Read(buffer)) > 0) {
                        outputStream.Write(buffer[..bytesLength]);
                    }
                }
            }
        } else {
            Logger.TraceLog($"Not Exists {nameof(path)} || {path}", Color.red);
            return false;
        }

        return true;
    }
}