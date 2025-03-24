using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Command {

    private static readonly Dictionary<string, Type> _commandTypeCacheDic;

    private readonly Dictionary<string, Func<object>> dynamicParameters = new();
    
    private const string IDENTIFIER_LITERAL = "@";
    private const string ASSIGN_LITERAL = ":";

    #region Common Parameter

    [CommandParameter("delay", true)] 
    public float delay { get => GetDynamicParameter(0.0f); set => SetDynamicParameter(value); }
    
    [CommandParameter("visible", true)]
    public bool isVisible { get => GetDynamicParameter(true); set => SetDynamicParameter(value); }

    #endregion
    
    public abstract Task ExecuteAsync();
    public virtual Task UndoAsync() => Task.CompletedTask;

    static Command() => _commandTypeCacheDic = ReflectionProvider.GetSubTypesOfType<Command>().ToDictionary(type => type.TryGetCustomAttribute<CommandAliasAttribute>(out var attribute) && string.IsNullOrEmpty(attribute.Alias) == false ? attribute.Alias : type.Name, type => type);

    public static Command Create(string commandLine) {
        commandLine = Regex.Replace(commandLine.Trim(), @"\s+", @" ");
        return Create(GetCommand(commandLine), ParseCommandParameters(commandLine));
    }

    protected static Command Create(string commandName, Dictionary<string, string> parameterDic) {
        var commandType = FindCommandType(commandName);
        if (commandType is null) {
            Logger.TraceError($"'{commandName}' is Invalid Type, Checking {nameof(CommandAliasAttribute)}.{nameof(CommandAliasAttribute.Alias)}");
            return null;
        }

        var command = Activator.CreateInstance(commandType) as Command;
        var fieldInfos = commandType.GetProperties().Where(property => property.IsDefined(typeof(CommandParameterAttribute), false));
        foreach (var fieldInfo in fieldInfos) {
            var paramAttribute = fieldInfo.GetCustomAttributes(typeof(CommandParameterAttribute), false).FirstOrDefault() as CommandParameterAttribute;
            if (paramAttribute == null) {
                Logger.TraceError($"'{nameof(paramAttribute)}' is Null. '{nameof(Command)}' must have '{nameof(CommandParameterAttribute)}'.");
                return null;
            }

            var paramName = string.IsNullOrEmpty(paramAttribute.Alias) == false && parameterDic.ContainsKey(paramAttribute.Alias) ? paramAttribute.Alias : fieldInfo.Name;
            if (parameterDic.ContainsKey(paramName) == false) {
                if (paramAttribute.Optional == false) {
                    Logger.TraceLog($"'{paramName}' is Missing parameter in '{commandName}'. If the parameter should be ignored, add an optional bool value to '{nameof(CommandParameterAttribute)}'.", Color.red);
                }
                
                continue;
            }

            BindDynamicParameter(command, fieldInfo.Name, parameterDic[paramName].Replace("\\{", "{").Replace("\\}", "}"), fieldInfo.PropertyType);
        }

        return command;
    }
    
    private static void BindDynamicParameter(Command command, string paramName, string paramValueString, Type paramType) => command.SetDynamicParameter(() => ParseParameterValue(paramValueString, paramType), paramName);
    
    private static Type FindCommandType(string typeName) {
        if (string.IsNullOrEmpty(typeName)) {
            Logger.TraceError($"'{nameof(typeName)}' is Null or Empty");
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
                Logger.TraceError($"{nameof(paramName)} or {nameof(paramValue)} is Null or Empty || {scriptLineText} || {paramName ?? string.Empty} || {paramValue ?? string.Empty}");
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
            return EnumUtil.TryConvertAllCase(paramType, paramValue, out var ob) ? ob : default(Enum);
        }
        
        if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(NameValuePair<>)) {
            var valueType = paramType.GetGenericArguments()[0];
            return Activator.CreateInstance(
                typeof(NameValuePair<>).MakeGenericType(valueType),
                paramValue.Contains(".") ? paramValue.GetBefore(".") : paramValue, 
                ParseParameterValue(paramValue.GetAfterFirst("."), valueType));
        }

        if (paramType.IsArray) {
            var strValues = paramValue.Split(',');
            for (var i = 0; i < strValues.Length; i++) {
                if (string.IsNullOrEmpty(strValues[i])) {
                    strValues[i] = null;
                }
            } 
            
            var objValues = strValues.Select(s => ParseParameterValue(s, paramType.GetElementType())).ToArray();
            var array = Array.CreateInstance(paramType.GetElementType(), objValues.Length);
            for (var i = 0; i < objValues.Length; i++) {
                array.SetValue(objValues[i], i);
            }
            
            return array;
        }

        // Simple value.
        try {
            return Convert.ChangeType(paramValue, paramType, System.Globalization.CultureInfo.InvariantCulture);
        } catch {
            return paramType.IsValueType ? Activator.CreateInstance(paramType) : null;
        }
    }

    protected TParameter GetDynamicParameter<TParameter>(TParameter defaultValue, [CallerMemberName] string parameterName = null) {
        if (string.IsNullOrWhiteSpace(parameterName)) {
            Logger.TraceError($"'{parameterName}' is Invalid parameter Name.");
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

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    protected sealed class CommandAliasAttribute : Attribute {
        
        public string Alias { get; }

        public CommandAliasAttribute (string alias) {
            Alias = alias;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    protected sealed class CommandParameterAttribute : Attribute {
        
        public string Alias { get; }
        public bool Optional { get; }
        
        public CommandParameterAttribute (string alias = null, bool optional = false) {
            Alias = alias;
            Optional = optional;
        }
    }
    
    protected class NameValuePair<TValue> {
        
        public string name;
        public TValue value;

        public NameValuePair(string name, TValue value) {
            this.name = name;
            this.value = value;
        }
    }
}

