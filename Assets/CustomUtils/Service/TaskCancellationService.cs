using System;
using System.Threading;
using System.Threading.Tasks;

public class TaskCancellationService : IService {

    private CancellationTokenSource _instantTokenSource;
    private CancellationToken _instantToken;

    void IService.Start() { }

    void IService.Stop() {
        if (_instantTokenSource?.IsCancellationRequested ?? false) {
            _instantTokenSource.Cancel();
            _instantTokenSource.Dispose();
        }
    }
    
    public void SubscribeInstantToken(out CancellationToken token, out Task cancelTask, Action onRegister) {
        if (_instantTokenSource?.IsCancellationRequested ?? false) {
            _instantTokenSource.Cancel();
            _instantTokenSource.Dispose();
        }
        
        _instantTokenSource = new CancellationTokenSource();
        _instantToken = _instantTokenSource.Token;
        _instantToken.Register(onRegister);

        token = _instantToken;
        cancelTask = CreateInstantCancelTask();
    }

    public void CancelInstantToken(float delay = 0f) {
        if (delay <= 0) {
            _instantTokenSource?.Cancel();
        } else {
            _instantTokenSource?.CancelAfter(TimeSpan.FromSeconds(delay));
        }
    }

    public async Task<bool> TryRunTask(Task cancelTask, Task executeTask, Action onCancel = null) {
        var waiter = await Task.WhenAny(cancelTask, executeTask);
        if (waiter == cancelTask) {
            onCancel?.Invoke();
            return false;
        }
        
        return true;
    }

    private Task CreateInstantCancelTask() {
        return Task.Run(() => {
            while (true) {
                if (_instantTokenSource.IsCancellationRequested || _instantToken.IsCancellationRequested) {
                    return;
                }
            }
        }, _instantToken);
    }
}
