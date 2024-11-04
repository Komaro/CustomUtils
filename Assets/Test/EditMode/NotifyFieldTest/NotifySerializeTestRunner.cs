using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;

[Category(TestConstants.Category.NOTIFY)]
public class NotifySerializeTestRunner {

    private NotifyProperty<int> _field;
    private NotifyConvertTestSample _sample;
    private NotifyComplexConvertTestSample _complexSample;

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        _field = new NotifyProperty<int> { Value = 4533 };

        _sample = new NotifyConvertTestSample {
            intProperty = { Value = RandomUtil.GetRandom(0, 10000) },
            floatProperty = { Value = RandomUtil.GetRandom(0, 10000) },
            stringProperty = { Value = RandomUtil.GetRandom(20) },
        };

        _complexSample = new NotifyComplexConvertTestSample();
        _complexSample.intProperty.Value = RandomUtil.GetRandom(0, 10000);
        _complexSample.floatProperty.Value = RandomUtil.GetRandom(0, 10000);
        _complexSample.stringProperty.Value = RandomUtil.GetRandom(20);
        _complexSample.intCollection = new NotifyRecordCollection<int>(RandomUtil.GetRandoms(5));
        _complexSample.stringList = new NotifyRecordList<string>(RandomUtil.GetRandoms(10, _ => RandomUtil.GetRandom(10)));
        _complexSample.recordDictionary = new NotifyRecordDictionary<int, float>(RandomUtil.GetRandoms(10, index => new KeyValuePair<int, float>(index, RandomUtil.GetRandom(0.0f, 1000.0f))));
        _complexSample.structDictionary = new NotifyRecordDictionary<int, TestStruct>(RandomUtil.GetRandoms(10, index => new KeyValuePair<int, TestStruct>(index, new TestStruct(RandomUtil.GetRandom(20)))));
    }
    
    [Test(Description = "Notify Csv Convert Test")]
    public void NotifyCsvConvertTest() {
        var text = CsvUtil.Serialize(new[] { _field });
        Assert.IsNotNull(text);
        Logger.TraceLog($"\n{text.TrimEnd()}");
        Logger.TraceLog("Pass field csv serialize");

        var deserializeIntField = CsvUtil.DeserializeFromText<NotifyProperty<int>>(text).First();
        EvaluateNotifyProperty(deserializeIntField);
        Logger.TraceLog($"Pass {nameof(NotifyField)} csv convert test\n");
        Logger.TraceLog($"\n{deserializeIntField.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = CsvUtil.Serialize<NotifyPropertyClassMap<NotifyConvertTestSample>>(new[] { _sample });
        Assert.IsNotEmpty(text);
        Logger.TraceLog($"\n{text.TrimEnd()}");
        
        var deserializeSample = CsvUtil.DeserializeFromText<NotifyConvertTestSample, NotifyPropertyClassMap<NotifyConvertTestSample>>(text).First();
        Assert.IsNotNull(deserializeSample);
        EvaluateNotifyConvertTestSample(deserializeSample);
        Logger.TraceLog($"Pass {nameof(NotifyConvertTestSample)} csv convert test");
        Logger.TraceLog($"\n{deserializeSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");
    }

    [Test(Description = "Notify Json Convert Test")]
    public void NotifyJsonConvertTest() {
        var text = JsonConvert.SerializeObject(_field, Formatting.Indented);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass field json serialize");
        
        var deserializeIntProperty = JsonConvert.DeserializeObject<NotifyProperty<int>>(text);
        EvaluateNotifyProperty(deserializeIntProperty);
        Logger.TraceLog($"Pass {nameof(NotifyField)} json convert test\n");
        Logger.TraceLog($"{deserializeIntProperty.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = JsonConvert.SerializeObject(_complexSample, Formatting.Indented);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass sample json serialize");

        var deserializeTestSample = JsonConvert.DeserializeObject<NotifyComplexConvertTestSample>(text);
        EvaluateNotifyConvertTestSample(deserializeTestSample);
        Logger.TraceLog($"Pass {nameof(NotifyComplexConvertTestSample)} json convert test\n");
        Logger.TraceLog($"{deserializeTestSample.ToStringAllFields()}\n");
    }

    [Test(Description = "Notify Xml Convert Test")]
    public void NotifyXmlConvertTest() {
        var text = XmlUtil.Serialize(_field);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass field xml serialize");
        
        var deserializeIntField = XmlUtil.DeserializeAsClassFromText<NotifyProperty<int>>(text);
        EvaluateNotifyProperty(deserializeIntField);
        Logger.TraceLog($"Pass {nameof(NotifyField)} xml convert test\n");
        Logger.TraceLog($"{deserializeIntField.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = XmlUtil.Serialize(_complexSample);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass sample xml serialize");

        var deserializeTestSample = XmlUtil.DeserializeAsClassFromText<NotifyComplexConvertTestSample>(text);
        EvaluateNotifyConvertTestSample(deserializeTestSample);
        Logger.TraceLog($"Pass {nameof(NotifyComplexConvertTestSample)} xml convert test\n");
        Logger.TraceLog($"{deserializeTestSample.ToStringAllFields()}\n");
    }

    private void EvaluateNotifyProperty(NotifyProperty<int> property) {
        Assert.IsNotNull(property, $"\n{property.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(property)} null check");

        Assert.IsTrue(ReferenceEquals(_field, property) == false, $"\n{property.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(property)} reference check");
        
        Assert.IsTrue(_field == property, $"\n{property.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(property)} value check");
    }

    private void EvaluateNotifyConvertTestSample(NotifyConvertTestSample evaluateSample) {
        Assert.IsNotNull(evaluateSample, $"\n{evaluateSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(evaluateSample)} null check");

        var comparisonSample = evaluateSample is NotifyComplexConvertTestSample ? _complexSample : _sample;
        Assert.IsTrue(ReferenceEquals(comparisonSample, evaluateSample) == false, $"\n{evaluateSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(evaluateSample)} reference check");

        Assert.IsTrue(comparisonSample == evaluateSample, $"\n{evaluateSample.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}");
        Logger.TraceLog($"Pass {nameof(evaluateSample)} value check");
    }

    public record NotifyConvertTestSample {
        
        public NotifyProperty<int> intProperty = new();
        public NotifyProperty<float> floatProperty = new();
        public NotifyProperty<string> stringProperty = new();
    }

    public record NotifyComplexConvertTestSample : NotifyConvertTestSample {

        public NotifyRecordCollection<int> intCollection;
        public NotifyRecordList<string> stringList;
        public NotifyRecordDictionary<int, float> recordDictionary;
        public NotifyRecordDictionary<int, TestStruct> structDictionary;
        public NotifyRecordDictionary<int, TestClass> classDictionary = new();
    }
}

public class TestClass : IDisposable {

    public string text;
    
    public TestClass() { }
    public TestClass(string text) => this.text = text;

    ~TestClass() => Dispose();
    
    public void Dispose() { 
        // temp
    }
}

public struct TestStruct {
    
    public string text;

    public TestStruct(string text) => this.text = text;
}