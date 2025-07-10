using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitInstruction : CustomYieldInstruction {

    private bool _isDone = false;
    private readonly IEnumerator _enumerator;
    
    public override bool keepWaiting => _isDone == false;

    public WaitInstruction(IEnumerator enumerator) => _enumerator = enumerator;

    public WaitInstruction Run(MonoBehaviour mono) {
        if (_enumerator != null) {
            mono.StartCoroutine(RunCoroutine(mono));
        }

        return this;
    }
    
    private IEnumerator RunCoroutine(MonoBehaviour mono) {
        yield return mono.StartCoroutine(_enumerator);
        _isDone = true;
    }
}

public class WaitAllInstruction : CustomYieldInstruction {

    private readonly List<CustomYieldInstruction> _instructionList = new();
    
    public override bool keepWaiting {
        get {
            foreach (var instruction in _instructionList) {
                if (instruction.keepWaiting) {
                    return true;
                }
            }

            return false;
        }
    }

    public void Add(CustomYieldInstruction instruction) => _instructionList.Add(instruction);

    public void Clear() => _instructionList.Clear();
}
