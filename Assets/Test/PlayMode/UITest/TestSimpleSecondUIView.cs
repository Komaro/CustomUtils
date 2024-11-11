
[UIView("TestViewModel_Second", priority = 1)]
public class TestSimpleSecondUIView : UIView<TestSimpleUIViewModel> {

    protected override void OnNotifyModelChanged(string fieldName, NotifyFieldChangedEventArgs args) => Logger.TraceLog($"{fieldName} || {args.GetType().Name}");
}
