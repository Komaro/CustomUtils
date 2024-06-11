using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class CommonExtension {

    private static StringBuilder _stringBuilder = new();

    public static string ToStringAllFields(this object ob) {
        if (_stringBuilder == null) {
            return string.Empty;
        }
        
        var type = ob.GetType();
        _stringBuilder.Clear();
        _stringBuilder.AppendLine(type.Name);
        
        foreach (var info in type.GetFields().Select(x => (x.Name, x.GetValue(ob)))) {
            _stringBuilder.AppendLine($"{info.Item1} || {info.Item2}");
        }

        return _stringBuilder.ToString();
    }

    public static string GetString(this byte[] bytes, ENCODING_FORMAT format = ENCODING_FORMAT.UTF_8) => format switch {
        ENCODING_FORMAT.UTF_32 => Encoding.UTF32.GetString(bytes),
        ENCODING_FORMAT.UNICODE => Encoding.Unicode.GetString(bytes),
        ENCODING_FORMAT.ASCII => Encoding.ASCII.GetString(bytes),
        _ => Encoding.UTF8.GetString(bytes)
    };

    public static string GetRawString(this byte[] bytes) => Convert.ToBase64String(bytes);

    public static bool IsGenericCollectionType(this Type type) {
        if (type.IsGenericType == false) {
            return false;
        }

        foreach (var interfaceType in type.GetInterfaces()) {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                return true;
            }
        }

        return false;
    }

    public static IEnumerator CloneEnumerator(this ICollection collection) => new ArrayList(collection).GetEnumerator();
}
