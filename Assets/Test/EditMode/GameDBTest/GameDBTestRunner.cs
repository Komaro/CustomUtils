using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameDBTestRunner {

    private const string DB_LIST = "DBList";
    private const string TEST_GAME_SAMPLE_DB_01 = nameof(TestSampleGameDB_01);
    private const string TEST_GAME_SAMPLE_DB_02 = nameof(TestSampleGameDB_02);

    private static readonly string TEST_TEMP_FOLDER_PATH = $"{Constants.Path.PROJECT_TEMP_PATH}/temp_db";

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

    public abstract class GameDBTestRunnerBase : ITestHandler {

        public virtual void SetUp() => LogAssert.ignoreFailingMessages = false;
        public virtual void TearDown() => LogAssert.ignoreFailingMessages = true;

        public virtual void StartTest(CancellationToken token) {
            if (Service.TryGetService<GameDBService>(out var service)) {
                if (service.IsNullProvider()) {
                    Assert.Fail();
                }
                
                if (service.TryGet<TestSampleGameDB_01>(out var db_01)) {
                    for (var count = 0; count < 20; count++) {
                        if (db_01.TryGet(RandomUtil.GetRandom(0, db_01.Length), out var data)) {
                            Logger.TraceLog(data.text);
                        }
                    }
                } else {
                    Assert.Fail();
                }

                if (service.TryGet<TestSampleGameDB_02>(out var db_02)) {
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
    }

    #region [Json]
    
    public class JsonTestRunner : GameDBTestRunnerBase {
        
        private static readonly string DB_LIST_JSON = $"{DB_LIST}{Constants.Extension.JSON}";
        private static readonly string TEST_GAME_DB_01_JSON = $"{TEST_GAME_SAMPLE_DB_01}{Constants.Extension.JSON}";
        private static readonly string TEST_GAME_DB_02_JSON = $"{TEST_GAME_SAMPLE_DB_02}{Constants.Extension.JSON}";
    
        private static readonly string JSON_DB_LIST_JSON_FULL_PATH = $"{Constants.Path.PROJECT_TEMP_PATH}/{DB_LIST_JSON}";
        private static readonly string TEST_GAME_DB_JSON_01_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_01_JSON}";
        private static readonly string TEST_GAME_DB_JSON_02_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_02_JSON}";
        
        public override void SetUp() {
            base.SetUp();
            
            if (File.Exists(JSON_DB_LIST_JSON_FULL_PATH) == false) {
                var data = new SampleDBList {
                    names = new[] { TEST_GAME_SAMPLE_DB_01, TEST_GAME_SAMPLE_DB_02 }
                };

                JsonUtil.SaveJson(JSON_DB_LIST_JSON_FULL_PATH, data);
            }

            // TODO. 저장한 데이터와  원본 데이터간의 비교 테스트가 없음. JsonUtil.Load()를 통해 저장된 데이터 획득 후 추가 확인 필요
            if (File.Exists(TEST_GAME_DB_JSON_01_FULL_PATH) == false) {
                var data = new SampleRawGameDB<TestSampleGameDB_01.TestData>(TestSampleGameDB_01.CreateSamples());
                JsonUtil.SaveJson(TEST_GAME_DB_JSON_01_FULL_PATH, data);
            }

            if (File.Exists(TEST_GAME_DB_JSON_02_FULL_PATH) == false) {
                var data = new SampleRawGameDB<TestSampleGameDB_02.TestData>(TestSampleGameDB_02.CreateSamples());
                JsonUtil.SaveJson(TEST_GAME_DB_JSON_02_FULL_PATH, data);
            }
        }
    }

    #endregion

    #region [Xml]
    
    public class XmlTestRunner : GameDBTestRunnerBase {
        
        private static readonly string DB_LIST_XML = $"{DB_LIST}{Constants.Extension.XML}";
        private static readonly string TEST_GAME_DB_01_XML = $"{TEST_GAME_SAMPLE_DB_01}{Constants.Extension.XML}";
        private static readonly string TEST_GAME_DB_02_XML = $"{TEST_GAME_SAMPLE_DB_02}{Constants.Extension.XML}";

        private static readonly string DB_LIST_XML_FULL_PATH = $"{Constants.Path.PROJECT_TEMP_PATH}/{DB_LIST_XML}";
        private static readonly string TEST_GAME_DB_XML_01_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_01_XML}";
        private static readonly string TEST_GAME_DB_XML_02_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_02_XML}";

        public override void SetUp() {
            base.SetUp();
            
            if (File.Exists(DB_LIST_XML_FULL_PATH) == false) {
                var data = new SampleDBList {
                    names = new[] { TEST_GAME_SAMPLE_DB_01, TEST_GAME_SAMPLE_DB_02 },
                };
                
                XmlUtil.SerializeToFile(DB_LIST_XML_FULL_PATH, typeof(SampleDBList), data);
            }
            
            if (File.Exists(TEST_GAME_DB_XML_01_FULL_PATH) == false) {
                var data = new SampleRawGameDB<TestSampleGameDB_01.TestData>(TestSampleGameDB_01.CreateSampleList());
                XmlUtil.SerializeToFile(TEST_GAME_DB_XML_01_FULL_PATH, typeof(SampleRawGameDB<TestSampleGameDB_01.TestData>), data);
            }

            if (File.Exists(TEST_GAME_DB_XML_02_FULL_PATH) == false) {
                var data = new SampleRawGameDB<TestSampleGameDB_02.TestData>(TestSampleGameDB_02.CreateSampleList());
                XmlUtil.SerializeToFile(TEST_GAME_DB_XML_02_FULL_PATH, typeof(SampleRawGameDB<TestSampleGameDB_02.TestData>), data);
            }
        }
    }
    
    #endregion

    #region [Csv]

    public class CsvTestRunner : GameDBTestRunnerBase {
    
        private static readonly string DB_LIST_CSV = $"{DB_LIST}{Constants.Extension.CSV}";
        private static readonly string TEST_GAME_DB_01_CSV = $"{TEST_GAME_SAMPLE_DB_01}{Constants.Extension.CSV}";
        private static readonly string TEST_GAME_DB_02_CSV = $"{TEST_GAME_SAMPLE_DB_02}{Constants.Extension.CSV}";

        private static readonly string DB_LIST_CSV_FULL_PATH = $"{Constants.Path.PROJECT_TEMP_PATH}/{DB_LIST_CSV}";
        private static readonly string TEST_GAME_DB_CSV_01_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_01_CSV}";
        private static readonly string TEST_GAME_DB_CSV_02_FULL_PATH = $"{TEST_TEMP_FOLDER_PATH}/{TEST_GAME_DB_02_CSV}";
        
        public override void SetUp() {
            base.SetUp();
            
            if (File.Exists(DB_LIST_CSV) == false) {
                var records = new List<SampleDBName> {
                    new() { name = TEST_GAME_SAMPLE_DB_01 },
                    new() { name = TEST_GAME_SAMPLE_DB_02 },
                };

                CsvUtil.SerializeToFile(DB_LIST_CSV_FULL_PATH, records);
            }
        
            if (File.Exists(TEST_GAME_DB_CSV_01_FULL_PATH) == false) {
                var csv = new SampleRawGameDB<TestSampleGameDB_01.TestData>(TestSampleGameDB_01.CreateSamples());
                CsvUtil.SerializeToFile(TEST_GAME_DB_CSV_01_FULL_PATH, csv.data);
            }

            if (File.Exists(TEST_GAME_DB_CSV_02_FULL_PATH) == false) {
                var csv = new SampleRawGameDB<TestSampleGameDB_02.TestData>(TestSampleGameDB_02.CreateSamples());
                CsvUtil.SerializeToFile(TEST_GAME_DB_CSV_02_FULL_PATH, csv.data);
            }
        }
    }
    
    #endregion
}

#region [Sample Data]

public record SampleDBList {

    public string[] names { get; set; }
}

public record SampleDBName {
    
    public string name { get; set; }
}

public record SampleRawGameDB<T> {

    public T[] data { get; set; }

    public SampleRawGameDB() { }
    public SampleRawGameDB(IEnumerable<object> data) => this.data = data.OfType<T>().ToArray();
    public SampleRawGameDB(IEnumerable<T> data) => this.data = data.ToArray();
}

public class TestSampleGameDB_01 : GameDB<int, TestSampleGameDB_01.TestData> {

    public TestSampleGameDB_01(GameDBProvider provider) : base(provider) { }
    protected override int CreateKey(TestData data) => data.index;

    public static TestData[] CreateSamples() => new[] {
        new TestData { index = 0, text = "Test 0" },
        new TestData { index = 1, text = "Test 1" },
        new TestData { index = 2, text = "Test 2" },
        new TestData { index = 3, text = "Test 3" },
    };

    public static List<TestData> CreateSampleList() => new() {
        new TestData { index = 0, text = "Test 0" },
        new TestData { index = 1, text = "Test 1" },
        new TestData { index = 2, text = "Test 2" },
        new TestData { index = 3, text = "Test 3" },
    };

    public record TestData {

        public int index { get; set; }
        public string text { get; set; }
    }
}

public class TestSampleGameDB_02 : GameDB<uint, TestSampleGameDB_02.TestData> {
    
    public TestSampleGameDB_02(GameDBProvider provider) : base(provider) { }

    protected override uint CreateKey(TestData data) => data.index;

    public static TestData[] CreateSamples() => new[] {
        new TestData { index = 0, type = TEST_SAMPLE_TYPE.NONE },
        new TestData { index = 1, type = TEST_SAMPLE_TYPE.FIRST },
        new TestData { index = 2, type = TEST_SAMPLE_TYPE.SECOND },
        new TestData { index = 3, type = TEST_SAMPLE_TYPE.FIRST },
    };
    
    public static List<TestData> CreateSampleList() => new() {
        new TestData { index = 0, type = TEST_SAMPLE_TYPE.NONE },
        new TestData { index = 1, type = TEST_SAMPLE_TYPE.FIRST },
        new TestData { index = 2, type = TEST_SAMPLE_TYPE.SECOND },
        new TestData { index = 3, type = TEST_SAMPLE_TYPE.FIRST },
    };

    public record TestData {
        
        public uint index { get; set; }
        public TEST_SAMPLE_TYPE type { get; set; }
    }

    public enum TEST_SAMPLE_TYPE {
        NONE,
        FIRST,
        SECOND,
    }
}

#endregion