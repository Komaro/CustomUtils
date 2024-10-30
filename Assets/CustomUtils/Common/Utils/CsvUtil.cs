using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

public static class CsvUtil {
    
    #region [Serialize]

    public static async Task SerializeToFileAsync(string path, IEnumerable records, CancellationToken token = default) {
        using (var writer = new StreamWriter(path)) {
            await SerializeToWriterAsync(writer, records, token);
        }
    }

    public static void SerializeToFile(string path, IEnumerable records) {
        using (var writer = new StreamWriter(path)) {
            SerializeToWriter(writer, records);
        }
    }

    public static async Task SerializeToWriterAsync(TextWriter writer, IEnumerable records, CancellationToken token = default) {
        try {
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                await csvWriter.WriteRecordsAsync(records, token);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static void SerializeToWriter(TextWriter writer, IEnumerable records) {
        try {
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                csvWriter.WriteRecords(records);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static async Task<string> SerializeAsync(IEnumerable records) {
        try {
            using (var stringWriter = new StringWriter(new StringBuilder()))
            using (var csvWriter = new CsvWriter(stringWriter, CultureInfo.InvariantCulture)) {
                await csvWriter.WriteRecordsAsync(records);
                return stringWriter.ToString();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return string.Empty;
    }

    public static string Serialize(IEnumerable records) {
        try {
            using (var stringWriter = new StringWriter(new StringBuilder()))
            using (var csvWriter = new CsvWriter(stringWriter, CultureInfo.InvariantCulture)) {
                csvWriter.WriteRecords(records);
                return stringWriter.ToString();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return string.Empty;
    }

    public static string Serialize<TClassMap>(IEnumerable records) where TClassMap : ClassMap {
        using (var stringWriter = new StringWriter(new StringBuilder())) 
        using (var csvWriter = new CsvWriter(stringWriter, CultureInfo.InvariantCulture)) {
            csvWriter.Context.RegisterClassMap(typeof(TClassMap));
            csvWriter.WriteRecords(records);
            return stringWriter.ToString();
        }
    }

    #endregion

    #region [Deserialize]

    public static bool TryDeserializeFromText<T>(string text, out IEnumerable<T> csv) => (csv = DeserializeFromText<T>(text))?.Any() ?? false;
    
    public static IEnumerable<T> DeserializeFromText<T>(string text) {
        using (var reader = new StringReader(text)) {
            return DeserializeFromReader<T>(reader);
        }
    }
    
    public static bool TryDeserializeFromText<T, TClassMap>(string text, out IEnumerable<T> csv) where TClassMap : ClassMap => (csv = DeserializeFromText<T>(text))?.Any() ?? false;
    
    public static IEnumerable<T> DeserializeFromText<T, TClassMap>(string text) where TClassMap : ClassMap {
        using (var reader = new StringReader(text)) {
            return DeserializeFromReader<T, TClassMap>(reader);
        }
    }

    public static async Task<IEnumerable<T>> DeserializeFromFileAsync<T>(string path, CancellationToken token = default) {
        using (var reader = new StreamReader(path)) {
            return await DeserializeFromReaderAsync<T>(reader, token);
        }
    }

    public static async Task<IEnumerable<object>> DeserializeFromFileAsync(string path, Type type, CancellationToken token = default) {
        using (var reader = new StreamReader(path)) {
            return await DeserializeFromReaderAsync(reader, type, token);
        }
    }
    
    public static bool TryDeserializeFromFile<T>(string path, out IEnumerable<T> csv) => (csv = DeserializeFromFile<T>(path)).Equals(Enumerable.Empty<T>()) == false;
    public static bool TryDeserializeFromFile(string path, Type type, out IEnumerable<object> csv) => (csv = DeserializeFromFile(path, type)).Equals(Enumerable.Empty<object>()) == false;

    public static IEnumerable<T> DeserializeFromFile<T>(string path) {
        using (var reader = new StreamReader(path)) {
            return DeserializeFromReader<T>(reader);
        }
    }
    
    public static IEnumerable<object> DeserializeFromFile(string path, Type type) {
        using (var reader = new StreamReader(path)) {
            return DeserializeFromReader(reader, type);
        }
    }
    
    public static async Task<IEnumerable<T>> DeserializeFromReaderAsync<T>(TextReader reader, CancellationToken token = default) {
        try {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                return await csvReader.GetRecordsAsync<T>(token).ToEnumerable();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return Enumerable.Empty<T>();
    }

    public static async Task<IEnumerable<object>> DeserializeFromReaderAsync(TextReader reader, Type type, CancellationToken token = default) {
        try {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                return await csvReader.GetRecordsAsync(type, token).ToEnumerable();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return Enumerable.Empty<object>();
    }

    public static bool TryDeserializeFromReader<T>(TextReader reader, out IEnumerable<T> csv) => (csv = DeserializeFromReader<T>(reader)).Equals(Enumerable.Empty<T>()) == false;
    public static bool TryDeserializeFromReader(TextReader reader, Type type, out IEnumerable<object> csv) => (csv = DeserializeFromReader(reader, type)).Equals(Enumerable.Empty<object>()) == false;

    public static IEnumerable<T> DeserializeFromReader<T>(TextReader reader) {
        try {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                return csvReader.GetRecords<T>().ToImmutableArray();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return Enumerable.Empty<T>();
    }

    public static IEnumerable<T> DeserializeFromReader<T, TClassMap>(TextReader reader) where TClassMap : ClassMap {
        try {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                csvReader.Context.RegisterClassMap<TClassMap>();
                return csvReader.GetRecords<T>().ToImmutableArray();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }

        return Enumerable.Empty<T>();
    }

    public static IEnumerable<object> DeserializeFromReader(TextReader reader, Type type) {
        try {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                return csvReader.GetRecords(type).ToImmutableArray();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return Enumerable.Empty<object>();
    }
    
    public static IEnumerable<object> DeserializeFromReader<TClassMap>(TextReader reader, Type type) where TClassMap : ClassMap {
        try {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                csvReader.Context.RegisterClassMap<TClassMap>();
                return csvReader.GetRecords(type).ToImmutableArray();
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return Enumerable.Empty<object>();
    }

    #endregion
}
