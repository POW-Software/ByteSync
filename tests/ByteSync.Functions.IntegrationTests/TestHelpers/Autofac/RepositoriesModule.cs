namespace ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;

public class RepositoriesModule : BaseElementTypeModule
{
    public RepositoriesModule(bool useConcrete) : base(useConcrete)
    {
        
    }

    protected override string ElementsType => "Repository";
}