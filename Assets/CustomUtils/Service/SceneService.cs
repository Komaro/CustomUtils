
public class SceneService : IService {

    private bool _isServing;

    bool IService.IsServing() => _isServing;
    
    void IService.Init() {
        _isServing = true;
    }
    
    void IService.Start() {
        throw new System.NotImplementedException();
    }

    void IService.Stop() {
        throw new System.NotImplementedException();
    }
}
