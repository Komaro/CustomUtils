#if UNITY_EDITOR

using System;

public partial class SecurityService {
    
    public string GenerateRandomKey<T>(string key, ENCRYPT_TYPE type, int length = 32) where T : ISecurityModule => GenerateRandomKey(typeof(T), key, type, length);
    public string GenerateRandomKey(Type moduleType, string key, ENCRYPT_TYPE type, int length = 32) => TryGetSecurityModule(moduleType, out var module) ? module.GenerateBytes(key, type, length).GetRawString() : string.Empty;

    public bool TrGenerateRandomBytes<T>(out byte[] bytes, string key, ENCRYPT_TYPE type, int length = 32) where T : ISecurityModule => (bytes = GenerateRandomBytes<T>(key, type, length)) != Array.Empty<byte>(); 
    public byte[] GenerateRandomBytes<T>(string key, ENCRYPT_TYPE type, int length = 32) where T : ISecurityModule => GenerateRandomBytes(typeof(T), key, type, length);

    public bool TryGenerateRandomBytes(out byte[] bytes, Type moduleType, string key, ENCRYPT_TYPE type, int length = 32) => (bytes = GenerateRandomBytes(moduleType, key, type, length)) != Array.Empty<byte>();
    public byte[] GenerateRandomBytes(Type moduleType, string key, ENCRYPT_TYPE type, int length = 32) => TryGetSecurityModule(moduleType, out var module) ? module.GenerateBytes(key, type, length) : Array.Empty<byte>();

    public bool TryGenerateNativeSecuritySolution<T>(string key, out (byte[] byteKey, string nativeSolution) solution) where T : ISecurityModule => TryGenerateNativeSecuritySolution(typeof(T), key, out solution);
    public bool TryGenerateNativeSecuritySolution(Type moduleType, string key, out (byte[] byteKey, string nativeSolution) solution) => (solution = GenerateNativeSecuritySolution(moduleType, key)) != (Array.Empty<byte>(), string.Empty);
    
    public (byte[] byteKey, string nativeSolution) GenerateNativeSecuritySolution<T>(string key) where T : ISecurityModule => GenerateNativeSecuritySolution(typeof(T), key);
    public (byte[] byteKey, string nativeSolution) GenerateNativeSecuritySolution(Type moduleType, string key) => TryGetSecurityModule(moduleType, out var module) ? module.GenerateNativeSecuritySolution(key) : (Array.Empty<byte>(), string.Empty);
}

#endif