using System;

public class EditorAsyncOperation : IProgress<float> {
    
    public virtual bool IsDone => Progress >= 1;
    public virtual float Progress { get; protected set; }

    public delegate void CompleteHandler(EditorAsyncOperation operation);
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
}