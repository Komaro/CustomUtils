using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

[Priority(5)]
public class SampleXmlGameDBProvider : GameDBProvider {
    
    private Dictionary<Type, object> _dbDic = new();

    public override void Init(IEnumerable<Type> dbTypes) {
        var typeDic = dbTypes.ToDictionary(type => type.Name, type => type);
        if (typeDic.Count <= 0) {
            Logger.TraceError($"{nameof(typeDic)} is empty");
            return;
        }
        
        if (XmlUtil.TryDeserializeAsClassFromFile<SampleXmlDBList>($"{Constants.Path.PROJECT_TEMP_PATH}/.temp_db/{"DBList".AutoSwitchExtension(Constants.Extension.XML)}", out var dbList)) {
            foreach (var name in dbList.db) {
                if (typeDic.TryGetValue(name, out var type) && type.BaseType != null) {
                    var dataType = type.BaseType.GetGenericArguments()[1];
                    var xmlType = typeof(SampleXmlGameData<>).MakeGenericType(dataType);
                    if (XmlUtil.TryDeserializeFromFile($"{Constants.Path.PROJECT_TEMP_PATH}/.temp_db/{name.AutoSwitchExtension(Constants.Extension.XML)}", xmlType,out var db)) {
                        _dbDic.AutoAdd(dataType, db);
                    }
                }
            }
        }
    }

    public override List<T> GetList<T>() {
        if (_dbDic.TryGetValue(typeof(T), out var db) && db is SampleXmlGameData<T> data) {
            return data.dataList;
        }

        return new List<T>();
    }

    public override void Clear() => _dbDic?.Clear();
}

[XmlRoot("DBList")]
public record SampleXmlDBList {

    [XmlElement]
    public string[] db;
}

[XmlRoot]
public record SampleXmlGameData<T> : ITestHandler {

    [XmlElement]
    public List<T> dataList = new();

    public SampleXmlGameData() { }
    public SampleXmlGameData(List<T> dataList) => this.dataList = dataList;
    
    public void StartTest(CancellationToken token) {
        
    }
}