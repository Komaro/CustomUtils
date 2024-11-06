using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine.UI;

[UIView("TestViewModel")]
public class TestSimpleUIView : UIView<TestSimpleUIViewModel> {

    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _countText;

    private TextMeshProUGUI _collectionText;
    private TextMeshProUGUI _dictionaryText;
    
    private Button _increaseCountButton;
    private Button _decreaseCountButton;
    
    private void Awake() {
        gameObject.TryFindComponent("TitleText", out _titleText);
        gameObject.TryFindComponent("CountText", out _countText);
        
        gameObject.TryFindComponent("CollectionText", out _collectionText);
        gameObject.TryFindComponent("DictionaryText", out _dictionaryText);
        
        gameObject.TryFindComponent("IncreaseButton", out _increaseCountButton);
        gameObject.TryFindComponent("DecreaseButton", out _decreaseCountButton);
        
        _increaseCountButton.onClick.AddListener(OnClickIncreaseCountButton);
        _decreaseCountButton.onClick.AddListener(OnClickDecreaseCountButton);
    }

    protected override void OnNotifyModelChanged(string fieldName, NotifyFieldChangedEventArgs args) {
        switch (args) {
            case NotifyCollectionChangedEventArgs listArgs:
                OnNotifyListChanged(fieldName, listArgs);
                break;
            default:
                OnNotifyPropertyChanged(fieldName);
                break;
        }
    }

    private void OnNotifyPropertyChanged(string name) {
        switch (name) {
            case nameof(model.Title):
                UpdateTitle();
                break;
            case nameof(model.Count):
                UpdateCount();
                break;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateTitle() => _titleText.text = model.Title;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCount() => _countText.text = model.Count.ToString();
    
    private void OnNotifyListChanged(string name, NotifyCollectionChangedEventArgs args) {
        switch (name) {
            case nameof(model.Collection):
                UpdateCollection(args);
                break;
            case nameof(model.List):
                UpdateList(args);
                break;
            case nameof(model.Dictionary):
                UpdateDictionary(args);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCollection(NotifyCollectionChangedEventArgs args) {
        Logger.TraceLog(args.action);
        _collectionText.text = model.Collection.ToStringCollection(", ");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateList(NotifyCollectionChangedEventArgs args) {
        Logger.TraceLog(args.action);
        _collectionText.text = model.List.ToStringCollection(", ");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateDictionary(NotifyCollectionChangedEventArgs args) {
        Logger.TraceLog(args.action);
        _dictionaryText.text = model.Dictionary.ToStringCollection(x => x.ToStringPair(), ", ");
    }

    private void OnClickIncreaseCountButton() => model.IncreaseCount(10);
    private void OnClickDecreaseCountButton() => model.DecreaseCount(10);
}
