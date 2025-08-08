using Autofac;
using ByteSync.ServerCommon.Services.Storage;

namespace ByteSync.Functions.IntegrationTests.Services;

[TestFixture]
public class AzureBlobStorageService_Tests
{
    private ILifetimeScope _scope;

    [SetUp]
    public void Setup()
    {
        _scope = GlobalTestSetup.Container.BeginLifetimeScope();
    }

    [TearDown]
    public void Teardown()
    {
        _scope.Dispose();
    }

    [Test]
    public void TestWithSettings()
    {
        var AzureBlobStoragService = _scope.Resolve<AzureBlobStorageService>();
        
    }
}