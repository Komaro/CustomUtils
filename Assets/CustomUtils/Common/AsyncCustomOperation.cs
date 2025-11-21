using System;
using System.Collections;
using System.Threading.Tasks;

public interface IAsyncCustomOperation : IEnumerator, IProgress<float> {
    
    public bool IsDone { get; }
    public bool IsCanceled { get; }
    public bool IsFailed { get; }
    
    public void Init();
    public void Done();
    
    public Task ToTask();
    public IEnumerator ToCoroutine();
}

public class AsyncCustomOperation : IAsyncCustomOperation {

    public virtual bool IsDone => Progress >= 1;
    public virtual bool IsCanceled => false;
    public virtual bool IsFailed => false;

    public virtual float Progress { get; protected set; }
    public virtual string ProgressDisplay => ((int)Progress).ToString();

    public virtual float Percentage => Progress * 100f;
    public virtual string PercentageDisplay => ((int)Percentage).ToString();

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

    public virtual void Init() => Progress = 0f;
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
            onComplete.Handler?.Invoke(this);
        }
    }

    public virtual void Report(int value, int totalValue) {
        if (value <= 0 || totalValue <= 0) {
            throw new DivideByZeroException($"{nameof(value)} = {value} || {nameof(totalValue)} = {totalValue}");
        }

        Report(value / (float)totalValue);
    }

    public virtual Task ToTask() {
        var completionSource = new TaskCompletionSource<bool>();
        if (IsDone) {
            completionSource.SetResult(true);
        } else {
            onComplete += operation => {
                if (operation.IsCanceled) {
                    completionSource.SetCanceled();
                } else {
                    completionSource.TrySetResult(true);
                }
            };
        }
        
        return completionSource.Task;
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

    public override void Report(float value) {
        if (IsDone) {
            return;
        }

        if (value > Progress) {
            OnProgress.Handler?.Invoke(Progress);
        }

        Progress = value;

        if (IsDone) {
            onComplete.Handler?.Invoke(this);
        }
    }

    public void Complete(TValue result) {
        Result = result;
        if (IsDone == false) {
            Done();
        }
    }
}