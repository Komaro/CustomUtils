using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;

public static class CsvUtil {
    
    #region [Serialize]

    public static async Task SerializeToFileAsync(string path, CancellationToken token = default, params object[] records) {
        using (var writer = new StreamWriter(path)) {
            await SerializeToWriterAsync(writer, token, records);
        }
    }

    public static void SerializeToFile(string path, params object[] records) {
        using (var writer = new StreamWriter(path)) {
            SerializeToWriter(writer, records);
        }
    }

    public static async Task SerializeToWriterAsync(TextWriter writer, CancellationToken token = default, params object[] records) {
        try {
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                await csvWriter.WriteRecordsAsync(records, token);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static void SerializeToWriter(TextWriter writer, params object[] records) {
        try {
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                csvWriter.WriteRecords(records);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
    }
    
    public static async Task<string> SerializeAsync(Type type, params object[] records) {
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

    public static string Serialize(Type type, params object[] records) {
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

    #endregion

    #region [Deserialize]

    public static IEnumerable<object> DeserializeFromFile(string path, Type type) {
        using (var reader = new StreamReader(path)) {
            return DeserializeFromReader(reader, type);
        }
    }
    
    public static IEnumerable<object> DeserializeFromReader(TextReader reader, Type type) {
        try {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture)) {
                return csvReader.GetRecords(type);
            }
        } catch (Exception ex) {
            Logger.TraceError(ex);
        }
        
        return Enumerable.Empty<object>();
    }
    
    #endregion
}
