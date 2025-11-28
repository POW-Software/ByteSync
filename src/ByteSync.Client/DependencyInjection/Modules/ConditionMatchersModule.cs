using Autofac;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Comparisons.ConditionMatchers;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class ConditionMatchersModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ContentIdentityExtractor>().AsSelf().SingleInstance();
        
        builder.RegisterType<ContentConditionMatcher>().As<IConditionMatcher>();
        builder.RegisterType<SizeConditionMatcher>().As<IConditionMatcher>();
        builder.RegisterType<DateConditionMatcher>().As<IConditionMatcher>();
        builder.RegisterType<PresenceConditionMatcher>().As<IConditionMatcher>();
        builder.RegisterType<NameConditionMatcher>().As<IConditionMatcher>();
        
        builder.RegisterType<ConditionMatcherFactory>().AsSelf().SingleInstance();
    }
}