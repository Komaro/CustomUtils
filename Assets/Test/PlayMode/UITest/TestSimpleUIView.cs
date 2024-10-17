using System.Collections.Specialized;
using TMPro;
using UnityEngine.UI;

public class TestSimpleUIView : UIView<TestSimpleUIViewModel> {

    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _countText;

    private TextMeshProUGUI _collectionText;
    private TextMeshProUGUI _dictionaryText;
    
    private Button _increaseCountButton;
    private Button _decreaseCountButton;
    
    private void Awake() {
        gameObject.TryFindComponent("", out _titleText);
        gameObject.TryFindComponent("", out _countText);
        
        gameObject.TryFindComponent("", out _collectionText);
        gameObject.TryFindComponent("", out _dictionaryText);
        
        gameObject.TryFindComponent("", out _increaseCountButton);
        gameObject.TryFindComponent("", out _decreaseCountButton);
        
        _increaseCountButton.onClick.AddListener(OnClickIncreaseCountButton);
        _decreaseCountButton.onClick.AddListener(OnClickDecreaseCountButton);
    }

    private void OnEnable() {
        model.OnNotifyProperty += OnNotifyPropertyChanged;
        model.OnNotifyCollection += OnNotifyCollectionChanged;
        model.OnNotifyDictionary += OnNotifyDictionaryChanged;
    }

    private void OnNotifyPropertyChanged(string propertyName) {
        switch (propertyName) {
            case nameof(model.Title):
                _titleText.text = model.Title;
                break;
            case nameof(model.Count):
                _countText.text = model.Count.ToString();
                break;
        }
    }

    private void OnNotifyCollectionChanged(string collectionName, NotifyCollectionChangedEventArgs args) {
        switch (collectionName) {
            case nameof(model.Collection):
                _collectionText.text = model.Collection.ToStringCollection(", ");
                break;
        }
    }
    
    private void OnNotifyDictionaryChanged(string dictionaryName, NotifyCollectionChangedEventArgs args) {
        switch (dictionaryName) {
            case nameof(model.Dictionary):
                _dictionaryText.text = model.Dictionary.ToStringCollection(", ");
                break;
        }
    }

    private void OnClickIncreaseCountButton() => model.IncreaseCount(10);
    private void OnClickDecreaseCountButton() => model.DecreaseCount(10);
}
