using System;
using System.Collections;
using UnityEngine;
using Color = System.Drawing.Color;

#if UNITY_6000_0_OR_NEWER == false
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
#endif

public class AsyncCustomOperation : IEnumerator, IProgress<float>
{
    public virtual bool IsDone => Status is OperationStatus.SUCCESS or OperationStatus.CANCELED or OperationStatus.EXCEPTION or OperationStatus.FAILED;

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
    protected CompleteHandler onComplete;

    public event CompleteHandler OnComplete
    {
        add
        {
            if (IsDone)
                value.Invoke(this);
            else
                onComplete += value;
        }
        
        remove => onComplete -= value;
    }

    public delegate void ProgressHandler(float progress);
    public ProgressHandler OnProgress;

    public virtual void Init()
    {
        Progress = 0f;
        Status = OperationStatus.NONE;
    }

    public virtual void Done() => Report(1f);

    public virtual void Clear()
    {
        onComplete = null;
        OnProgress = null;
    }

    public virtual void Report(float value)
    {
        if (IsDone)
            return;

        if (value > Progress)
            OnProgress?.Invoke(Progress);

        Progress = value;
        if (Progress >= 1f)
            Complete(OperationStatus.SUCCESS);
    }

    public virtual void Report(int value, int totalValue)
    {
        if (value <= 0 || totalValue <= 0)
            throw new DivideByZeroException($"{nameof(value)} = {value} || {nameof(totalValue)} = {totalValue}");

        Report(value / (float)totalValue);
    }

    public virtual void Cancel() => Complete(OperationStatus.CANCELED);
    public virtual void Fail() => Complete(OperationStatus.FAILED);

    public virtual void Exception(params Exception[] exceptions) 
    {
        Exceptions = exceptions;
        Complete(OperationStatus.EXCEPTION);
    }

    protected virtual void Complete(OperationStatus statue)
    {
        if (IsDone)
        {
            Logger.TraceLog($"Already completed || {Status}", Color.Yellow);
            return;
        }

        Status = statue;

        var complete = onComplete;
        Clear();
        complete?.Invoke(this);
    }
    
#if UNITY_6000_0_OR_NEWER
    
    public virtual async Awaitable ToAwaitable()
    {
        var completionSource = new AwaitableCompletionSource<bool>();
        if (IsDone)
            CompleteAwaitableFromStatus(this, completionSource);
        else
            onComplete += operation => CompleteAwaitableFromStatus(operation, completionSource);
        
        await completionSource.Awaitable;
    }

    protected virtual void CompleteAwaitableFromStatus(AsyncCustomOperation operation, AwaitableCompletionSource<bool> completionSource)
    {
        switch (operation.Status) 
        {
            case OperationStatus.CANCELED:
            case OperationStatus.FAILED:
                completionSource.TrySetCanceled();
                break;
            case OperationStatus.EXCEPTION:
                completionSource.TrySetException(operation.Exceptions?[0] ?? new Exception());
                break;
            default:
                completionSource.TrySetResult(true);
                break;
        }
    }
    
#else

    public virtual Task ToTask() 
    {
        var completionSource = new TaskCompletionSource<bool>();
        if (IsDone)
            CompleteTaskFromStatus(this, completionSource);
        else
            onComplete += operation => CompleteTaskFromStatus(operation, completionSource);
        
        return completionSource.Task;
    }

    protected virtual void CompleteTaskFromStatus(AsyncCustomOperation operation, TaskCompletionSource<bool> completionSource) 
    {
        switch (operation.Status) 
        {
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
    
#endif
    
    public virtual IEnumerator ToEnumerator()
    {
        while (IsDone == false)
            yield return null;
    }

    public virtual IEnumerator ToEnumerator(IEnumerator enumerator)
    {
        while (IsDone == false)
            yield return enumerator;
    }

    bool IEnumerator.MoveNext() => IsDone == false;
    void IEnumerator.Reset() => throw new NotSupportedException();
    object IEnumerator.Current => null;
}

public class AsyncCustomOperation<TValue> : AsyncCustomOperation
{
    private TValue _result;

    public TValue Result
    {
        get
        {
            if (IsDone == false)
                throw new InvalidOperationException($"{nameof(AsyncCustomOperation<TValue>)} already completed");

            return _result ?? throw new NullReferenceException($"{nameof(_result)}({nameof(TValue)}) is null. You must call {nameof(Complete)} to provide the result before accessing {nameof(Result)}");
        }

        protected set => _result = value;
    }

    public void Complete(TValue result)
    {
        Result = result;
        if (IsDone == false)
            Done();
    }
}

public static class AsyncCustomOperationAwaiter
{
#if UNITY_6000_0_OR_NEWER
    public static Awaitable.Awaiter GetAwaiter(this AsyncCustomOperation operation) => operation.ToAwaitable().GetAwaiter();
#else
    public static TaskAwaiter GetAwaiter(this AsyncCustomOperation operation) => operation.ToTask().GetAwaiter();
#endif
}

public enum OperationStatus 
{
    NONE,
    SUCCESS,
    CANCELED,
    FAILED,
    EXCEPTION,
}