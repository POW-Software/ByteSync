using ByteSync.Business.DataNodes;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class DataNodeRepository : BaseSourceCacheRepository<DataNode, string>, IDataNodeRepository
{
    private readonly ISessionInvalidationCachePolicy<DataNode, string> _sessionInvalidationCachePolicy;

    public DataNodeRepository(IEnvironmentService environmentService,
        ISessionInvalidationCachePolicy<DataNode, string> sessionInvalidationCachePolicy)
    {
        CurrentMemberDataNodes = SourceCache
            .Connect()
            .Filter(node => Equals(node.ClientInstanceId, environmentService.ClientInstanceId))
            .AsObservableCache();

        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, false);
    }

    protected override string KeySelector(DataNode dataNode) => dataNode.NodeId;

    public IObservableCache<DataNode, string> CurrentMemberDataNodes { get; }

    public IList<DataNode> SortedCurrentMemberDataNodes
    {
        get
        {
            return CurrentMemberDataNodes.Items.OrderBy(n => n.NodeId).ToList();
        }
    }
}
