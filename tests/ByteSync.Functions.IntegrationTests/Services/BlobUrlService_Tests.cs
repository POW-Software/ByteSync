using Autofac;
using ByteSync.ServerCommon.Services;

namespace ByteSync.Functions.IntegrationTests.Services;

[TestFixture]
public class BlobUrlService_Tests
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
        var blobUrlService = _scope.Resolve<BlobUrlService>();
        
    }
}