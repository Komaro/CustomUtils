using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JsonExtension {
    
    public static bool TryToJson<T>(this string text, out T json) => (json = text.ToJson<T>()) != null;
    public static T ToJson<T>(this string text) => JsonConvert.DeserializeObject<T>(text);

    public static bool TryGetValue<T>(this JObject jObject, string key, out T value) {
        jObject.ThrowIfNull(nameof(jObject));
        if (jObject.TryGetValue(key, out var token)) {
            value = token.Value<T>();
            return value != null;
        }

        value = default;
        return false;
    }
}
