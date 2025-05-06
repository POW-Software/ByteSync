using System.Reflection;
using Autofac;
using ByteSync.Business.Filtering.Evaluators;
using ByteSync.Business.Filtering.Parsing;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class ExpressionFilterModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FilterParser>().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<FilterTokenizer>().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<OperatorParser>().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<PropertyValueExtractor>().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<PropertyComparer>().AsSelf().AsImplementedInterfaces();
        
        var executingAssembly = Assembly.GetExecutingAssembly();
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && t.GetInterfaces().Any()
                        && t.Namespace != null
                        && t.Namespace.StartsWith("ByteSync.Business.Filtering.Evaluators")
                        && t.Name.EndsWith("Evaluator"))
            .AsSelf().AsImplementedInterfaces();
    }
}