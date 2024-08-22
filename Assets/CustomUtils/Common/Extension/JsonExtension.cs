using Newtonsoft.Json;

public static class JsonExtension {
    
    public static bool TryToJson<T>(this string text, out T json) => (json = text.ToJson<T>()) != null;
    public static T ToJson<T>(this string text) => JsonConvert.DeserializeObject<T>(text);
}
