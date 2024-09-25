using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[Priority(5)]
public class SampleXmlGameDBProvider : GameDBProvider {
    
    private Dictionary<Type, object> _dbDic = new();

    public override bool Init(IEnumerable<Type> dbTypes) {
        var typeDic = dbTypes.ToDictionary(type => type.Name, type => type);
        if (typeDic.Count <= 0) {
            Logger.TraceError($"{nameof(typeDic)} is empty");
            return false;
        }
        
        if (XmlUtil.TryDeserializeAsClassFromFile<SampleDBList>($"{Constants.Path.PROJECT_TEMP_PATH}/{"DBList".AutoSwitchExtension(Constants.Extension.XML)}", out var dbList)) {
            foreach (var name in dbList.names) {
                if (typeDic.TryGetValue(name, out var type) && type.BaseType != null) {
                    var dataType = type.BaseType.GetGenericArguments()[1];
                    var xmlType = typeof(SampleRawGameDB<>).MakeGenericType(dataType);
                    if (XmlUtil.TryDeserializeFromFile($"{Constants.Path.PROJECT_TEMP_PATH}/temp_db/{name.AutoSwitchExtension(Constants.Extension.XML)}", xmlType,out var db)) {
                        _dbDic.AutoAdd(dataType, db);
                    }
                }
            }

            return true;
        }

        return false;
    }

    public override List<T> GetDataList<T>() {
        if (_dbDic.TryGetValue(typeof(T), out var db) && db is SampleRawGameDB<T> data) {
            return data.data.ToList();
        }

        return new List<T>();
    }

    public override void Clear() => _dbDic?.Clear();
}
//
// public record SampleXmlGameData<T> {
//
//     [XmlElement]
//     public List<T> dataList = new();
//
//     public SampleXmlGameData() { }
//     public SampleXmlGameData(List<T> dataList) => this.dataList = dataList;
// }