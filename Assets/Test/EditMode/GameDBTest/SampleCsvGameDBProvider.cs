using System;
using System.Collections.Generic;
using System.Linq;

[Priority(1)]
public class SampleCsvGameDBProvider : GameDBProvider {
    
    private Dictionary<Type, object> _dbDic = new();

    public override bool Init(IEnumerable<Type> dbTypes) {
        var typeDic = dbTypes.ToDictionary(type => type.Name, type => type);
        if (typeDic.Count <= 0) {
            Logger.TraceError($"{nameof(typeDic)} is empty");
            return false;
        }
        
        if (CsvUtil.TryDeserializeFromFile<SampleDBName>($"{Constants.Path.PROJECT_TEMP_PATH}/{"DBList".AutoSwitchExtension(Constants.Extension.CSV)}", out var dbNames)) {
            foreach (var name in dbNames.Select(x => x.name)) {
                if (typeDic.TryGetValue(name, out var type) && type.BaseType != null) {
                    var dataType = type.BaseType.GetGenericArguments()[1];
                    if (CsvUtil.TryDeserializeFromFile($"{Constants.Path.PROJECT_TEMP_PATH}/.temp_db/{name.AutoSwitchExtension(Constants.Extension.CSV)}", dataType, out var csv)) {
                        var csvType = typeof(SampleRawGameDB<>).MakeGenericType(dataType);
                        if (SystemUtil.TryCreateInstance(out var db, csvType, csv)) {
                            _dbDic.AutoAdd(dataType, db);
                        }
                    }
                }
            }

            return true;
        }

        return false;
    }

    public override List<T> GetDataList<T>() {
        if (_dbDic.TryGetValue(typeof(T), out var ob) && ob is SampleRawGameDB<T> rawData) {
            return rawData.data.ToList();
        }

        return new List<T>();
    }

    public override void Clear() => _dbDic?.Clear();
}