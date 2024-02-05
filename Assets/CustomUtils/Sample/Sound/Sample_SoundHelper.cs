using System;
using UniRx;
using UnityEngine;

public class Sample_SoundHelper : MonoBehaviour {
    
    public string commandLine = "";
    
    public SAMPLE_FX_PLAY_TYPE playType = SAMPLE_FX_PLAY_TYPE.INSTANT;

    public float delay = 0;

    private Command command;
    private bool _isStop = false;
    
    private void OnEnable() {
        if (_isStop == false && playType != SAMPLE_FX_PLAY_TYPE.MANUAL) {
            PlaySound();
        }
    }

    private void OnDisable() {
        if (playType == SAMPLE_FX_PLAY_TYPE.ENABLE_AND_AUTO_DISABLE) {
            StopSound();
        }
    }

    // Function Call
    public void PlaySound(string commandLine) {
        this.commandLine = commandLine;
        PlaySound();
    }

    public void PlaySound() {
        if (string.IsNullOrEmpty(commandLine) == false) {
            command = Command.Create(commandLine);
            switch (playType) {
                case SAMPLE_FX_PLAY_TYPE.DELAY:
                    delay = Mathf.Max(0, delay);
                    Observable.EveryLateUpdate().Delay(TimeSpan.FromSeconds(delay)).Subscribe(_ => {
                        command.ExecuteAsync();
                        _isStop = true;
                    });
                    break;
                case SAMPLE_FX_PLAY_TYPE.ENABLE:
                case SAMPLE_FX_PLAY_TYPE.MANUAL:
                    command.ExecuteAsync();
                    break;
                default:
                    command.ExecuteAsync();
                    _isStop = true;
                    break;
            }
        }
    }

    // Function Call
    public void StopSound(string commandLine) {
        this.commandLine = commandLine;
        StopSound();
    }
    
    public void StopSound() {
        if (string.IsNullOrEmpty(commandLine) == false) {
            command ??= Command.Create(commandLine);
            command.UndoAsync();
            
            switch (playType) {
                case SAMPLE_FX_PLAY_TYPE.ENABLE:
                case SAMPLE_FX_PLAY_TYPE.MANUAL:
                    break;
                default:
                    _isStop = true;
                    break;
            }
        }
    }
}

public enum SAMPLE_FX_PLAY_TYPE {
    NONE,
    INSTANT,                    // 한 번 출력
    DELAY,                      // delay(sec) 시간 후 한 번 출력
    MANUAL,                     // 자동으로 출력하지 않음
    ENABLE,                     // 매 OnEnable 마다 출력
    ENABLE_AND_AUTO_DISABLE,    // 매 OnEnable 마다 출력하고 OnDisable 에서 출력을 멈춤(구현되어 있는 경우)
}
