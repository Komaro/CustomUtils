using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category(TestConstants.Category.SERVICE)]
public class SchedulerServiceTestRunner {

    [OneTimeSetUp]
    public void OneTimeSetUp() => LogAssert.ignoreFailingMessages = true;

    [OneTimeTearDown]
    public void OneTimeTearDown() => LogAssert.ignoreFailingMessages = false;

    [UnityTest]
    public IEnumerator SchedulerServiceTest() {
        if (Service.TryGetService<SchedulerService>(out var service)) {
            for (var i = 0; i < 10; i++) {
                var delay = RandomUtil.GetRandom(0f, 3f);
                service.AttachTask(delay, () => Logger.TraceLog($"{nameof(delay)} = {delay}"));
            }
        }

        yield return new WaitForSeconds(5f);
    }
}
