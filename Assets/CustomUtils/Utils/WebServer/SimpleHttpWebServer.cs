using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;


public class SimpleHttpWebServer : IDisposable {
    
    private HttpListener _listener = new();
    private CancellationTokenSource _listenerCancelToken = new();
    private ConcurrentBag<Thread> _activeThreadBag = new();

    private Dictionary<Type, HttpServeModule> _serveModuleDic = new();

    private string _targetDirectory;

    public SimpleHttpWebServer(string prefix, HttpServeModule module) : this(prefix) => AddServeModule(module);
    public SimpleHttpWebServer(string prefix) => _listener.Prefixes.Add(prefix);
    public SimpleHttpWebServer() : this(Constants.Network.DEFAULT_LOCAL_HOST) { }

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
        if (_activeThreadBag.IsEmpty == false) {
            _activeThreadBag.Clear();
        }

        ThreadPool.QueueUserWorkItem(_ => Run(_listenerCancelToken.Token));
        Logger.TraceLog($"{nameof(SimpleHttpWebServer)} {nameof(Start)} || {_listener.Prefixes.ToStringCollection(", ")}", Color.green);
    }

    public void Restart() {
        Stop();
        Start();
    }

    public void Stop() {
        ClearThread();
        _listenerCancelToken.Cancel();
        _listener.Stop();
        Logger.TraceLog($"{nameof(SimpleHttpWebServer)} {nameof(Stop)}", Color.yellow);
    }

    public void Close() {
        if (IsRunning()) {
            Stop();
        }
        
        _listener.Close();
        Logger.TraceLog($"{nameof(SimpleHttpWebServer)} {nameof(Close)}", Color.red);
    }

    private void Run(CancellationToken token) {
        while (_listener.IsListening && token.IsCancellationRequested == false) {
            try {
                var getTask = _listener.GetContextAsync();
                getTask.Wait(token);

                if (token.IsCancellationRequested) {
                    Logger.TraceLog($"Receive Cancellation Request", Color.red);
                    break;
                }

                var thread = new Thread(() => Serve(getTask.Result));
                thread.Start();
                _activeThreadBag.Add(thread);
            } catch (OperationCanceledException ex) {
            } catch (HttpListenerException ex) {
                Logger.TraceError(ex);
            } catch (InvalidOperationException ex) {
                Logger.TraceError(ex);
            } catch (Exception ex) {
                Logger.TraceError(ex);
            }
        }
    }

    private void Serve(HttpListenerContext context) {
        try {
            if (_serveModuleDic.Any() == false) {
                throw new NoServeModuleException();
            }

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

    private void ClearThread() {
        while (_activeThreadBag.IsEmpty == false) {
            if (_activeThreadBag.TryTake(out var result) && result is { IsAlive: true }) {
                result.Join(10000);
            }
        }
        
        Logger.TraceLog($"{nameof(SimpleHttpWebServer)} Thread Clear.", Color.yellow);
    }

    public void Dispose() {
        if (IsRunning()) {
            Close();
        }

        if (_activeThreadBag != null && _activeThreadBag.IsEmpty != false) {
            ClearThread();
        }
    }

    public string GetURL() => _listener.Prefixes.First();
    public string GetTargetDirectory() => _targetDirectory;
    public int GetRunningThreadCount() => _activeThreadBag.Count(x => x.IsAlive);
    public List<Type> GetServeModuleTypeList() => _serveModuleDic.Keys.ToList();
    public bool IsRunning() => _listener?.IsListening ?? false;
    public bool IsContainsServeModule(Type type) => _serveModuleDic.ContainsKey(type);
    
    
    private class NoServeModuleException : SystemException {
    
        public NoServeModuleException() : base($"Currently, {nameof(HttpServeModule)} does not exist.") { }
    }
}
