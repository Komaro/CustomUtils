using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


public class SimpleHttpServer : IDisposable {
    
    private HttpListener _listener = new();
    private Task _listenerTask;
    private CancellationTokenSource _listenerCancelToken;
    private Dictionary<Type, HttpServeModule> _serveModuleDic = new();

    private string _targetDirectory;

    public SimpleHttpServer(string prefix, HttpServeModule module) : this(prefix) => AddServeModule(module);
    public SimpleHttpServer(string prefix) => _listener.Prefixes.Add(prefix);
    public SimpleHttpServer() : this(Constants.Network.DEFAULT_LOCAL_HOST) { }
    
    ~SimpleHttpServer() => Dispose();
    
    public void Dispose() {
        if (IsRunning()) {
            Close();
        }
    }
    
    public void Start(string targetDirectory) {
        _targetDirectory = targetDirectory;
        Start();
    }
    
    public void Start() {
        if (IsRunning()) {
            Logger.TraceLog($"{nameof(_listener)} is Already Listening", Color.yellow);
            return;
        }
        
        _listener.Start();
        try {
            _listenerCancelToken = new CancellationTokenSource();
            _ = Task.Run(() => Run(_listenerCancelToken.Token), _listenerCancelToken.Token);
            Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Start)} || {_listener.Prefixes.ToStringCollection(", ")}", Color.green);
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
        Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Stop)}", Color.yellow);
    }

    public void Close() {
        if (IsRunning()) {
            Stop();
        }
        
        _listener.Close();
        Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Close)}", Color.red);
    }

    private async void Run(CancellationToken token) {
        try {
            while (_listener.IsListening && token.IsCancellationRequested == false) {
                try {
                    var task = _listener.GetContextAsync();
                    task.Wait(token);
                    if (token.IsCancellationRequested) {
                        Logger.TraceLog($"Receive Cancellation Request", Color.red);
                        break;
                    }

                    var content = await task;
                    _ = Task.Run(() => Serve(content, token), token);
                } catch (OperationCanceledException) {
                    Logger.TraceLog(nameof(OperationCanceledException), Color.red);
                    break;
                } catch (HttpListenerException ex) {
                    Logger.TraceError(ex);
                    break;
                } catch (InvalidOperationException ex) {
                    Logger.TraceError(ex);
                    break;
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        } finally {
            if (_listener.IsListening) {
                _listener.Stop();
                _listener.Close();
                Logger.TraceLog($"{nameof(SimpleHttpServer)} {nameof(Exception)} {nameof(Close)}", Color.red);
            }
        }
    }

    private void Serve(HttpListenerContext context, CancellationToken token) {
        try {
            if (_serveModuleDic.Any() == false) {
                throw new NoServeModuleException();
            }

            token.ThrowIfCancellationRequested();

            if (context.Request != null) {
                foreach (var module in _serveModuleDic.Values) {
                    if (module.Serve(context)) {
                        break;
                    }
                }
            }
        } catch (Exception ex) {
            context.Response.StatusCode = (int) HttpStatusCode.NotFound;
            Logger.TraceError(ex);
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
            Logger.TraceLog($"Already {nameof(HttpServeModule)}. {nameof(type)} : {nameof(type.Name)}", Color.yellow);
            return;
        }
        
        if (Activator.CreateInstance(type) is HttpServeModule module) {
            module.AddServer(this);
            _serveModuleDic.Add(type, module);
            Logger.TraceLog($"Add {type.Name}", Color.cyan);
        }
    }

    public void AddServeModule(HttpServeModule module) {
        if (module == null) {
            Logger.TraceError($"{nameof(module)} is Null");
            return;
        }
        
        var type = module.GetType();
        if (_serveModuleDic.ContainsKey(type)) {
            Logger.TraceLog($"Already {nameof(HttpServeModule)}. {nameof(module)} : {nameof(type.Name)}", Color.yellow);
            return;
        }
        
        module.AddServer(this);
        _serveModuleDic.Add(type, module);
        Logger.TraceLog($"Add {type.Name}", Color.cyan);
    }

    public void RemoveServeModule<T>() where T : HttpServeModule => RemoveServeModule(typeof(T));
    
    public void RemoveServeModule(Type type) {
        if (_serveModuleDic.TryGetValue(type, out var module)) {
            module.Close();
            _serveModuleDic.AutoRemove(type);
            Logger.TraceLog($"Remove {type.Name}", Color.red);
        }
    }

    public void ClearServeModule() {
        _serveModuleDic.SafeClear(module => module.Close());
        Logger.TraceLog("Clear Serve Module", Color.red);
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
