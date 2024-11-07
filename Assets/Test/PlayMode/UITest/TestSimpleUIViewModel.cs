public class TestSimpleUIViewModel : UIViewModel {

    public readonly NotifyProperty<string> Title = new();
    public readonly NotifyProperty<int> Count = new();

    public readonly NotifyCollection<int> Collection = new();
    public readonly NotifyList<long> List = new();
    public readonly NotifyDictionary<int, long> Dictionary = new ();
    
    public void IncreaseCount(int count) => Count.Value += count;
    public void DecreaseCount(int count) => Count.Value -= count;
}