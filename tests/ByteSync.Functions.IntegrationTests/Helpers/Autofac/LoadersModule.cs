namespace ByteSync.Functions.IntegrationTests.Helpers.Autofac;

public class LoadersModule : BaseElementTypeModule
{
    public LoadersModule(bool useConcrete) : base(useConcrete)
    {

    }

    protected override string ElementsType => "Loader";
}