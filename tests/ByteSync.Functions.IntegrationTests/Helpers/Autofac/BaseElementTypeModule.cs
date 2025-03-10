﻿using Autofac;
using FakeItEasy.Sdk;

namespace ByteSync.Functions.IntegrationTests.Helpers.Autofac;

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
    }
}