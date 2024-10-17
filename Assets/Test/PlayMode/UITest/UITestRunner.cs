using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class UITestRunner {

    private const int TEST_COUNT_001 = 48853;
    
    [UnityTest]
    public IEnumerator RxUIViewModelTest() {
        var root = new GameObject("Root");
        if (Service.GetService<ResourceService>().TryInstantiate<TestUIView>(out var ui, "TestUI", root)) {
            var uiType = ui.GetType();
            ui.ChangeModel(new TestUIView.TestUIViewModel(TEST_COUNT_001));
            ui.OnChangeCount(TEST_COUNT_001);
            ui.OnClickButton();

            yield return new WaitForSeconds(0.25f);
            
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            if (uiType.TryGetFieldValue<TextMeshProUGUI>(out var text, ui, "_countText", bindingFlags)) {
                Logger.TraceLog(text.text);
                Assert.IsNotEmpty(text.text);
                Assert.IsTrue(int.TryParse(text.text, out var count));
                Assert.IsTrue(count == (TEST_COUNT_001 + 10));
                Logger.TraceLog("Pass Count Test\n");
            }

            yield return new WaitForSeconds(0.25f);
            if (uiType.TryGetFieldValue(out text, ui, "_scoreText", bindingFlags)) {
                Logger.TraceLog(text.text);
                Assert.IsNotEmpty(text.text);
                Logger.TraceLog("Pass Score Test\n");
            }
            
            yield return new WaitForSeconds(0.25f);
            if (uiType.TryGetFieldValue<TMP_InputField>(out var inputField, ui, "_countInputField", bindingFlags)) {
                Logger.TraceLog(inputField.text);
                Assert.IsNotEmpty(inputField.text);
                Assert.IsTrue(int.TryParse(inputField.text, out var count));
                Assert.IsTrue(count == TEST_COUNT_001 + 10);
                Logger.TraceLog("Pass InputField Test\n");
            }
        }
    }

    [UnityTest]
    public IEnumerator SimpleUIViewModelTest() {
        var root = new GameObject("Root");
        if (Service.GetService<ResourceService>().TryInstantiate<TestUIView>(out var ui, "some", root)) {
            // TODO. TEST SimpleUIViewModel
            yield return null;
        }
    }
}