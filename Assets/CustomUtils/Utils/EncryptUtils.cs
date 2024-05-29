using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class EncryptUtils {

    #region [AES]
    
    public static byte[] EncrytAES(byte[] bytes, string key = nameof(EncryptUtils)) {
        using (var aes = Aes.Create()) {
            aes.Key = ConvertStringToAESKey(key);
            aes.IV = new byte[16];

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV)) {
                return PerformCryptography(bytes, encryptor);
            }
        }
    }

    public static byte[] DecryptAES(byte[] bytes, string key = nameof(EncryptUtils)) {
        using (var aes = Aes.Create()) {
            aes.Key = ConvertStringToAESKey(key);
            aes.IV = new byte[16];

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV)) {
                return PerformCryptography(bytes, decryptor);
            }
        }
    }

    private static byte[] PerformCryptography(byte[] bytes, ICryptoTransform cryptoTransform) {
        using (var memoryStream = new MemoryStream()) {
            using (var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write)) {
                cryptoStream.Write(bytes, 0, bytes.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
    }

    public static byte[] ConvertStringToAESKey(string key) {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using (var sha = SHA256.Create()) {
            return sha.ComputeHash(keyBytes);
        }
    }
    
    #endregion
}