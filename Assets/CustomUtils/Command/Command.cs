using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public abstract class Command {

    private Dictionary<string, Func<object>> dynamicParameters = new();

    private static Dictionary<string, Type> _commandTypeCacheDic;
    
    private const string IDENTIFIER_LITERAL = "@";
    private const string ASSIGN_LITERAL = ":";
    
    public abstract Task ExecuteAsync ();

    static Command() {
        if (_commandTypeCacheDic == null) {
            _commandTypeCacheDic = new();
            ReflectionManager.GetSubClassTypes<Command>()?.ForEach(x => _commandTypeCacheDic.AutoAdd(x.GetCustomAttributes(typeof(CommandAliasAttribute), false).FirstOrDefault() is CommandAliasAttribute targetAttribute ? targetAttribute.Alias : x.Name, x));
        }
    }
    
    public static Command Create(string commandLine) => Create(GetCommand(commandLine), ParseCommandParameters(commandLine));
    
    protected static Command Create (string commandName, Dictionary<string, string> parameterDic) {
        var commandType = FindCommandType(commandName);
        if (commandType is null) {
            Logger.TraceError($"{commandName} is Invalid Type, Checking {nameof(CommandAliasAttribute)}");
            return null;
        }

        var command = Activator.CreateInstance(commandType) as Command;
        var fieldInfos = commandType.GetProperties().Where(property => property.IsDefined(typeof(CommandParameterAttribute), false));
        foreach (var fieldInfo in fieldInfos) {
            var paramAttribute = fieldInfo.GetCustomAttributes(typeof(CommandParameterAttribute), false).FirstOrDefault() as CommandParameterAttribute;
            if (paramAttribute == null) {
                Logger.TraceError($"{nameof(paramAttribute)} is Null");
                return null;
            }

            var paramName = string.IsNullOrEmpty(paramAttribute.Alias) == false && parameterDic.ContainsKey(paramAttribute.Alias) ? paramAttribute.Alias : fieldInfo.Name;
            if (parameterDic.ContainsKey(paramName) == false) {
                if (paramAttribute.Optional == false) {
                    Logger.TraceLog($"'{commandType.Name}' is Miising '{paramName}' parameter");
                }
                
                continue;
            }

            BindDynamicParameter(command, fieldInfo.Name, parameterDic[paramName].Replace("\\{", "{").Replace("\\}", "}"), fieldInfo.PropertyType);
        }

        return command;
    }
    
    private static void BindDynamicParameter (Command command, string paramName, string paramValueString, Type paramType) => command.SetDynamicParameter(() => ParseParameterValue(paramValueString, paramType), paramName);
    
    private static Type FindCommandType (string typeName) {
        if (string.IsNullOrEmpty(typeName)) {
            Logger.TraceError($"{nameof(typeName)} is Null or Empty");
            return null;
        }

        return _commandTypeCacheDic.TryGetValue(typeName, out var type) ? type : _commandTypeCacheDic.Values.FirstOrDefault(x => x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }

    protected static string GetCommand(string commandLine) {
        var commandName = commandLine.GetBetween(IDENTIFIER_LITERAL, " ");
        return string.IsNullOrEmpty(commandName) ? commandLine.GetAfter(IDENTIFIER_LITERAL) : commandName;
    }
    
    protected static Dictionary<string, string> ParseCommandParameters(string scriptLineText) {
        var commandParamDic = new Dictionary<string, string>();
        var paramPairs = scriptLineText.Substring(scriptLineText.IndexOf(' ') + 1).Split(' ');
        foreach (var paramPair in paramPairs) {
            var paramName = paramPair.GetBefore(ASSIGN_LITERAL);
            var paramValue = paramPair.GetAfterFirst(ASSIGN_LITERAL);
            
            if (string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(paramValue)) {
                Logger.TraceError($"{nameof(paramName)} or {nameof(paramValue)} is Null or Empty || {paramName ?? string.Empty} || {paramValue ?? string.Empty}");
                return commandParamDic;
            }
            
            if (paramValue.WrappedIn("\"")) {
                paramValue = paramValue.Substring(1, paramValue.Length - 2);
            }

            paramValue = paramValue.Replace("\\\"", "\"");
            commandParamDic.Add(paramName, paramValue);
        }

        return commandParamDic;
    }
    
    protected static object ParseParameterValue(string paramValue, Type paramType) {
        if (string.IsNullOrEmpty(paramValue)) 
            return null;
        
        var nullableType = Nullable.GetUnderlyingType(paramType);
        if (nullableType != null) {
            paramType = nullableType;
        }

        if (paramType.IsEnum) {
            if (Enum.IsDefined(paramType, paramValue)) {
                return Enum.Parse(paramType, paramValue);
            }

            paramValue = paramValue.ToUpper();
            if (Enum.IsDefined(paramType, paramValue)) {
                return Enum.Parse(paramType, paramValue);
            }

            paramValue = paramValue.GetForceTitleCase();
            if (Enum.IsDefined(paramType, paramValue)) {
                return Enum.Parse(paramType, paramValue);
            }

            return default(Enum);
        }
        
        if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Named<>)) {
            var valueType = paramType.GetGenericArguments()[0];
            return Activator.CreateInstance(
                typeof(Named<>).MakeGenericType(valueType),
                paramValue.Contains(".") ? paramValue.GetBefore(".") : paramValue, 
                ParseParameterValue(paramValue.GetAfterFirst("."), 
                valueType));
        }

        if (paramType.IsArray) {
            var strValues = paramValue.Split(',');
            for (int i = 0; i < strValues.Length; i++) {
                if (string.IsNullOrEmpty(strValues[i])) strValues[i] = null;
            } 
            
            var objValues = strValues.Select(s => ParseParameterValue(s, paramType.GetElementType())).ToArray();
            var array = Array.CreateInstance(paramType.GetElementType(), objValues.Length);
            for (int i = 0; i < objValues.Length; i++) {
                array.SetValue(objValues[i], i);
            }
            
            return array;
        }

        // Simple value.
        try {
            return Convert.ChangeType(paramValue, paramType, System.Globalization.CultureInfo.InvariantCulture);
        } catch (Exception e) {
            return paramType.IsValueType ? Activator.CreateInstance(paramType) : null;
        }
    }

    protected TParameter GetDynamicParameter<TParameter>(TParameter defaultValue, [CallerMemberName] string parameterName = null) {
        if (string.IsNullOrWhiteSpace(parameterName)) {
            Logger.TraceError($"{parameterName} is Invalid parameter Name.");
            return defaultValue;
        }
        
        return dynamicParameters.TryGetValue(parameterName, out var getter) ? getter?.Invoke() is TParameter parameter ? parameter : defaultValue : defaultValue;
    }
    
    protected void SetDynamicParameter (object parameterValue, [CallerMemberName] string parameterName = null) {
#pragma warning disable IDE0039
        Func<object> GetValue = () => parameterValue;
#pragma warning restore IDE0039
        SetDynamicParameter(GetValue, parameterName);
    }

    protected void SetDynamicParameter(Func<object> parameterValue, [CallerMemberName] string parameterName = null) => dynamicParameters.AutoAdd(parameterName, parameterValue);

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    protected sealed class CommandAliasAttribute : Attribute {
        
        public string Alias { get; }

        public CommandAliasAttribute (string alias) {
            Alias = alias;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    protected sealed class CommandParameterAttribute : Attribute {
        
        public string Alias { get; }
        public bool Optional { get; }
        
        public CommandParameterAttribute (string alias = null, bool optional = false) {
            Alias = alias;
            Optional = optional;
        }
    }
    
    protected class Named<TValue> {
        
        public string name;
        public TValue value;

        public Named(string name, TValue value) {
            this.name = name;
            this.value = value;
        }
    }
}

