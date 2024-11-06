using System;
using UnityEngine;

public interface IUIOptionExecute {

    void Execute(Enum type, UIOptionAttribute attribute, GameObject ui) {
        switch (type) {
            case SAMPLE_UI_OPTION_EXECUTE_TYPE.LOAD:
                LoadExecute(attribute, ui);
                break;
            case SAMPLE_UI_OPTION_EXECUTE_TYPE.OPEN:
                OpenExecute(attribute, ui);
                break;
            case SAMPLE_UI_OPTION_EXECUTE_TYPE.CLOSE:
                CloseExecute(attribute, ui);
                break;
        }
    }
    
    void LoadExecute(UIOptionAttribute attribute, GameObject ui);
    void OpenExecute(UIOptionAttribute attribute, GameObject ui);
    void CloseExecute(UIOptionAttribute attribute, GameObject ui);
}

public class UIOptionExecuteAttribute : Attribute {
    
    public Enum type;

    public UIOptionExecuteAttribute(Enum type) => this.type = type;
}

public enum SAMPLE_UI_OPTION_EXECUTE_TYPE {
    NONE,
    LOAD,
    OPEN,
    CLOSE,
}