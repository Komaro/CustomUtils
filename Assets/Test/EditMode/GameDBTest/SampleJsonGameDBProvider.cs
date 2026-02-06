using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[Priority(11)]
public class SampleJsonGameDBProvider : GameDBProvider {

    private Dictionary<Type, object> dbDic = new();

    public override int Count => dbDic.Count;

    public override bool Init(IEnumerable<Type> dbTypes) {
        var typeDic = dbTypes.ToDictionary(type => type.Name, type => type);
        if (typeDic.Count <= 0) {
            Logger.TraceError($"{nameof(typeDic)} is empty");
            return false;
        }

        if (JsonUtil.TryLoadJson<SampleDBList>($"{Constants.Path.PROJECT_TEMP_PATH}/{"DBList".AutoSwitchExtension(Constants.Extension.JSON)}", out var dbList)) {
            foreach (var name in dbList.names) {
                if (typeDic.TryGetValue(name, out var type) && type.BaseType != null) {
                    var text = File.ReadAllText($"{Constants.Path.PROJECT_TEMP_PATH}/temp_db/{name.AutoSwitchExtension(Constants.Extension.JSON)}");
                    if (string.IsNullOrEmpty(text)) {
                        Logger.TraceError($"{text} is null or empty || {name}");
                        continue;
                    }

                    var dataType = type.BaseType.GenericTypeArguments[1];
                    var jsonType = typeof(SampleRawGameDB<>).MakeGenericType(dataType);
                    if (JsonUtil.TryDeserialize(text, jsonType, out var ob)) {
                        dbDic.AutoAdd(dataType, ob);
                    }
                }
            }

            return true;
        }
        
        return false;
    }

    public override IEnumerable<TData> GetData<TData>() => dbDic.TryGetValue(typeof(TData), out var obj) && obj is SampleRawGameDB<TData> rawData ? rawData.data : Enumerable.Empty<TData>();

    public override void Clear() => dbDic.Clear();
}