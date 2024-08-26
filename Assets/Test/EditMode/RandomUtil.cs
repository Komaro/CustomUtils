using System;

public static class RandomUtil {

    private static Random _random = new(DateTime.Now.GetHashCode());
    private static int _count;
    
    public static int GetRandom(int min, int max) {
        CheckRandomCount();
        return _random.Next(min, max);
    }

    private static void CheckRandomCount() {
        _count++;
        if (_count > 100) {
            _count = 0;
            _random = new Random(DateTime.Now.GetHashCode());
        }
    }
}