using Autofac;
using FakeItEasy.Sdk;

namespace ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;

public abstract class BaseElementTypeModule : Module
{
    protected bool UseConcrete { get; }

    protected BaseElementTypeModule(bool useConcrete)
    {
        UseConcrete = useConcrete;
    }
    
    protected abstract string ElementsType { get; }
    
    protected override void Load(ContainerBuilder builder)
    {
        if (UseConcrete)
        {
            builder.RegisterAssemblyTypes(GlobalTestSetup.ByteSyncServerCommonAssembly)
                .Where(t => t.Name.EndsWith(ElementsType))
                .AsImplementedInterfaces()
                .AsSelf()
                .InstancePerLifetimeScope();
            
            var genericRepositoryTypes_ = GlobalTestSetup.ByteSyncServerCommonAssembly.GetTypes()
                .Where(t => t.Name.EndsWith(ElementsType) && t.IsGenericTypeDefinition);
            
            var genericRepositoryTypes = GlobalTestSetup.ByteSyncServerCommonAssembly.GetTypes()
                .Where(t => t.Name.Contains(ElementsType + "`") && t.IsGenericTypeDefinition && !t.IsInterface);
            
            foreach (var genericType in genericRepositoryTypes)
            {
                builder.RegisterGeneric(genericType)
                    .AsImplementedInterfaces()
                    .AsSelf()
                    .InstancePerLifetimeScope();
            }
        }
        else
        {
            var repositoryInterfaces = GlobalTestSetup.ByteSyncServerCommonAssembly.GetTypes()
                .Where(t => t.IsInterface && t.Name.EndsWith(ElementsType));
            
            foreach (Type repositoryInterface in repositoryInterfaces)
            {
                builder.Register(_ => Create.Fake(repositoryInterface))
                    .As(repositoryInterface)
                    .InstancePerLifetimeScope();
            }
        }

        /*
        foreach (var type in SpecificTypes)
        {
            // builder.Register(_ => Create.Fake(type))
            //     .As(type)
            //     .AsImplementedInterfaces()
            //     .InstancePerLifetimeScope();
            
            // builder.RegisterType(type)
            //     .AsImplementedInterfaces()
            //     .InstancePerLifetimeScope();
            // builder.Register(type)
            //     .As(type)
            //     .AsImplementedInterfaces()
            //     .InstancePerLifetimeScope();
            
            
            if (type.IsGenericTypeDefinition)
            {
                // Utiliser RegisterGeneric pour les types génériques ouverts
                builder.RegisterGeneric(type)
                    .AsImplementedInterfaces()
                    .InstancePerLifetimeScope();
            }
            else
            {
                // Garder RegisterType pour les types concrets
                builder.RegisterType(type)
                    .AsImplementedInterfaces()
                    .InstancePerLifetimeScope();
            }
        }*/
    }

    protected virtual IEnumerable<Type> SpecificTypes
    {
        get
        {
            return new List<Type>();
        }
    }
}