public abstract class SwitchText<T> {

    public T SwitchValue { get; set; }
    public abstract string Text { get; }

    public static implicit operator string(SwitchText<T> switchText) => switchText.Text;

    protected SwitchText(T defaultSwitchValue) => SwitchValue = defaultSwitchValue;
}

public class BoolSwitchText : SwitchText<bool> {

    public override string Text => SwitchValue ? _trueValue : _falseValue;

    private readonly string _trueValue;
    private readonly string _falseValue;

    public BoolSwitchText(string trueValue, string falseValue) : base(false) {
        _trueValue = trueValue;
        _falseValue = falseValue;
    }
}