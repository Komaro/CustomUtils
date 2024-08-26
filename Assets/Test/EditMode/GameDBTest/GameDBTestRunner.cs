using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

public class GameDBTestRunner {

    private const string DB_LIST = "DBList";
    private const string TEST_GAME_SAMPLE_DB_01 = nameof(TestSampleGameDB_01);
    private const string TEST_GAME_SAMPLE_DB_02 = nameof(TestSampleGameDB_02);

    private static readonly string TEST_TEMP_FOLDER_PATH = $"{Constants.Path.PROJECT_TEMP_PATH}/.temp_db";

    [SetUp]
    public void SetUp() {
        AssetBundle.UnloadAllAssetBundles(true);
        SystemUtil.EnsureDirectoryExists(TEST_TEMP_FOLDER_PATH);
    }

    [OneTimeTearDown]
    public void TearDown() {
        SystemUtil.DeleteDirectory(TEST_TEMP_FOLDER_PATH);
        
        Service.RemoveService<GameDBService>();
        Service.RemoveService<ResourceService>();
    }

    [TestCase(typeof(JsonTestRunner))]
    [TestCase(typeof(XmlTestRunner))]
    [TestCase(typeof(CsvTestRunner))]
    public void StartTest(Type type) {
        if (SystemUtil.TryCreateInstance<ITestHandler>(out var handler, type)) {
            RunHandler(handler);
        }
    }
    
    public void RunHandler(ITestHandler handler) {
        handler.SetUp();
        handler.StartTest(CancellationToken.None);
        handler.TearDown();
    }

    #region [Json]
    
    public class JsonTestRunner : ITestHandler {
        
        private static readonly string DB_LIST_JSON = $"{DB_LIST}{Constants.Extension.JSON}";
        private static readonly string TEST_GAME_DB_JSON = $"{TEST_GAME_SAMPLE_DB_01}{Constants.Extension.JSON}";
    
        private static readonly string JSON_DB_LIST_JSON_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{DB_LIST_JSON}";
        private static readonly string TEST_GAME_DB_JSON_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_JSON}";
        
        public void SetUp() {
            if (File.Exists(JSON_DB_LIST_JSON_FULL_PATH) == false) {
                var jsonData = new SampleJsonDBList {
                    db = new List<string> {
                        TEST_GAME_SAMPLE_DB_01
                    }
                };

                JsonUtil.SaveJson(JSON_DB_LIST_JSON_FULL_PATH, jsonData);
            }

            if (File.Exists(TEST_GAME_DB_JSON_FULL_PATH) == false) {
                var dummyData = new SampleJsonGameData<TestSampleGameDB_01.TestData>(new List<TestSampleGameDB_01.TestData> {
                    new() { index = 0, text = "Test 0" },
                    new() { index = 1, text = "Test 1" },
                    new() { index = 2, text = "Test 2" },
                    new() { index = 3, text = "Test 3" },
                });
                
                JsonUtil.SaveJson(TEST_GAME_DB_JSON_FULL_PATH, dummyData);
            }
        }

        public void StartTest(CancellationToken token) {
            var db = Service.GetService<GameDBService>().Get<TestSampleGameDB_01>();
            if (db == null) {
                Assert.Fail();
            }

            for (var count = 0; count < 20; count++) {
                if (db.TryGet(RandomUtil.GetRandom(0, 4), out var data) == false) {
                    Assert.Fail();
                }
                
                Logger.TraceLog(data.text);
            }
        }
    }

    #endregion

    #region [Xml]
    
    public class XmlTestRunner : ITestHandler {
        
        private static readonly string DB_LIST_XML = $"{DB_LIST}{Constants.Extension.XML}";
        private static readonly string TEST_GAME_DB_01_XML = $"{TEST_GAME_SAMPLE_DB_01}{Constants.Extension.XML}";
        private static readonly string TEST_GAME_DB_02_XML = $"{TEST_GAME_SAMPLE_DB_02}{Constants.Extension.XML}";

        private static readonly string DB_LIST_XML_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{DB_LIST_XML}";
        private static readonly string TEST_GAME_DB_XML_01_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_01_XML}";
        private static readonly string TEST_GAME_DB_XML_02_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_02_XML}";

        public void SetUp() {
            if (File.Exists(DB_LIST_XML_FULL_PATH) == false) {
                var xmlData = new SampleXmlDBList {
                    db = new string[] { TEST_GAME_SAMPLE_DB_01, TEST_GAME_SAMPLE_DB_02 },
                };
                
                XmlUtil.SerializeToFile(DB_LIST_XML_FULL_PATH, typeof(SampleXmlDBList), xmlData);
            }
            
            if (File.Exists(TEST_GAME_DB_XML_01_FULL_PATH) == false) {
                var dummyData = new SampleXmlGameData<TestSampleGameDB_01.TestData>(new List<TestSampleGameDB_01.TestData> {
                    new() { index = 0, text = "Test 0" },
                    new() { index = 1, text = "Test 1" },
                    new() { index = 2, text = "Test 2" },
                    new() { index = 3, text = "Test 3" },
                });
                
                XmlUtil.SerializeToFile(TEST_GAME_DB_XML_01_FULL_PATH, typeof(SampleXmlGameData<TestSampleGameDB_01.TestData>), dummyData);
            }

            if (File.Exists(TEST_GAME_DB_XML_02_FULL_PATH) == false) {
                var dummyData = new SampleXmlGameData<TestSampleGameDB_02.TestData>(new List<TestSampleGameDB_02.TestData> {
                    new() { index = 0, type = TestSampleGameDB_02.TEST_SAMPLE_TYPE.NONE },
                    new() { index = 1, type = TestSampleGameDB_02.TEST_SAMPLE_TYPE.FIRST },
                    new() { index = 2, type = TestSampleGameDB_02.TEST_SAMPLE_TYPE.SECOND },
                    new() { index = 3, type = TestSampleGameDB_02.TEST_SAMPLE_TYPE.FIRST },
                });
                
                XmlUtil.SerializeToFile(TEST_GAME_DB_XML_02_FULL_PATH, typeof(SampleXmlGameData<TestSampleGameDB_02.TestData>), dummyData);
            }
        }

        public void StartTest(CancellationToken token) {
            if (Service.GetService<GameDBService>().TryGet<TestSampleGameDB_01>(out var db_01)) {
                for (var count = 0; count < 20; count++) {
                    if (db_01.TryGet(RandomUtil.GetRandom(0, db_01.Length), out var data)) {
                        Logger.TraceLog(data.text);
                    }
                }
            } else {
                Assert.Fail();
            }

            if (Service.GetService<GameDBService>().TryGet<TestSampleGameDB_02>(out var db_02)) {
                for (var count = 0; count < 20; count++) {
                    if (db_02.TryGet((uint)RandomUtil.GetRandom(0, db_02.Length), out var data)) {
                        Logger.TraceLog(data.type);
                    }
                }
            } else {
                Assert.Fail();
            }
        }
    }
    
    #endregion

    #region [Csv]

    public class CsvTestRunner : ITestHandler {
        
        public void SetUp() {
            
        }

        public void TearDown() { }

        public void StartTest(CancellationToken token) {
            
        }
    }
    
    #endregion
}

public class TestSampleGameDB_01 : GameDB<int, TestSampleGameDB_01.TestData> {
    
    public TestSampleGameDB_01(GameDBProvider provider) : base(provider) { }
    protected override int CreateKey(TestData data) => data.index;
    
    public record TestData {

        public int index;
        public string text;
    }
}

public class TestSampleGameDB_02 : GameDB<uint, TestSampleGameDB_02.TestData> {
    
    public TestSampleGameDB_02(GameDBProvider provider) : base(provider) { }

    protected override uint CreateKey(TestData data) => data.index;
    
    public record TestData {
        
        public uint index;
        public TEST_SAMPLE_TYPE type;
    }

    public enum TEST_SAMPLE_TYPE {
        NONE,
        FIRST,
        SECOND,
    }
}