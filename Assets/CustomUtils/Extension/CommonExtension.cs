using System.Linq;
using System.Reflection;
using System.Text;

public static class CommonExtension {

    private static StringBuilder _stringBuilder = new();

    public static string ToStringAllFields(this object ob) {
        if (_stringBuilder == null) {
            return string.Empty;
        }
        
        var type = ob.GetType();
        _stringBuilder.Clear();
        _stringBuilder.AppendLine(type.Name);
        
        foreach (var info in type.GetFields().Select(x => (x.Name, x.GetValue(ob)))) {
            _stringBuilder.AppendLine($"{info.Item1} || {info.Item2}");
        }

        return _stringBuilder.ToString();
    }
}
