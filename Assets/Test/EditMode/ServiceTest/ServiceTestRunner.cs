using System.Threading.Tasks;
using NUnit.Framework;

[Category(TestConstants.Category.SERVICE)]
public class ServiceTestRunner {

    [OneTimeSetUp]
    public void OneTimeSetUp() {
        Service.RemoveService<ResourceService>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() {
        
    }

    [Test]
    public async Task ServiceAsyncTest() {
        var service = await Service.GetServiceAsync<StopWatchService>();
        service.Start();
        await Service.StartServiceAsync<ResourceService>();
        await Service.GetServiceAsync<ResourceService>();
        await Service.RestartServiceAsync<ResourceService>();
        await Service.RefreshServiceAsync<ResourceService>();
        await Service.StopServiceAsync<ResourceService>();
        await Service.RemoveServiceAsync<ResourceService>();
        service.Stop();
    }

    [Test]
    public void ServiceAsyncTaskTest() {
        async Task TestRun() => await Service.StartServiceAsync<ResourceService>();
        _ = Task.Run(async () => await TestRun());
    }
}
