using System;
using System.Collections;
using System.Threading.Tasks;

public class AsyncCustomOperation : IEnumerator, IProgress<float> {

    public virtual bool IsDone => Status != OperationStatus.NONE;
    
    public OperationStatus Status { get; protected set; }

    public virtual bool Success => Status == OperationStatus.SUCCESS;
    public virtual bool Canceled => Status == OperationStatus.CANCELED;
    public virtual bool Failed => Status == OperationStatus.FAILED;
    public virtual bool Excepted => Status == OperationStatus.EXCEPTION;

    public virtual float Progress { get; protected set; }
    public virtual string ProgressDisplay => ((int)Progress).ToString();

    public virtual float Percentage => Progress * 100f;
    public virtual string PercentageDisplay => ((int)Percentage).ToString();
    
    public Exception[] Exceptions { get; protected set; }

    public delegate void CompleteHandler(AsyncCustomOperation operation);

    protected SafeDelegate<CompleteHandler> onComplete;

    public event CompleteHandler OnComplete {
        add {
            if (IsDone) {
                value.Invoke(this);
            } else {
                onComplete += value;
            }
        }
        remove => onComplete -= value;
    }

    public delegate void ProgressHandler(float progress);

    public SafeDelegate<ProgressHandler> OnProgress;

    public virtual void Init() {
        Progress = 0f;
        Status = OperationStatus.NONE;
    }

    public virtual void Done() => Report(1f);

    public virtual void Clear() {
        onComplete.Clear();
        OnProgress.Clear();
    }

    public virtual void Report(float value) {
        if (IsDone) {
            return;
        }

        if (value > Progress) {
            OnProgress.Handler?.Invoke(Progress);
        }

        Progress = value;

        if (IsDone) {
            Status = OperationStatus.SUCCESS;
            onComplete.Handler?.Invoke(this);
        }
    }

    public virtual void Report(int value, int totalValue) {
        if (value <= 0 || totalValue <= 0) {
            throw new DivideByZeroException($"{nameof(value)} = {value} || {nameof(totalValue)} = {totalValue}");
        }

        Report(value / (float)totalValue);
    }

    public virtual void Cancel() => Status = OperationStatus.CANCELED;
    public virtual void Fail() => Status = OperationStatus.FAILED;

    public virtual void Exception(params Exception[] exceptions) {
        Status = OperationStatus.EXCEPTION;
        Exceptions = exceptions;
    }
    
    public virtual Task ToTask() {
        var completionSource = new TaskCompletionSource<bool>();
        if (IsDone) {
            CompleteTaskFromStatus(this, completionSource);
        } else {
            onComplete += operation => CompleteTaskFromStatus(operation, completionSource);
        }
        
        return completionSource.Task;
    }

    protected virtual void CompleteTaskFromStatus(AsyncCustomOperation operation, TaskCompletionSource<bool> completionSource) {
        switch (operation.Status) {
            case OperationStatus.CANCELED:
            case OperationStatus.FAILED:
                completionSource.TrySetCanceled();
                break;
            case OperationStatus.EXCEPTION:
                completionSource.TrySetException(operation.Exceptions);
                break;
            default:
                completionSource.TrySetResult(true);
                break;
        }
    }

    public virtual IEnumerator ToCoroutine() {
        while (IsDone == false) {
            yield return null;
        }
    }

    public virtual IEnumerator ToCoroutine(IEnumerator enumerator) {
        while (IsDone == false) {
            yield return enumerator;
        }
    }

    bool IEnumerator.MoveNext() => IsDone;
    void IEnumerator.Reset() => Init();
    object IEnumerator.Current => this;
}

public class AsyncCustomOperation<TValue> : AsyncCustomOperation {

    private TValue _result;
    
    public TValue Result {
        get {
            if (IsDone == false) {
                throw new InvalidOperationException($"{nameof(AsyncCustomOperation<TValue>)} already completed");
            }

            return _result ?? throw new NullReferenceException<TValue>(nameof(AsyncCustomOperation<TValue>));
        }

        protected set => _result = value;
    }

    public void Complete(TValue result) {
        Result = result;
        if (IsDone == false) {
            Done();
        }
    }
}

public enum OperationStatus {
    NONE,
    SUCCESS,
    CANCELED,
    FAILED,
    EXCEPTION,
}