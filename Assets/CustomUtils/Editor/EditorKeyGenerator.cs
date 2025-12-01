using UnityEditor;
using System;
using Newtonsoft.Json;

// TODO.
public class EditorKeyGenerator : EditorService<EditorKeyGenerator>
{
    private string aesKeyHex = "";
    private string aesIVHex = "";

    private KeyGeneratorDrawer _drawer;
    
    [MenuItem("Service/Native/EditorKeyGenerator")]
    public static void OpenWindow() => Window.Show();

    protected override void Refresh() {
        if (HasOpenInstances<EditorKeyGenerator>()) {
            _drawer ??= new KeyGeneratorDrawer(Window);
            _drawer.CacheRefresh();
        }
    }

    private void OnGUI() {
        if (_drawer == null) {
            EditorGUILayout.HelpBox($"{nameof(KeyGeneratorDrawer)} 가 존재하지 않습니다.", MessageType.Error);
            return;
        }
        
        _drawer.Draw();
    }
}

public class KeyGeneratorDrawer : EditorAutoConfigDrawer<KeyGeneratorConfig, KeyGeneratorConfig.NullConfig> {
    
    protected override string CONFIG_NAME => nameof(KeyGeneratorConfig);
    protected override string CONFIG_PATH => $"{Constants.Path.COMMON_CONFIG_PATH}/{CONFIG_NAME.AutoSwitchExtension(Constants.Extension.JSON)}";
    
    public KeyGeneratorDrawer(EditorWindow window) : base(window) { }

    public override void Draw() {
        base.Draw();

        EditorCommon.DrawEnumPopup("Generator", ref config.generatorType);
        EditorCommon.DrawLabelTextField("Seed", ref config.seed);
        EditorCommon.DrawLabelIntField("Length", ref config.length);
        
        if (EditorCommon.DrawButton("Generate", string.IsNullOrEmpty(config.seed) && config.length > 0)) {
            var bytes = config.generatorType switch {
                KEY_GENERATOR_TYPE.MD5 => EncryptUtil.GetMD5Bytes(config.seed),
                KEY_GENERATOR_TYPE.SHA1 => EncryptUtil.GetSHA1LimitBytes(config.seed, config.length),
                KEY_GENERATOR_TYPE.SHA256 => EncryptUtil.GetSHA256LimitBytes(config.seed, config.length),
                KEY_GENERATOR_TYPE.SHA512 => EncryptUtil.GetSHA512LimitBytes(config.seed, config.length),
                _ => throw new ArgumentOutOfRangeException(nameof(config.generatedBytes))
            };
            
            config.generatedBytes = bytes;
            config.generatedKey = bytes.GetRawString();
            config.generatedHexKey = bytes.ToStringCollection(b => $"{b:X}");
            Logger.TraceLog(config.generatedKey);
            Logger.TraceLog(config.generatedHexKey);
        }
        
        EditorCommon.DrawSeparator();

        if (config.generatedBytes != null && config.generatedBytes.Length > 0) {
            using (new EditorGUILayout.VerticalScope(Constants.Draw.BOX)) {
                EditorCommon.DrawWideTextArea("Raw", config.generatedKey, 80f, Constants.Draw.CLIPPING_TEXT_AREA);
                EditorCommon.DrawWideTextArea("Hex", config.generatedHexKey, 80f, Constants.Draw.CLIPPING_TEXT_AREA);
                if (EditorCommon.DrawButton("Copy")) {
                    // TODO.
                }
            }
        }
        
        // TODO. Native Generator 코드 생성 로직 추가
    }
    
    
//     void Generate()
//     {
//         byte[] key = HexToBytes(aesKeyHex);
//         byte[] iv = HexToBytes(aesIVHex);
//
//         if (key.Length != 32) throw new Exception("AES Key must be 32 bytes (64 hex)");
//         if (iv.Length != 16) throw new Exception("AES IV must be 16 bytes (32 hex)");
//
//         System.Random rnd = new System.Random();
//
//         byte[] K1 = Rand(32, rnd);
//         byte[] K2 = Rand(32, rnd);
//         byte[] K3 = Rand(32, rnd);
//         byte[] K4 = Rand(32, rnd);
//
//         byte[] IV1 = Rand(16, rnd);
//         byte[] IV2 = Rand(16, rnd);
//
//         // --- Key 역연산 보정 ---
//         for (int i = 0; i < 32; i++)
//         {
//             byte rotated = RotL(key[i], 3);
//             byte c = (byte)(rotated ^ (i * 7));
//             byte a = (byte)(K1[i] ^ K2[(i + 5) % 32]);
//             byte b = (byte)(c - a);
//             K3[31 - i] = (byte)((b ^ K4[i]) - 0x41);
//         }
//
//         // --- IV 역연산 보정 ---
//         for (int i = 0; i < 16; i++)
//         {
//             byte orig = iv[i];
//             byte c = (byte)(orig ^ ((i * 11) & 0xFF));
//             IV2[15 - i] = (byte)((c ^ (0x5A + i)) - 0xA1);
//         }
//
//         // --- C++ 코드 생성 ---
//         string cpp = GenerateCPP(K1, K2, K3, K4, IV1, IV2);
//
//         string outPath = Application.dataPath + "/NativeSource/keyprovider.cpp";
//         Directory.CreateDirectory(Path.GetDirectoryName(outPath));
//         File.WriteAllText(outPath, cpp, Encoding.UTF8);
//
//         EditorUtility.DisplayDialog("Completed", "C++ Native KeyProvider generated:\n" + outPath, "OK");
//         AssetDatabase.Refresh();
//     }
//
//     byte[] Rand(int len, System.Random r) { byte[] b = new byte[len]; r.NextBytes(b); return b; }
//     byte RotL(byte v, int n) => (byte)((v << n) | (v >> (8 - n)));
//
//     byte[] HexToBytes(string hex)
//     {
//         hex = hex.Replace(" ", "");
//         if (hex.Length % 2 != 0) throw new Exception("Invalid hex string");
//         byte[] data = new byte[hex.Length / 2];
//         for (int i = 0; i < data.Length; i++)
//             data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
//         return data;
//     }
//
//     string CppArray(string name, byte[] arr)
//     {
//         StringBuilder sb = new StringBuilder();
//         sb.Append($"static const uint8_t {name}[{arr.Length}] = {{\n    ");
//         for (int i = 0; i < arr.Length; i++)
//         {
//             sb.Append($"0x{arr[i]:X2}");
//             if (i < arr.Length - 1) sb.Append(", ");
//             if ((i + 1) % 8 == 0) sb.Append("\n    ");
//         }
//         sb.Append("\n};\n\n");
//         return sb.ToString();
//     }
//
//     string GenerateCPP(byte[] K1, byte[] K2, byte[] K3, byte[] K4,
//                        byte[] IV1, byte[] IV2)
//     {
//         return
// $@"#include <stdint.h>
// #include <string.h>
//
// #if defined(_WIN32)
// #define EXPORT_API __declspec(dllexport)
// #else
// #define EXPORT_API
// #endif
//
// {CppArray("K1", K1)}
// {CppArray("K2", K2)}
// {CppArray("K3", K3)}
// {CppArray("K4", K4)}
// {CppArray("IV1", IV1)}
// {CppArray("IV2", IV2)}
//
// extern ""C"" EXPORT_API void GetAesKey(uint8_t* out32)
// {{
//     uint8_t tmp[32];
//     for (int i = 0; i < 32; i++)
//     {{
//         uint8_t a = K1[i] ^ K2[(i + 5) % 32];
//         uint8_t b = (K3[31 - i] + 0x41) ^ K4[i];
//         uint8_t c = (a + b) ^ ((i * 7) & 0xFF);
//         tmp[i] = (uint8_t)((c >> 3) | (c << 5));
//     }}
//     memcpy(out32, tmp, 32);
// }}
//
// extern ""C"" EXPORT_API void GetAesIV(uint8_t* out16)
// {{
//     uint8_t tmp[16];
//     for (int i = 0; i < 16; i++)
//     {{
//         uint8_t a = IV1[i] ^ (0x5A + i);
//         uint8_t b = IV2[(15 - i)] ^ 0xA1;
//         tmp[i] = (uint8_t)((a + b) ^ ((i * 11) & 0xFF));
//     }}
//     memcpy(out16, tmp, 16);
// }}
// ";
//     }
}

public enum KEY_GENERATOR_TYPE {
    MD5,
    SHA1,
    SHA256,
    SHA512
}

public class KeyGeneratorConfig : JsonCoroutineAutoConfig {

    public KEY_GENERATOR_TYPE generatorType;
    public string seed;
    public int length;

    [JsonIgnore]
    public byte[] generatedBytes;
    
    [JsonIgnore]
    public string generatedKey;
    
    [JsonIgnore]
    public string generatedHexKey;
    
    public override bool IsNull() => this is NullConfig;

    public class NullConfig : KeyGeneratorConfig { }
}