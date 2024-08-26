using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[Priority(10)]
public class SampleJsonGameDBProvider : GameDBProvider {

    private Dictionary<Type, object> dbDic = new();

    public override void Init(IEnumerable<Type> dbTypes) {
        var typeDic = dbTypes.ToDictionary(type => type.Name, type => type);
        if (typeDic.Count <= 0) {
            Logger.TraceError($"{nameof(typeDic)} is empty");
            return;
        }

        if (JsonUtil.TryLoadJson<SampleJsonDBList>($"{Constants.Path.PROJECT_TEMP_PATH}/.temp_db/{"DBList".AutoSwitchExtension(Constants.Extension.JSON)}", out var dbList)) {
            foreach (var name in dbList.db) {
                if (typeDic.TryGetValue(name, out var type) && type.BaseType != null) {
                    var text = File.ReadAllText($"{Constants.Path.PROJECT_TEMP_PATH}/.temp_db/{name.AutoSwitchExtension(Constants.Extension.JSON)}");
                    if (string.IsNullOrEmpty(text)) {
                        Logger.TraceError($"{text} is null or empty || {name}");
                        continue;
                    }

                    var dataType = type.BaseType.GenericTypeArguments[1];
                    var jsonType = typeof(SampleJsonGameData<>).MakeGenericType(dataType);
                    if (JsonUtil.TryDeserialize(text, jsonType, out var ob)) {
                        dbDic.AutoAdd(dataType, ob);
                    }
                }
            }
        }
    }

    public override List<T> GetList<T>() {
        if (dbDic.TryGetValue(typeof(T), out var obj) && obj is SampleJsonGameData<T> data) {
            return data.dataList;
        }

        return new List<T>();
    }

    public override void Clear() => dbDic.Clear();
}

public record SampleJsonDBList {
    
    public List<string> db = new();
}

public record SampleJsonGameData<T> {

    public List<T> dataList = new();
    
    public SampleJsonGameData(List<T> dataList) => this.dataList = dataList;
}