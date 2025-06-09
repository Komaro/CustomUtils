using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.PerformanceTesting;

[Category(TestConstants.Category.FUNCTIONAL)]
[Category(TestConstants.Category.PERFORMANCE)]
public class DynamicMethodTestRunner {
        
    [Performance]
    [TestCase(50, 100)]
    [TestCase(50, 200)]
    [TestCase(50, 300)]
    [TestCase(50, 400)]
    [TestCase(50, 500)]
    [TestCase(50, 600)]
    [TestCase(50, 700)]
    [TestCase(50, 800)]
    [TestCase(50, 900)]
    [TestCase(50, 1000)]
    public void ConfigGetFieldsPerformanceTest(int measurementCount, int count) {
        var reflectionGroup = new SampleGroup("Reflection");
        var dynamicMethodGroup = new SampleGroup("DynamicMethod");
        
        var obj = new GetFieldsTestClass();
        var type = obj.GetType();
        
        Measure.Method(() => {
            foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(x => x.IsDefined<JsonIgnoreAttribute>() == false)) {
                if (info.FieldType.IsArray) {
                    _ = info.GetValue(obj) as Array;
                } else if (info.FieldType.IsGenericCollectionType()) {
                    _ = info.GetValue(obj) is ICollection collection ? collection.CloneEnumerator() : null;
                } else {
                    _ = info.GetValue(obj);
                }
            }
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(reflectionGroup).Run();
        
        var getFieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Instance).WhereSelect(info => info.IsDefined<JsonIgnoreAttribute>() == false, info => (info, DynamicMethodProvider.GetFieldValueFunc(obj, info)));
        Measure.Method(() => {
            foreach (var (info, func) in getFieldInfos) {
                if (info.FieldType.IsArray) {
                    _ = func(obj) as Array;
                } else if (info.FieldType.IsGenericCollectionType()) {
                    _ = func(obj) is ICollection collection ? collection.CloneEnumerator() : null;
                } else {
                    _ = func(obj);
                }
            }
        }).WarmupCount(2).MeasurementCount(measurementCount).IterationsPerMeasurement(count).SampleGroup(dynamicMethodGroup).Run();
    }

    private class GetFieldsTestClass {

        public int intValue = 5;
        public string stringValue = "TEST";
        public string[] stringArray = {"1", "2", "3", "4"};
        public List<int> intList = new(new [] {1, 2, 3, 4});
    }
}