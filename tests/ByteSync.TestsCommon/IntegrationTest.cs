using Autofac;
using ByteSync.TestsCommon.Mocking;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace ByteSync.TestsCommon;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class IntegrationTest
{
    private IContainer _container = null!;
    protected ContainerBuilder _builder;
    
    protected ITestDirectoryService _testDirectoryService = null!;

    protected IntegrationTest()
    {
        _builder = new ContainerBuilder();

        _builder.RegisterType<TestDirectoryService>().As<ITestDirectoryService>().SingleInstance();
    }
    
    protected IContainer Container => _container;

    protected void BuildMoqContainer()
    {
        _builder.RegisterSource(new MoqRegistrationSource());
        
        _container = _builder.Build();
        
        _testDirectoryService = _container.Resolve<ITestDirectoryService>();
    }
    
    protected void RegisterType<T>() where T : notnull
    {
        _builder.RegisterType<T>().SingleInstance();
    }
    
    protected void RegisterType<TImplementation, TInterface>() where TImplementation : notnull where TInterface : notnull
    {
        _builder.RegisterType<TImplementation>().AsSelf().As<TInterface>().SingleInstance();
    }
    
    [TearDown]
    public void TearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome  == ResultState.Success)
        {
            _testDirectoryService?.Clear();
        }
        
        _container?.Dispose();
    }
}