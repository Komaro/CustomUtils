using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public interface IScenarioAnimationInfo {

    public Task Play() => Task.CompletedTask;
    public Task Play(GameObject go) => Task.CompletedTask;
    public Task Stop() => Task.CompletedTask;
    public Task Stop(GameObject go) => Task.CompletedTask;
}

public interface IScenarioActor {

    string Id { get; }

    Task ChangeVisible(bool isVisible);
    Task ChangePosition(Vector3 position);
    Task ChangeScale(Vector3 scale);

    Task PlayAnimation(IScenarioAnimationInfo info);

    Transform GetTransform();

    void CleanUp();
}

public interface IScenarioInput : IScenarioActor {
    
    void AddOnClickListener(Action onClick);
}

public interface IScenarioPrinter : IScenarioActor {
    
    void OnPrint(string text);
    void OnEnd();
    void OnSkip();

    IScenarioIndicator GetIndicator();
    void SetActiveIndicator(bool isActive);
    
    bool IsTyping();
}

public interface IScenarioIndicator : IScenarioActor { }

public class ScenarioService : IService {

    private string _bodyText;
    private List<string> _textList = new();
    private int _index = 0;

    private Action _callback;
    private Action _exCallback;

    private IScenarioInput _input;
    private IScenarioPrinter _textPrinter;
    private ReactiveCollection<IScenarioActor> _actorList = new ();

    private bool _isPlaying = false;
    private bool _isStop = false;
    private bool _isServing = false;

    private bool _isExecuteCommand;
    private bool _isSkip;
    
    private Task _currentTask;

    public bool IsServing() => _isServing;

    public void Start() {
        _actorList.Clear();
        _isServing = true;
    }

    public void Stop() { }

    public void InitScenario() {
        _bodyText = string.Empty;
        _textList.Clear();
        _index = 0;

        _callback = null;
        _exCallback = null;

        ClearActor();
        RemoveInput();
        RemovePrinter();

        _isPlaying = false;
    }

    public void SetScenario(string bodyText, string splitter = "", Action callback = null, Action exCallback = null) {
        InitScenario();
        
        _index = -1;
        _isStop = false;
        
        _callback = callback;
        _exCallback = exCallback;
        
        if (string.IsNullOrEmpty(bodyText)) {
            _exCallback?.Invoke();
            return;
        }
        
        _bodyText = bodyText;
        GenerateText(bodyText, splitter);
        Logger.TraceLog(string.Join('\n', _textList));
    }

    public void PlayScenario() {
        if (_textList.Count <= 0) {
            Logger.TraceError($"{nameof(_textList)} Count is Zero");
            _exCallback?.Invoke();
            return;
        }

        _isPlaying = true;
        _textPrinter.ChangeVisible(false);
        if (_isStop) {
            _isStop = false;
            NextLine();
        } else {
            _textPrinter.SetActiveIndicator(true);
            NextLine();
        }
    }

    public void StopScenario() => _isStop = true;

    public void EndScenario() {
        _textPrinter?.OnEnd();
        _callback?.Invoke();

        _isPlaying = false;
    }
    
    public void SkipScenario() {
        StopScenario();
        EndScenario();
    }
    
    public void NextLine() {
        _index++;
        
        if (IsValidIndex() == false) {
            EndScenario();
            StopScenario();
            return;
        }

        ExecuteLine(_textList[_index]);
    }

    private async void ExecuteLine(string lineText) {
        Logger.TraceLog(lineText);
        if (lineText.StartsWith('@')) {
            var command = Command.Create(lineText);
            await ExecuteCommand(command);
            if (_isPlaying && IsValidIndex() && _textPrinter.IsTyping() == false) {
                NextLine();
            }
            return;
        }

        _textPrinter.OnPrint(lineText);

        if (_index + 1 == _textList.Count) {
            _textPrinter.SetActiveIndicator(false);
        }
    }

    private async Task ExecuteCommand(Command command) {
        _isSkip = false;
        _isExecuteCommand = true;
        
        _currentTask = command.ExecuteAsync();
        await Task.WhenAll(_currentTask);
        
        _isSkip = false;
        _isExecuteCommand = false;
    }

    public void SkipLine() => _textPrinter.OnSkip();
    public void SkipCommand() => _isSkip = true;

    public void AddInput(IScenarioInput input) {
        _input = input;
        _input?.AddOnClickListener(OnClickEvent);

        switch (input) {
            case IScenarioPrinter printer:
                AddPrinter(printer);
                break;
        }
    }

    public void RemoveInput() {
        _input?.ChangeVisible(false);
        _input = null;
    }

    public IScenarioInput GetInput() => _input;

    public void AddPrinter(IScenarioPrinter textPrinter) => _textPrinter = textPrinter;
    
    public void RemovePrinter() {
        _textPrinter?.ChangeVisible(false);
        _textPrinter = null;
    }

    public bool TryGetPrinter(out IScenarioPrinter printer) {
        printer = GetPrinter();
        return printer != null;
    }
    
    public IScenarioPrinter GetPrinter() => _textPrinter;

    public void AddActor(IScenarioActor actor) => _actorList.Add(actor);
    public void AddActor(params IScenarioActor[] actors) => actors?.ForEach(x => _actorList.Add(x));
    public void AddActor(List<IScenarioActor> actorList) => actorList?.ForEach(x => _actorList.Add(x));
    public void RemoveActor(IScenarioActor actor) => _actorList?.Remove(actor);
    public void ClearActor() => _actorList?.Clear();
    public List<IScenarioActor> GetActor() => _actorList?.ToList();

    public List<IScenarioActor> GetAllActor() {
        var list = new List<IScenarioActor> { _textPrinter, _textPrinter };
        list.AddRange(_actorList);
        return list;
    }

    private void GenerateText(string bodyText, string splitter) {
        if (string.IsNullOrEmpty(splitter)) {
            splitter = "\n";
        }

        _textList = bodyText.Split(splitter).ToList();
    }

    private void OnClickEvent() {
        if (_isStop || _isSkip) {
            return;
        }
        
        if (_textPrinter.IsTyping()) {
            SkipLine();
            return;
        }

        if (_currentTask?.IsCompleted == false) {
            SkipCommand();
            return;
        }

        NextLine();
    }

    public bool IsValidSkip() => _isExecuteCommand && _isSkip;
    private bool IsValidIndex() => _index + 1 <= _textList.Count;
    public bool IsPlaying() => _isPlaying;
}
