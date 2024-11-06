using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

[Category(TestConstants.Category.UI)]
public class UIServiceTestRunner {

    private Scene _activeScene;
    
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
        
    }

    [Test]
    public void UIServiceTest() {
        if (Service.TryGetService<UIService>(out var service)) {
            
        }
    }

    [Test]
    [Performance]
    public void TempPerformanceTest() {
        var forGroup = new SampleGroup("ForCount");
        var canvases = Object.FindObjectsOfType<Canvas>();
        Measure.Method(() => {
            foreach (var canvas in canvases) {
                _ = canvas.transform.GetHierarchyDepth();
            }
        }).WarmupCount(1).IterationsPerMeasurement(15).MeasurementCount(5).SampleGroup(forGroup).GC().Run();
    }
}

[Priority(100)]
public class TestUIInitializeProvider : UIInitializeProvider {

    public override bool IsReady() => true;

    public override Transform GetUIRoot() {
        var canvas = Object.FindObjectsOfType<Canvas>().OrderBy(canvas => canvas.transform.GetHierarchyDepth(), true).First();
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