using System;
using System.Collections.Generic;
using Random = System.Random;
using UnityRandom = UnityEngine.Random;

public static class RandomUtil {

    private static Random _random = new(DateTime.Now.GetHashCode());
    private static int _count;
    
    public static int GetRandom(int min, int max) {
        CheckRandomCount();
        return _random.Next(min, max);
    }

    public static float GetRandom(float min, float max) {
        CheckRandomCount();
        return UnityRandom.Range(min, max);
    }

    public static string GetRandom(int length, char min = 'A', char max = 'z') {
        if (min > max) {
            return string.Empty;
        }
        
        CheckRandomCount();
        StringUtil.StringBuilderPool.Get(out var builder);
        for (var i = 0; i < length; i++) {
            builder.Append((char)_random.Next(min, max));
        }
        
        StringUtil.StringBuilderPool.Release(builder);
        return builder.ToString();
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
}