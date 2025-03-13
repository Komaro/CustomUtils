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
}
