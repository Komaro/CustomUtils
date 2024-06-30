using UnityEngine;
using UnityEngine.UI;

public class TestScene : MonoBehaviour {

    private Camera _mainCamera;
    private Camera _copyCamera;

    private RawImage _rawImage;
    
    public void Awake() {
        _mainCamera = Camera.main;
        gameObject.TryFindComponent("CopyCamera", out _copyCamera);
        gameObject.TryFindComponent("RawImage", out _rawImage);
    }

    public void Start() {
        if (_copyCamera.activeTexture == null) {
            var renderTexture = new RenderTexture(Screen.width, Screen.height, 50) {
                enableRandomWrite = true
            };
            renderTexture.Create();
            
            _copyCamera.targetTexture = renderTexture;
            _rawImage.texture = _copyCamera.targetTexture;
        }

        Service.StartService<GraphicService>();
    }
}
