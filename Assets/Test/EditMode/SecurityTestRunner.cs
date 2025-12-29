using System.Linq;
using NUnit.Framework;

public class SecurityTestRunner {

    [Test]
    public void TripleDESTest() {
        var key = RandomUtil.GetRandom(20);
        var plainText = RandomUtil.GetRandom(20);
        Logger.TraceLog($"{nameof(key)} : {key} || {nameof(plainText)} : {plainText}");
        
        var encryptTextBytes = EncryptUtil.EncryptTripleDESBytes(plainText, key);
        var decryptTextBytes = EncryptUtil.DecryptTripleDESBytes(encryptTextBytes.GetRawString(), key);
        Assert.IsTrue(plainText == decryptTextBytes.GetString());
        Logger.TraceLog($"Pass Bytes Test || {plainText} == {decryptTextBytes.GetString()}");

        var encryptText = EncryptUtil.EncryptTripleDES(plainText, key);
        var decryptText = EncryptUtil.DecryptTripleDES(encryptText, key);
        Logger.TraceLog(plainText == decryptText);
        Logger.TraceLog($"Pass string Test || {plainText} == {decryptText}");
    }
    
    [Test]
    public void SHATest() {
        var randomKey = RandomUtil.GetRandom(128);
        Logger.TraceLog($"{nameof(randomKey)} || {randomKey}");

        var encryptKey = EncryptUtil.GetSHA1LimitBytes(randomKey, 8);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");

        encryptKey = EncryptUtil.GetSHA1LimitBytes(randomKey, 16);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
        
        encryptKey = EncryptUtil.GetSHA1LimitBytes(randomKey, 32);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
        
        encryptKey = EncryptUtil.GetSHA256LimitBytes(randomKey, 8);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
        
        encryptKey = EncryptUtil.GetSHA256LimitBytes(randomKey, 16);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
        
        encryptKey = EncryptUtil.GetSHA256LimitBytes(randomKey, 32);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
        
        encryptKey = EncryptUtil.GetSHA512LimitBytes(randomKey, 8);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
        
        encryptKey = EncryptUtil.GetSHA512LimitBytes(randomKey, 16);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
        
        encryptKey = EncryptUtil.GetSHA512LimitBytes(randomKey, 32);
        Logger.TraceLog($"{nameof(encryptKey)} || {encryptKey.GetRawString()}");
    }

    [Test]
    public void SecurityTest() {
        var service = Service.GetService<SecurityService>();
        var randomKey = RandomUtil.GetRandom(20);
        Assert.IsNotEmpty(randomKey);
        
        Assert.IsNotEmpty(service.GenerateRandomBytes<DefaultSecurityModule>(randomKey, ENCRYPT_TYPE.MD5));
        Assert.IsNotEmpty(service.GenerateRandomBytes<DefaultSecurityModule>(randomKey, ENCRYPT_TYPE.SHA1));
        Assert.IsNotEmpty(service.GenerateRandomBytes<DefaultSecurityModule>(randomKey, ENCRYPT_TYPE.SHA256));
        Assert.IsNotEmpty(service.GenerateRandomBytes<DefaultSecurityModule>(randomKey, ENCRYPT_TYPE.SHA512));
        
        Assert.Throws<InvalidTypeException<ENCRYPT_TYPE>>(() => service.GenerateRandomBytes(typeof(DefaultSecurityModule), randomKey, ENCRYPT_TYPE.AES));
        
        var (byteKey, nativeSolution) = service.GenerateNativeSecuritySolution<DefaultSecurityModule>("45112");
        Assert.IsNotEmpty(byteKey);
        Assert.IsNotEmpty(nativeSolution);
        
        Logger.TraceLog(nativeSolution);
        
        var nativeGetByteKey = service.GetNativeKey<DefaultSecurityModule>();
        Assert.IsNotEmpty(nativeGetByteKey);
        
        Logger.TraceLog(byteKey.ToStringCollection(b => b.ToHex()));
        Logger.TraceLog(nativeGetByteKey.ToStringCollection(b => b.ToHex()));
        
        Assert.IsTrue(byteKey.SequenceEqual(nativeGetByteKey));
        
        Service.RemoveService<SecurityService>();
    }
}
