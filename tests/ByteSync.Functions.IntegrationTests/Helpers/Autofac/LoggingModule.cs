using Autofac;
using Autofac.Core;
using FakeItEasy;
using FakeItEasy.Sdk;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.IntegrationTests.Helpers.Autofac;

public class LoggingModule : Module
{   
    protected override void Load(ContainerBuilder builder)
    {
        Func<IComponentContext, Type[], IEnumerable<Parameter>, object> factory = (context, types, parameters) =>
        {
            var type = typeof(ILogger<>).MakeGenericType(types);
            
            var fakeLogger = Create.Fake(type);
            return fakeLogger;
        };
        
        builder.RegisterGeneric(factory)
            .As(typeof(ILogger<>))
            .SingleInstance();
        
        builder.RegisterInstance(A.Fake<ILoggerFactory>())
            .As<ILoggerFactory>()
            .SingleInstance();
    }
}