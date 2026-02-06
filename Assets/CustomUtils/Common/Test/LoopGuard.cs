using System;

public struct LoopGuard {

    private readonly int _loopLimit;
    
    private int _loopCount;

    public LoopGuard(int loopLimit) {
        _loopLimit = loopLimit;
        _loopCount = 0;
    }

    public void Reset() => _loopCount = 0;
    public void Increase() => _loopCount++;

    public bool IsLimitedLoop() => _loopCount > _loopLimit;
    public bool IsLimitedLoopWithCountIncrease() => _loopCount++ > _loopLimit;

    public void ThrowIfLimitedLoopWithCountIncrease() {
        if (_loopCount++ > _loopLimit) {
            throw new LoopEscapeException(_loopLimit);
        }
    }

    public class LoopEscapeException : Exception {
        
        public LoopEscapeException(int loopLimit) : base($"The loop reached the specified {nameof(loopLimit)}({loopLimit})") { }
    }
}