namespace ByteSync.Functions.IntegrationTests.Helpers.Autofac;

public class RepositoriesModule : BaseElementTypeModule
{
    public RepositoriesModule(bool useConcrete) : base(useConcrete)
    {

    }

    protected override string ElementsType => "Repository";
}