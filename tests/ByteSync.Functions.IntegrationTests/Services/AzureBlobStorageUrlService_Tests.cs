using Autofac;
using ByteSync.ServerCommon.Services.Storage;

namespace ByteSync.Functions.IntegrationTests.Services;

[TestFixture]
public class AzureBlobStorageUrlService_Tests
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
        var AzureBlobStorageUrlService = _scope.Resolve<AzureBlobStorageUrlService>();
        
    }
}