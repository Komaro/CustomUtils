public partial interface ISecurityModule {

    public byte[] GetNativeKey();
}

#if UNITY_EDITOR

public partial interface ISecurityModule {
    
    public byte[] GenerateBytes(string key, ENCRYPT_TYPE type, int length = 32);
    public (byte[] byteKey, string nativeSolution) GenerateNativeSecuritySolution(string key);
}

#endif