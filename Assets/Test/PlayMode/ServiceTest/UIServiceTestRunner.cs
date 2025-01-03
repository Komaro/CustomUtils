using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[Category(TestConstants.Category.UI)]
[Category(TestConstants.Category.SERVICE)]
public class UIServiceTestRunner {

    private Scene _activeScene;
    
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        LogAssert.ignoreFailingMessages = true;
    }
    
    [UnitySetUp]
    public IEnumerator Setup() {
        if (SceneManager.GetActiveScene() != _activeScene) {
            var operation = SceneManager.LoadSceneAsync("TestScene_01");
            while (operation.isDone == false) {
                yield return null;
            }

            _activeScene = SceneManager.GetActiveScene();
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator UIServiceTest() {
        if (Service.TryGetService<UIService>(out var service)) {
            yield return null;
            Assert.IsTrue(service.IsValid());
            Logger.TraceLog("Pass valid check");
            
            Assert.IsNull(service.Current);
            
            Assert.IsTrue(service.TryOpen<TestSimpleUIView>(out var first));
            var viewModel = new TestSimpleUIViewModel {
                Title = { Value = "ChangeViewModel Test" },
                Count = { Value = 3544 }
            };

            first.ChangeViewModel(viewModel);

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            first.FieldChangeTest<TextMeshProUGUI>("_titleText", flags, ui => ui.text == "ChangeViewModel Test");
            first.FieldChangeTest<TextMeshProUGUI>("_countText", flags, ui => ui.text == 3544.ToString());

            viewModel = new TestSimpleUIViewModel {
                Title = { Value = "Open Test" },
                Count = { Value = 4533 }
            };
            
            Assert.IsTrue(service.TryOpen(viewModel, out first));
            first.FieldChangeTest<TextMeshProUGUI>("_titleText", flags, ui => ui.text == "Open Test");
            first.FieldChangeTest<TextMeshProUGUI>("_countText", flags, ui => ui.text == 4533.ToString());
            
            Assert.IsTrue(service.Current.Equals(first));
            Assert.IsNull(service.Previous);
            
            Logger.TraceLog("Pass Base Test");
            
            Assert.IsTrue(service.TryOpen<TestSimpleSecondUIView>(new TestSimpleUIViewModel {
                Title = { Value = "Second Test" }
            }, out var second));
            
            Assert.AreEqual(service.Current, second);
            Assert.AreEqual(service.Previous, first);

            service.FieldChangeTest<Transform>("_uiRoot", flags, tr => {
                Logger.TraceLog(tr.GetComponentsInChildren<UIViewMonoBehaviour>(true).ToStringCollection(comp => $"{comp.gameObject.name} || {(comp.GetType().TryGetCustomAttribute<UIViewAttribute>(out var attribute) ? attribute.priority : "9999999999")}", "\n"));
                return true;
            });
            
            foreach (var uiView in service.GetType().GetFieldValue<Transform>(service, "_uiRoot", flags).GetComponentsInChildren<UIViewMonoBehaviour>(true)) {
                Logger.TraceLog($"{uiView.name} || {uiView.Priority}");
            }

            Assert.IsNotNull(service.Open<TestSimpleUIView>());
            Assert.AreEqual(service.Current.GetType(), typeof(TestSimpleUIView));
            Assert.AreEqual(service.Previous.GetType(), typeof(TestSimpleSecondUIView));
            
            Assert.IsTrue(Service.RemoveService<UIService>());
        }
    }

    [Test]
    [Performance]
    public void TempPerformanceTest() {
        var forGroup = new SampleGroup("ForCount");
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Measure.Method(() => {
            foreach (var canvas in canvases) {
                _ = canvas.transform.GetHierarchyDepth();
            }
        }).WarmupCount(1).IterationsPerMeasurement(15).MeasurementCount(5).SampleGroup(forGroup).GC().Run();
    }
}

[Priority(99999)]
public class TestUIInitializeProvider : UIInitializeProvider {

    public override bool IsReady() => true;

    public override Transform GetUIRoot() {
        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).OrderBy(canvas => canvas.transform.GetHierarchyDepth(), true).First();
        if (canvas != null) {
            return canvas.transform;
        }

        var go = new GameObject("UI");
        go.AddComponent<Canvas>();
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }
}