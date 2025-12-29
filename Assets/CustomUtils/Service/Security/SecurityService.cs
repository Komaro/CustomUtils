using System;
using System.Collections.Generic;

// TODO. Runtime Section
public partial class SecurityService : IService {

    private readonly Dictionary<Type, ISecurityModule> _moduleDic = new();
    
    private ISecurityModule _securityModule;
    
    void IService.Start() { }
    void IService.Stop() { }
    void IService.Remove() => _moduleDic.Clear();

    public byte[] GetNativeKey<T>() where T : ISecurityModule => GetNativeKey(typeof(T));
    public byte[] GetNativeKey(Type moduleType) => TryGetSecurityModule(moduleType, out var module) ? module.GetNativeKey() : Array.Empty<byte>();

    private bool TryGetSecurityModule(Type moduleType, out ISecurityModule module) => (module = GetSecurityModule(moduleType)) != null;
    
    private ISecurityModule GetSecurityModule(Type moduleType) {
        try {
            if (_moduleDic.TryGetValue(moduleType, out var module)) {
                return module;
            }

            if (SystemUtil.TryCreateInstance(out module, moduleType)) {
                _moduleDic.Add(moduleType, module);
                return module;
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return null;
    }
}
