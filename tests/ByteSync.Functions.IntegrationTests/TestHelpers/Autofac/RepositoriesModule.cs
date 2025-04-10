using ByteSync.ServerCommon.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;

namespace ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;

public class RepositoriesModule : BaseElementTypeModule
{
    public RepositoriesModule(bool useConcrete) : base(useConcrete)
    {
        
    }

    protected override string ElementsType => "Repository";

    protected override IEnumerable<Type> SpecificTypes
    {
        get
        {
            if (UseConcrete)
            {
                return [
                    //typeof(RedisInfrastructureService), 
                    typeof(CacheRepository<>), 
                    //typeof(CacheKeyFactory)
                ];
            }
            else
            {
                return new List<Type>();
            }
        }
    }
}