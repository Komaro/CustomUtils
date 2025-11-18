using System;
using System.Threading.Tasks;

public class AsyncCustomOperation : IProgress<float> {

    public virtual bool IsDone => Progress >= 1;
    public virtual bool IsCanceled => false;
    public virtual bool IsFailed => false;

    public virtual float Progress { get; protected set; }
    public virtual string ProgressDisplay => ((int) Progress).ToString();

    public virtual float Percentage => Progress * 100f;
    public virtual string PercentageDisplay => ((int) Percentage).ToString();

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
        onComplete += _ => completionSource.SetResult(true);
        return completionSource.Task;
    }
}
