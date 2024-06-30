using System;
using System.IO;
using System.Security.Cryptography;

public static class EncryptUtil {

    #region [MD5]

    public static string GetMD5(string text) => GetMD5Bytes(text).GetRawString();
    public static byte[] GetMD5Bytes(string text) => GetMD5Bytes(text.GetBytes());

    public static byte[] GetMD5Bytes(byte[] bytes) {
        try {
            using (var md = MD5.Create()) {
                return md.ComputeHash(bytes);
            }
        } catch (Exception ex) {
            Console.WriteLine(ex);
            return Array.Empty<byte>();
        }
    }

    #endregion

    #region [SHA]

    public static string GetSHA1(string text) => GetSHA1Bytes(text).GetRawString();
    public static string GetSHA256(string text) => GetSHA256Bytes(text).GetRawString();
    public static string GetSHA512(string text) => GetSHA512Bytes(text).GetRawString();

    public static byte[] GetSHA1Bytes(string text) => GetSHA1Bytes(text.GetBytes());
    public static byte[] GetSHA256Bytes(string text) => GetSHA256Bytes(text.GetBytes());
    public static byte[] GetSHA512Bytes(string text) => GetSHA512Bytes(text.GetBytes());

    public static byte[] GetSHA1LimitBytes(string plainKey, int byteLength) => TrimBytes(GetSHA1Bytes(plainKey), byteLength);
    public static byte[] GetSHA256LimitBytes(string plainKey, int byteLength) => TrimBytes(GetSHA256Bytes(plainKey), byteLength);
    public static byte[] GetSHA512LimitBytes(string plainKey, int byteLength) => TrimBytes(GetSHA512Bytes(plainKey), byteLength);

    public static byte[] GetSHA1Bytes(byte[] bytes) {
        try {
            using (var sha = SHA1.Create()) {
                return sha.ComputeHash(bytes);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<byte>();
        }
    }

    public static byte[] GetSHA256Bytes(byte[] bytes) {
        try {
            using (var sha = SHA256.Create()) {
                return sha.ComputeHash(bytes);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<byte>();
        }
    }

    public static byte[] GetSHA512Bytes(byte[] bytes) {
        try {
            using (var sha = SHA512.Create()) {
                return sha.ComputeHash(bytes);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<byte>();
        }
    }

    #endregion

    #region [DES]

    public static string EncryptDES(string plainText, string key = nameof(EncryptUtil)) => EncryptDESBytes(plainText, key).GetRawString();
    public static string DecryptDES(string cipherText, string key = nameof(EncryptUtil)) => DecryptDESBytes(cipherText, key).GetString();

    public static byte[] EncryptDESBytes(string plainText, string key = nameof(EncryptUtil)) => EncryptDESBytes(plainText.GetBytes(), key);
    public static byte[] DecryptDESBytes(string cipherText, string key = nameof(EncryptUtil)) => DecryptDESBytes(cipherText.GetRawBytes(), key);

    public static byte[] EncryptDESBytes(byte[] bytes, string key = nameof(EncryptUtil)) {
        try {
            using (var des = DES.Create()) {
                des.Key = GetSHA256LimitBytes(key, 8);
                des.IV = new byte[8];

                using (var encryptor = des.CreateEncryptor(des.Key, des.IV)) {
                    return PerformCryptography(bytes, encryptor);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<byte>();
        }
    }

    public static byte[] DecryptDESBytes(byte[] bytes, string key = nameof(EncryptUtil)) {
        try {
            using (var des = DES.Create()) {
                des.Key = GetSHA256LimitBytes(key, 8);
                des.IV = new byte[8];

                using (var decryptor = des.CreateDecryptor(des.Key, des.IV)) {
                    return PerformCryptography(bytes, decryptor);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<byte>();
        }
    }

    #endregion

    #region [AES]

    public static bool TryEncryptAES(out string cipherText, string plainText, string key = nameof(EncryptUtil)) {
        cipherText = EncryptAES(plainText, key);
        return string.IsNullOrEmpty(cipherText) == false;
    }
    
    public static string EncryptAES(string plainText, string key = nameof(EncryptUtil)) => EncryptAESBytes(plainText, key).GetRawString();

    public static bool TryDecryptAES(out string plainText, string cipherText, string key = nameof(EncryptUtil)) {
        plainText = DecryptAES(cipherText, key);
        return string.IsNullOrEmpty(plainText) == false;
    }
    
    public static string DecryptAES(string cipherText, string key = nameof(EncryptUtil)) => DecryptAESBytes(cipherText, key).GetString();

    public static bool TryEncryptAESBytes(out byte[] cipherBytes, string plainText, string key = nameof(EncryptUtil)) {
        cipherBytes = EncryptAESBytes(plainText.GetBytes(), key);
        return cipherBytes is { Length: > 0 };
    }
    
    public static byte[] EncryptAESBytes(string plainText, string key = nameof(EncryptUtil)) => EncryptAESBytes(plainText.GetBytes(), key);
    
    public static bool TryDecryptAESBytes(out byte[] plainBytes, string cipherText, string key = nameof(EncryptUtil)) {
        plainBytes = DecryptAESBytes(cipherText.GetRawBytes(), key);
        return plainBytes is { Length: > 0 };
    }

    public static byte[] DecryptAESBytes(string cipherText, string key = nameof(EncryptUtil)) => DecryptAESBytes(cipherText.GetRawBytes(), key);

    public static bool TryEncryptAESBytes(out byte[] cipherBytes, byte[] bytes, string key = nameof(EncryptUtil)) {
        cipherBytes = EncryptAESBytes(bytes, key);
        return cipherBytes is { Length: > 0 };
    }

    public static byte[] EncryptAESBytes(byte[] bytes, string key = nameof(EncryptUtil)) {
        try {
            using (var aes = Aes.Create()) {
                aes.Key = GetSHA256LimitBytes(key, 16);
                aes.IV = new byte[16];

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV)) {
                    return PerformCryptography(bytes, encryptor);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<byte>();
        }
    }

    public static bool TryDecryptAESBytes(out byte[] plainBytes, byte[] bytes, string key = nameof(EncryptUtil)) {
        plainBytes = DecryptAESBytes(bytes, key);
        return plainBytes is { Length: > 0 };
    }

    public static byte[] DecryptAESBytes(byte[] bytes, string key = nameof(EncryptUtil)) {
        try {
            using (var aes = Aes.Create()) {
                aes.Key = GetSHA256LimitBytes(key, 16);
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV)) {
                    return PerformCryptography(bytes, decryptor);
                }
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
            return Array.Empty<byte>();
        }
    }

    #endregion

    public static bool TryDecrypt(out string plainText, string cipherText, string key = nameof(EncryptUtil), ENCRYPT_TYPE type = default) {
        plainText = Decrypt(cipherText, key, type);
        return string.IsNullOrEmpty(plainText) == false;
    }
    
    public static string Decrypt(string cipherText, string key = nameof(EncryptUtil), ENCRYPT_TYPE type = default) {
        switch (type) { 
            case ENCRYPT_TYPE.AES:
                return DecryptAES(cipherText, key);
            case ENCRYPT_TYPE.DES:
                return DecryptDES(cipherText, key);
        }

        Logger.TraceLog($"{type} is Invalid {nameof(ENCRYPT_TYPE)}");
        return string.Empty;
    }

    public static bool TryDecrypt(out byte[] plainBytes, byte[] bytes, string key = nameof(EncryptUtil), ENCRYPT_TYPE type = default) {
        plainBytes = Decrypt(bytes, key, type);
        return plainBytes != Array.Empty<byte>();
    }

    public static byte[] Decrypt(byte[] bytes, string key = nameof(EncryptUtil), ENCRYPT_TYPE type = default) {
        switch (type) {
            case ENCRYPT_TYPE.AES:
                return DecryptAESBytes(bytes, key);
            case ENCRYPT_TYPE.DES:
                return DecryptDESBytes(bytes, key);
        }
        
        Logger.TraceLog($"{type} is Invalid {nameof(ENCRYPT_TYPE)}");
        return Array.Empty<byte>();
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

    private static byte[] TrimBytes(byte[] bytes, int byteLength) {
        var trimBytes = new byte[byteLength];
        Array.Copy(bytes, trimBytes, Math.Min(byteLength, bytes.Length));
        return trimBytes;
    }
}

public enum ENCRYPT_TYPE {
    AES,
    DES,
    SHA,
    MD5,
}