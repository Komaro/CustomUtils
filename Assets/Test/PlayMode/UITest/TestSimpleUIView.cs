using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine.UI;

[UIView("TestViewModel", priority = 5)]
public class TestSimpleUIView : UIView<TestSimpleUIViewModel> {

    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _countText;

    private TextMeshProUGUI _collectionText;
    private TextMeshProUGUI _dictionaryText;
    
    private Button _increaseCountButton;
    private Button _decreaseCountButton;

    protected override void Awake() {
        base.Awake();
        
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
            case nameof(viewModel.Title):
                UpdateTitle();
                break;
            case nameof(viewModel.Count):
                UpdateCount();
                break;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateTitle() => _titleText.text = viewModel.Title;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCount() => _countText.text = viewModel.Count.ToString();
    
    private void OnNotifyListChanged(string name, NotifyCollectionChangedEventArgs args) {
        switch (name) {
            case nameof(viewModel.Collection):
                UpdateCollection(args);
                break;
            case nameof(viewModel.List):
                UpdateList(args);
                break;
            case nameof(viewModel.Dictionary):
                UpdateDictionary(args);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCollection(NotifyCollectionChangedEventArgs args) {
        Logger.TraceLog(args.action);
        _collectionText.text = viewModel.Collection.ToStringCollection(", ");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateList(NotifyCollectionChangedEventArgs args) {
        Logger.TraceLog(args.action);
        _collectionText.text = viewModel.List.ToStringCollection(", ");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateDictionary(NotifyCollectionChangedEventArgs args) {
        Logger.TraceLog(args.action);
        _dictionaryText.text = viewModel.Dictionary.ToStringCollection(x => x.ToStringPair(), ", ");
    }

    private void OnClickIncreaseCountButton() => viewModel.IncreaseCount(10);
    private void OnClickDecreaseCountButton() => viewModel.DecreaseCount(10);
}