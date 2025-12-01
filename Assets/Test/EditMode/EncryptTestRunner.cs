using NUnit.Framework;

public class EncryptTestRunner {

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
}
