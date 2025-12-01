using System;
using System.Buffers;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public abstract class HttpServeModule {

    protected SimpleHttpServer server;

    public void AttachServer(SimpleHttpServer server) => this.server = server;
    
    public abstract bool Serve(HttpListenerContext context);

    public virtual async Task<bool> ServeAsync(HttpListenerContext context, CancellationToken token) {
        await Task.CompletedTask;
        return false;
    }

    public virtual void Close() { }
}

public class AssetBundleDistributionServeModule : HttpServeModule {

    private const int bufferSize = 1024 * 32;
    
    public override bool Serve(HttpListenerContext context) {
        if (context.Request.HttpMethod == HttpMethod.Head.Method) {
            return false;
        }

        var path = Path.Combine(server.GetTargetDirectory(), context.Request.RawUrl.TrimStart('/'));
        if (File.Exists(path) == false) {
            Logger.TraceLog($"Not exists {nameof(path)} || {path}", Color.Red);
            return false;
        }
        
        Logger.TraceLog($"Serve || {context.Request.HttpMethod} || {path}", Color.Magenta);
        using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            context.Response.ContentLength64 = fileStream.Length;
            context.Response.StatusCode = (int) HttpStatusCode.OK;

            Span<byte> buffer = new byte[bufferSize];
            var bytesLength = 0;
            while ((bytesLength = fileStream.Read(buffer)) > 0) {
                context.Response.OutputStream.Write(buffer[..bytesLength]);
            }
        }
        
        return true;
    }

    public override async Task<bool> ServeAsync(HttpListenerContext context, CancellationToken token) {
        if (context.Request.HttpMethod == HttpMethod.Head.Method) {
            return false;
        }

        var path = Path.Combine(server.GetTargetDirectory(), context.Request.RawUrl.TrimStart('/'));
        if (File.Exists(path) == false) {
            Logger.TraceLog($"'{path}' is not exists path", Color.Yellow);
            return false;
        }
        
        using (var owner = MemoryPool<byte>.Shared.Rent(bufferSize))
        await using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            context.Response.ContentLength64 = fileStream.Length;
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            var byteLength = 0;
            var buffer = owner.Memory;
            while ((byteLength = await fileStream.ReadAsync(buffer, token)) > 0) {
                await context.Response.OutputStream.WriteAsync(buffer[..byteLength], token);
            }
        }

        return true;
    } 
}