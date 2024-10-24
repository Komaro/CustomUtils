using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class UITestRunner {

    [Test]
    public void SimpleUIViewModelTest() {
        var root = new GameObject("Root");
        if (Service.GetService<ResourceService>().TryInstantiate<TestSimpleUIView>(out var ui, "TestViewModel", root)) {
            ui.gameObject.SetActive(false);

            var viewModel = new TestSimpleUIViewModel {
                Title = { Value = "TestViewModel" }
            };
            
            ui.ChangeModel(viewModel);
            ui.gameObject.SetActive(true);
        }
    }
}