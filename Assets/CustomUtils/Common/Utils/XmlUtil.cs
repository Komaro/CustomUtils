using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

public static class XmlUtil {

    #region [Serialize]

    public static void SerializeToFile(string path, Type type, object ob) {
        using (var stream = new FileStream(path.AutoSwitchExtension(Constants.Extension.XML), FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
            SerializeToStream(stream, type, ob);
        }
    }
    
    public static void SerializeToStream<T>(object ob, Stream stream) => SerializeToStream(stream, typeof(T), ob);
    
    public static void SerializeToStream(Stream stream, Type type, object ob) {
        try {
            var serializer = new XmlSerializer(type);
            serializer.Serialize(stream, ob);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }

    public static string Serialize<T>(object ob) => Serialize(typeof(T), ob);

    public static string Serialize(Type type, object ob) {
        try {
            var serializer = new XmlSerializer(type);
            using (var writer = new StringWriter(new StringBuilder())) {
                serializer.Serialize(writer, ob);
                return writer.ToString();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return string.Empty;
    }    
    
    #endregion

    #region [Deserialize]
    
    public static bool TryDeserializeAsStructFromFile<TStruct>(string path, out TStruct xml) where TStruct : struct {
        var nullableStruct = DeserializeAsStructFromFile<TStruct>(path);
        if (nullableStruct.HasValue) {
            xml = nullableStruct.Value;
            return true;
        }
        
        xml = default;
        return false;
    }
    
    public static TStruct? DeserializeAsStructFromFile<TStruct>(string path) where TStruct : struct {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            return DeserializeFromStream(typeof(TStruct), stream) as TStruct?;
        }
    }

    public static bool TryDeserializeAsClassFromFile<TClass>(string path, out TClass xml) where TClass : class => (xml = DeserializeAsClassFromFile<TClass>(path)) != null;

    public static TClass DeserializeAsClassFromFile<TClass>(string path) where TClass : class {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            return DeserializeFromStream(typeof(TClass), stream) as TClass;
        }
    }

    public static bool TryDeserializeFromFile<T>(string path, out object xml) {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            return (xml = DeserializeFromStream(typeof(T), stream)) != null;
        }
    }
    
    public static object DeserializeFromFile<T>(string path) {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            return DeserializeFromStream(typeof(T), stream);
        }
    }

    public static bool TryDeserializeFromFile(string path, Type type, out object xml) {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            return (xml = DeserializeFromStream(type, stream)) != null;
        }
    }

    public static object DeserializeFromFile(string path, Type type) {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
            return DeserializeFromStream(type, stream);
        }
    }

    public static object DeserializeFromStream<T>(Stream stream) => DeserializeFromStream(typeof(T), stream);
    
    public static object DeserializeFromStream(Type type, Stream stream) {
        try {
            var serializer = new XmlSerializer(type);
            return serializer.Deserialize(stream);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return null;
    }

    public static object DeserializeFromReader(Type type, TextReader reader) {
        try {
            var serializer = new XmlSerializer(type);
            return serializer.Deserialize(reader);
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return null;
    }
    
    #endregion
}
