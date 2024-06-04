using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

public static class EncryptUtil {

    #region [SHA]

    public static string GetSHA1(string text) => BitConverter.ToString(GetSHA1Hash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
    public static string GetSHA256(string text) => BitConverter.ToString(GetSHA256Hash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
    public static string GetSHA512(string text) => BitConverter.ToString(GetSHA512Hash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();

    public static byte[] GetSHA1Hash(string text) => GetSHA1Hash(Encoding.UTF8.GetBytes(text));
    public static byte[] GetSHA256Hash(string text) => GetSHA256Hash(Encoding.UTF8.GetBytes(text));
    public static byte[] GetSHA512Hash(string text) => GetSHA512Hash(Encoding.UTF8.GetBytes(text));
    
    private static byte[] GetSHA1LimitHash(string plainKey, int byteLength) => TrimBytes(GetSHA1Hash(plainKey), byteLength);
    private static byte[] GetSHA256LimitHash(string plainKey, int byteLength) => TrimBytes(GetSHA256Hash(plainKey), byteLength);
    private static byte[] GetSHA512Hash(string plainKey, int byteLength) => TrimBytes(GetSHA512Hash(plainKey), byteLength);
    
    public static byte[] GetSHA1Hash(byte[] bytes) {
        using (var sha = SHA1.Create()) {
            return sha.ComputeHash(bytes);
        }
    }

    public static byte[] GetSHA256Hash(byte[] bytes) {
        using (var sha = SHA256.Create()) {
            return sha.ComputeHash(bytes);
        }
    }
    
    public static byte[] GetSHA512Hash(byte[] bytes) {
        using (var sha = SHA512.Create()) {
            return sha.ComputeHash(bytes);
        }
    }

    #endregion

    #region [DES]

    public static string EncryptDES(string plainText, string key = nameof(EncryptUtil)) => Convert.ToBase64String(EncryptDES(Encoding.UTF8.GetBytes(plainText), key));
    public static string DecryptDES(string cipherText, string key = nameof(EncryptUtil)) => Encoding.UTF8.GetString(DecryptDES(Convert.FromBase64String(cipherText), key));

    public static byte[] EncryptDES(byte[] bytes, string key = nameof(EncryptUtil)) {
        using (var des = DES.Create()) {
            des.Key = GetSHA256LimitHash(key, 8);
            des.IV = new byte[8];

            using (var encryptor = des.CreateEncryptor(des.Key, des.IV)) {
                return PerformCryptography(bytes, encryptor);
            }
        }
    }

    public static byte[] DecryptDES(byte[] bytes, string key = nameof(EncryptUtil)) {
        using (var des = DES.Create()) {
            des.Key = GetSHA256LimitHash(key, 8);
            des.IV = new byte[8];

            using (var decryptor = des.CreateDecryptor(des.Key, des.IV)) {
                return PerformCryptography(bytes, decryptor);
            }
        }
    }
    
    #endregion
    
    #region [AES]

    public static byte[] EncrytAES(byte[] bytes, string key = nameof(EncryptUtil)) {
        using (var aes = Aes.Create()) {
            aes.Key = GetSHA256LimitHash(key, 16);
            aes.IV = new byte[16];

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV)) {
                return PerformCryptography(bytes, encryptor);
            }
        }
    }

    public static byte[] DecryptAES(byte[] bytes, string key = nameof(EncryptUtil)) {
        using (var aes = Aes.Create()) {
            aes.Key = GetSHA256LimitHash(key, 16);
            aes.IV = new byte[16];

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV)) {
                return PerformCryptography(bytes, decryptor);
            }
        }
    }

    #endregion

    private static byte[] PerformCryptography(byte[] bytes, ICryptoTransform cryptoTransform) {
        using (var memoryStream = new MemoryStream()) {
            using (var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write)) {
                cryptoStream.Write(bytes, 0, bytes.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
    }

    private static byte[] TrimBytes(byte[] bytes, int byteLength) {
        var trimBytes = new byte[byteLength];
        Array.Copy(bytes, trimBytes, Math.Min(byteLength, bytes.Length));
        return trimBytes;
    }
}