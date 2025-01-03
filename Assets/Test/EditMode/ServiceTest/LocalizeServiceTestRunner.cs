using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[Category(TestConstants.Category.SERVICE)]
public class LocalizeServiceTestRunner {

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        Service.StartService<LocalizeService>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        Service.RemoveService<LocalizeService>();
    }

    [Test]
    public void LocalizeServiceTest() {
        if (Service.TryGetService<LocalizeService>(out var service)) {
            service.ChangeLanguageType(TestLanguageType.KO);
            Logger.TraceLog(service.Get("1"));
            Logger.TraceLog(service.Get("2"));
            Logger.TraceLog(service.Get("3"));
            
            service.ChangeLanguageType(TestLanguageType.EN);
            Logger.TraceLog(service.Get("1"));
            Logger.TraceLog(service.Get("2"));
            Logger.TraceLog(service.Get("3"));
            
            service.ChangeLanguageType(TestLanguageType.JP);
            Logger.TraceLog(service.Get("1"));
            Logger.TraceLog(service.Get("2"));
            Logger.TraceLog(service.Get("3"));
            
            service.ChangeProvider(new TestLocalizeServiceProvider());
        }
    }
}

[Priority(99999)]
public class TestLocalizeServiceProvider : LocalizeServiceProvider {

    private Dictionary<Enum, Dictionary<string, string>> _cacheDic = new() {
        { TestLanguageType.KO, new Dictionary<string, string> { { "1", "KO_TEST_1" }, { "2", "KO_TEST_2" }, { "3", "KO_TEST_3" }, } },
        { TestLanguageType.EN, new Dictionary<string, string> { { "1", "EN_TEST_1" }, { "2", "EN_TEST_2" }, { "3", "EN_TEST_3" }, } },
        { TestLanguageType.JP, new Dictionary<string, string> { { "1", "JP_TEST_1" }, { "2", "JP_TEST_2" }, { "3", "JP_TEST_3" }, } }
    };

    public override bool IsReady() => true;

    public override Dictionary<string, string> Get(Enum languageType) {
        if (_cacheDic.TryGetValue(languageType, out var dic)) {
            return dic;
        }

        return new Dictionary<string, string>();
    }
}

[LanguageEnum]
public enum TestLanguageType {
    KO,
    EN,
    JP,
}