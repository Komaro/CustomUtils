using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityEngine;
using Color = System.Drawing.Color;

public class SimpleHttpServer : IDisposable {
    
    private HttpListener _listener = new();
    private Task _listenerTask;
    private CancellationTokenSource _listenerCancelToken;
    private readonly Dictionary<Type, HttpServeModule> _serveModuleDic = new();

    private string _targetDirectory;

    public SimpleHttpServer(string prefix, HttpServeModule module) : this(prefix) => AddServeModule(module);
    public SimpleHttpServer(string prefix) => _listener.Prefixes.Add(prefix);
    public SimpleHttpServer() : this(Constants.Network.DEFAULT_LOCAL_HOST) { }
    
    private bool _isDisposed;
    
    ~SimpleHttpServer() => Dispose();

    public void Dispose() {
        if (_isDisposed) {
            return;
        }
        
        if (IsRunning()) {
            Close();
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
    
    public void Start(string targetDirectory) {
        _targetDirectory = targetDirectory;
        Start();
    }
    
    public void Start() {
        if (IsRunning()) {
            Logger.TraceLog($"{nameof(_listener)} is Already Listening", Color.Yellow);
            return;
        }
        
        _listener.Start();
        try {
            _listenerCancelToken = new CancellationTokenSource();
            _ = Task.Run(() => Run(_listenerCancelToken.Token), _listenerCancelToken.Token);
            Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Start)} || {_listener.Prefixes.ToStringCollection(", ")}", Color.GreenYellow);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public void Restart() {
        Stop();
        Start();
    }

    public void Stop() {
        if (_listenerCancelToken.IsCancellationRequested == false) {
            _listenerCancelToken.Cancel(); 
            _listenerCancelToken.Dispose();
        }

        _listener.Stop();
        Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Stop)}", Color.Yellow);
    }

    public void Close() {
        if (IsRunning()) {
            Stop();
        }
        
        _listener.Close();
        Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Close)}", Color.Red);
    }

    private async Task Run(CancellationToken token) {
        try {
            while (_listener.IsListening && token.IsCancellationRequested == false) {
                try {
                    var task = _listener.GetContextAsync();
                    task.Wait(token);
                    token.ThrowIfCancellationRequested();

                    var content = await task;
                    _ = Task.Run(() => Serve(content, token), token);
                } catch (OperationCanceledException) {
                    Logger.TraceLog($"Receive {nameof(OperationCanceledException)}", Color.Red);
                    break;
                } catch (HttpListenerException ex) {
                    Logger.TraceError(ex);
                    break;
                } catch (InvalidOperationException ex) {
                    Logger.TraceError(ex);
                    break;
                } catch (Exception ex){
                    Logger.TraceLog(ex, Color.Yellow);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            if (_listener.IsListening) {
                _listener.Stop();
                _listener.Close();
                Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Exception)} {nameof(Close)}", Color.Red);
            }
        }
    }

    private readonly Channel<HttpListenerContext> _requestChannel = Channel.CreateBounded<HttpListenerContext>(200);
    
    private async Task AcceptRequestAsync(CancellationToken token) {
        try {
            while (_listener.IsListening && token.IsCancellationRequested == false) {
                try {
                    var task = _listener.GetContextAsync();
                    var context = await task;
                    token.ThrowIfCancellationRequested();
                    await _requestChannel.Writer.WriteAsync(context, token);
                } catch (OperationCanceledException) {
                    Logger.TraceLog($"Receive {nameof(OperationCanceledException)}", Color.Red);
                    break;
                } catch (HttpListenerException ex) {
                    Logger.TraceError(ex);
                    break;
                } catch (InvalidOperationException ex) {
                    Logger.TraceError(ex);
                    break;
                } catch (Exception ex){
                    Logger.TraceLog(ex, Color.Yellow);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            if (_listener.IsListening) {
                _listener.Stop();
                _listener.Close();
                Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Exception)} {nameof(Close)}", Color.Red);
            }
        }
    }

    private async Task HandleRequestAsync(CancellationToken token) {
        try {
            while (_listener.IsListening && token.IsCancellationRequested == false) {
                var context = await _requestChannel.Reader.ReadAsync(token);
                token.ThrowIfCancellationRequested();
                _ = Task.Run(() => Serve(context, token), token);
            }
        }catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    // TODO. StatusCode 후처리 수정 필요. 정상 처리 되어 write 처리된 경우 Header를 더이상 수정할 수 없기 때문에 일괄적으로 처리하기 어려움 다른 방안 혹은 반환 처리를 통합하는 방안 강구 필요
    private void Serve(HttpListenerContext context, CancellationToken token) {
        try {
            if (_serveModuleDic.Count <= 0) {
                throw new NoServeModuleException();
            }

            token.ThrowIfCancellationRequested();
            
            if (context.Request != null) {
                if (context.Request.HttpMethod == HttpMethod.Head.Method) {
                    return;
                }
                
                foreach (var module in _serveModuleDic.Values) {
                    if (module.Serve(context)) {
                        return;
                    }
                }
            }
            
            context.Response.StatusCode = (int) HttpStatusCode.NotFound;
        } catch (Exception ex) {
            Logger.TraceError(ex);
            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
        } finally {
            context.Response.Close();
        }
    }

    public void AddServeModule<T>() where T : HttpServeModule => AddServeModule(typeof(T));

    public void AddServeModule(Type type) {
        if (type == null) {
            Logger.TraceError($"{nameof(type)} is Null");
            return;
        }
        
        if (_serveModuleDic.ContainsKey(type)) {
            Logger.TraceLog($"Already {nameof(HttpServeModule)}. {nameof(type)} : {nameof(type.Name)}", Color.Yellow);
            return;
        }
        
        if (Activator.CreateInstance(type) is HttpServeModule module) {
            module.AttachServer(this);
            _serveModuleDic.Add(type, module);
            Logger.TraceLog($"Add {type.Name}", Color.Cyan);
        }
    }

    public void AddServeModule(HttpServeModule module) {
        if (module == null) {
            Logger.TraceError($"{nameof(module)} is Null");
            return;
        }
        
        var type = module.GetType();
        if (_serveModuleDic.ContainsKey(type)) {
            Logger.TraceLog($"Already {nameof(HttpServeModule)}. {nameof(module)} : {nameof(type.Name)}", Color.Yellow);
            return;
        }
        
        module.AttachServer(this);
        _serveModuleDic.Add(type, module);
        Logger.TraceLog($"Add {type.Name}", Color.Cyan);
    }

    public void RemoveServeModule<T>() where T : HttpServeModule => RemoveServeModule(typeof(T));
    
    public void RemoveServeModule(Type type) {
        if (_serveModuleDic.TryGetValue(type, out var module)) {
            module.Close();
            _serveModuleDic.AutoRemove(type);
            Logger.TraceLog($"Remove {type.Name}", Color.Red);
        }
    }

    public void ClearServeModule() {
        _serveModuleDic.SafeClear(module => module.Close());
        Logger.TraceLog("Clear Serve Module", Color.Red);
    }

    public string GetURL() => _listener?.Prefixes.FirstOrDefault() ?? string.Empty;
    public string GetTargetDirectory() => _targetDirectory;
    public List<Type> GetServeModuleTypeList() => _serveModuleDic.Keys.ToList();
    public bool IsRunning() => _listener?.IsListening ?? false;
    public bool IsContainsServeModule(Type type) => _serveModuleDic.ContainsKey(type);
    
    private class NoServeModuleException : SystemException {
    
        public NoServeModuleException() : base($"Currently, {nameof(HttpServeModule)} does not exist.") { }
    }
}