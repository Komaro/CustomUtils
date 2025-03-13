using System;
using System.Collections.Generic;
using Random = System.Random;
using UnityRandom = UnityEngine.Random;

public static class RandomUtil {

    private static Random _random = new(DateTime.Now.GetHashCode());
    private static int _count;
    
    public static int GetRandom(int min, int max) {
        CheckRandomCount();
        FixMinMax(ref min, ref max);
        return _random.Next(min, max);
    }

    public static float GetRandom(float min, float max) {
        CheckRandomCount();
        FixMinMax(ref min, ref max);
        return UnityRandom.Range(min, max);
    }

    public static string GetRandom(int length, char min = 'A', char max = 'z') {
        CheckRandomCount();
        FixMinMax(ref min, ref max);
        using (_ = StringUtil.StringBuilderPool.Get(out var stringBuilder)) {
            for (var i = 0; i < length; i++) {
                stringBuilder.Append((char)_random.Next(min, max));
            }

            return stringBuilder.ToString();
        }
    }

    public static void GetRandom(ref Span<byte> bytes) {
        CheckRandomCount();
        _random.NextBytes(bytes);
    }

    public static void GetRandom(ref byte[] bytes) {
        CheckRandomCount();
        _random.NextBytes(bytes);
    }
    
    public static int GetRandomInt() {
        CheckRandomCount();
        return _random.Next();
    }
    
    public static double GetRandomDouble() {
        CheckRandomCount();
        return _random.NextDouble();
    }
    
    public static IEnumerable<int> GetRandoms(int length, int min = 0, int max = 10000) {
        CheckRandomCount();
        for (var i = 0; i < length; i++) {
            yield return _random.Next(min, max);
        }
    }

    public static IEnumerable<TValue> GetRandoms<TValue>(int length, Func<int, TValue> createFunc) {
        CheckRandomCount();
        for (var index = 0; index < length; index++) {
            yield return createFunc.Invoke(index);
        }
    }

    private static void CheckRandomCount() {
        _count++;
        if (_count > 100) {
            var seed = DateTime.Now.GetHashCode();
            _random = new Random(seed);
            UnityRandom.InitState(seed);
            
            _count = 0;
        }
    }

    private static void FixMinMax(ref int min, ref int max) {
        if (min == max) {
            max++;
        }

        if (min > max) {
            (min, max) = (max, min);
        }
    }

    private static void FixMinMax(ref float min, ref float max) {
        if (Math.Abs(min - max) < 1e-6f) {
            max++;
        }
        
        if (min > max) {
            (min, max) = (max, min);
        }
    }
    
    private static void FixMinMax(ref char min, ref char max) {
        if (min == max) {
            max++;
        }

        if (min > max) {
            (min, max) = (max, min);
        }
    }
}