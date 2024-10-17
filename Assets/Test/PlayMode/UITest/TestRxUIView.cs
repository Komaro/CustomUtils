using System;
using TMPro;
using UniRx;
using UnityEngine.UI;

public class TestUIView : UIView<TestUIView.TestUIViewModel> {

    private TextMeshProUGUI _countText;
    private TextMeshProUGUI _scoreText;
    private TMP_InputField _countInputField;

    private void Awake() {
        gameObject.TryFindComponent<Button>("Button", out var button);
        button.onClick.AddListener(OnClickButton);

        gameObject.TryFindComponent("CountText", out _countText);
        gameObject.TryFindComponent("ScoreText", out _scoreText);
        
        gameObject.TryFindComponent("CountInputField", out _countInputField);
        _countInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        _countInputField.onValueChanged.AddListener(OnChangeInputField);
    }

    public override void ChangeModel(TestUIViewModel model) {
        this.model.Dispose();
        this.model = model;
        model.count.Subscribe(OnChangeCount);
    }

    public void OnClickButton() => model.count.Value += 10;

    public void OnChangeCount(int count) {
        var countText = count.ToString();
        _countText.text = countText;
        _scoreText.text = $"Score : {countText}";
        _countInputField.text = countText;
    }

    public void OnChangeInputField(string input) {
        if (int.TryParse(input, out var count)) {
            model.count.Value = count;
        }
    }

    public record TestUIViewModel : IDisposable {

        public ReactiveProperty<int> count { get; } = new();

        public void Dispose() => count?.Dispose();
        
        public TestUIViewModel() {
            count.Value = 0;
        }

        public TestUIViewModel(int count) {
            this.count.Value = count;
        }
    }
}