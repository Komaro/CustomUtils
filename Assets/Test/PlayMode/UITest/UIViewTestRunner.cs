using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

[Category(TestConstants.Category.UI)]
public class UIViewTestRunner {

    [UnitySetUp]
    public IEnumerator OneTimeSetUp() {
        yield return null;
    }

    [UnityTest]
    public IEnumerator SimpleUIViewModelTest() {
        var root = new GameObject("Root");
        if (Service.GetService<ResourceService>().TryInstantiate<TestSimpleUIView>(out var ui, "TestViewModel", root)) {
            var viewModel = new TestSimpleUIViewModel {
                Title = { Value = "TestViewModel" }
            };

            ui.ChangeViewModel(viewModel);
            ui.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.25f);
            
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            viewModel.Title.Value = nameof(UIViewTestRunner);
            ui.FieldChangeTest<TextMeshProUGUI>("_titleText", flags, text => text.text == nameof(UIViewTestRunner));
            
            viewModel.Count.Value = 15;
            ui.FieldChangeTest<TextMeshProUGUI>("_countText", flags, text => text.text == viewModel.Count.ToString());

            viewModel.Collection.Add(5);
            viewModel.List.Add(10);
            viewModel.Dictionary.Add(1, 5);

            ui.MethodCallTest("OnClickIncreaseCountButton", flags, () => viewModel.Count == 25);
            ui.MethodCallTest("OnClickDecreaseCountButton", flags, () => viewModel.Count == 15);
        }
    }
}