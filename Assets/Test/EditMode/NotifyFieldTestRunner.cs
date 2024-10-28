using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using NUnit.Framework;

public class NotifyFieldTestRunner {

    private static readonly NotifyProperty<int> TEST_FIELD = new() { Value = 4533 };
    
    private static readonly NotifyConvertTestSample TEST_SAMPLE = new() {
        intProperty = { Value = 50 },
        floatProperty = { Value = 3.55f },
        stringProperty = { Value = "TestProperty" },
        innerSampleProperty = { Value = new NotifyConvertTestInnerSample {
            intField = 4455,
            stringField = "TestInnerField"
        } }
    };

    [Test(Description = "Notify Json Convert Test")]
    public void NotifyJsonConvertTest() {
        var text = JsonConvert.SerializeObject(TEST_FIELD);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass field json serialize");
        
        var deserializeIntProperty = JsonConvert.DeserializeObject<NotifyProperty<int>>(text);
        EvaluateNotifyProperty(deserializeIntProperty);
        Logger.TraceLog($"Pass {nameof(NotifyField)} json convert test\n");
        Logger.TraceLog($"{deserializeIntProperty.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = JsonConvert.SerializeObject(TEST_SAMPLE);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass sample json serialize");

        var deserializeTestSample = JsonConvert.DeserializeObject<NotifyConvertTestSample>(text);
        EvaluateNotifyConvertTestSample(deserializeTestSample);
        Logger.TraceLog($"Pass {nameof(NotifyConvertTestSample)} json convert test\n");
        Logger.TraceLog($"{deserializeTestSample.ToStringAllFields()}\n");
    }

    [Test(Description = "Notify Xml Convert Test")]
    public void NotifyXmlConvertTest() {
        var text = XmlUtil.Serialize<NotifyProperty<int>>(TEST_FIELD);
        Assert.IsNotNull(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass field xml serialize");
        
        var deserializeIntField = XmlUtil.DeserializeAsClassFromText<NotifyProperty<int>>(text);
        EvaluateNotifyProperty(deserializeIntField);
        Logger.TraceLog($"Pass {nameof(NotifyField)} xml convert test\n");
        Logger.TraceLog($"{deserializeIntField.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = XmlUtil.Serialize<NotifyConvertTestSample>(TEST_SAMPLE);
        Assert.IsNotEmpty(text);
        Logger.TraceLog(text);
        Logger.TraceLog("Pass sample xml serialize");

        var deserializeTestSample = XmlUtil.DeserializeAsClassFromText<NotifyConvertTestSample>(text);
        EvaluateNotifyConvertTestSample(deserializeTestSample);
        Logger.TraceLog($"Pass {nameof(NotifyConvertTestSample)} xml convert test\n");
        Logger.TraceLog($"{deserializeTestSample.ToStringAllFields()}\n");
    }

    [Test(Description = "Notify Csv Convert Test")]
    public void NotifyCsvConvertTest() {
        var text = CsvUtil.Serialize(new[] { TEST_FIELD });
        Assert.IsNotNull(text);
        Logger.TraceLog(text.TrimEnd());
        Logger.TraceLog("Pass field csv serialize");

        var deserializeIntField = CsvUtil.DeserializeFromText<NotifyProperty<int>>(text).First();
        EvaluateNotifyProperty(deserializeIntField);
        Logger.TraceLog($"Pass {nameof(NotifyField)} csv convert test\n");
        Logger.TraceLog($"{deserializeIntField.ToStringAllFields(bindingFlags: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).TrimEnd()}\n");

        text = CsvUtil.Serialize(new[] { TEST_SAMPLE });
        Assert.IsNotEmpty(text);
        // TODO. ClassMap 확장 없이 처리 불가능
    }

    private void EvaluateNotifyProperty(NotifyProperty<int> property) {
        Assert.IsNotNull(property);
        Logger.TraceLog($"Pass {nameof(property)} null check");

        Assert.IsTrue(ReferenceEquals(TEST_FIELD, property) == false);
        Logger.TraceLog($"Pass {nameof(property)} reference check");
        
        Assert.IsTrue(TEST_FIELD == property);
        Logger.TraceLog($"Pass {nameof(property)} value check");
    }

    private void EvaluateNotifyConvertTestSample(NotifyConvertTestSample testSample) {
        Assert.IsNotNull(testSample);
        Logger.TraceLog($"Pass {nameof(testSample)} null check");
        
        Assert.IsTrue(ReferenceEquals(TEST_SAMPLE, testSample) == false);
        Logger.TraceLog($"Pass {nameof(testSample)} reference check");

        Assert.IsTrue(TEST_SAMPLE == testSample);
        Logger.TraceLog($"Pass {nameof(testSample)} value check");
    }
}

public record NotifyConvertTestSample {

    public NotifyProperty<int> intProperty = new();
    public NotifyProperty<float> floatProperty = new();
    public NotifyProperty<string> stringProperty = new();
    public NotifyProperty<NotifyConvertTestInnerSample> innerSampleProperty = new();
}

public record NotifyConvertTestInnerSample {
        
    public int intField { get; set; }
    public string stringField { get; set; }
}