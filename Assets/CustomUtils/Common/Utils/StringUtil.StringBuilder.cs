using System.Text;
using UnityEngine.Pool;

public static partial class StringUtil {
    
    public static readonly ObjectPool<StringBuilder> StringBuilderPool = new(OnCreateStringBuilder, actionOnRelease: OnReleaseStringBuilder, defaultCapacity:20);
    public static readonly ConcurrentObjectPool<StringBuilder> StringBuilderConcurrentPool = new(OnCreateStringBuilder, actionOnRelease: OnReleaseStringBuilder, defaultCapacity:10);

    private const int MAX_CAPACITY = 1024;
    private const int DEFAULT_CAPACITY = 256;
    
    private static StringBuilder OnCreateStringBuilder() {
        var stringBuilder = new StringBuilder();
        stringBuilder.EnsureCapacity(DEFAULT_CAPACITY);
        return stringBuilder;
    }

    private static void OnReleaseStringBuilder(StringBuilder stringBuilder) {
        stringBuilder.Clear();
        if (stringBuilder.Capacity > MAX_CAPACITY) {
            stringBuilder.Capacity = DEFAULT_CAPACITY;
        }
    }
}