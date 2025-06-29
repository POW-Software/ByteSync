using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Services.Inventories;

public class DataSourceCodeGenerator : IDataSourceCodeGenerator, IDisposable
{
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly IDisposable _dataSourceSub;
    private readonly IDisposable _dataNodeSub;

    public DataSourceCodeGenerator(IDataSourceRepository dataSourceRepository, IDataNodeRepository dataNodeRepository)
    {
        _dataSourceRepository = dataSourceRepository;
        _dataNodeRepository = dataNodeRepository;

        _dataSourceSub = _dataSourceRepository.ObservableCache.Connect()
            .WhereReasonsAre(ChangeReason.Add, ChangeReason.Remove)
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    RecomputeCodesForNode(change.Current.DataNodeId);
                }
            });

        _dataNodeSub = _dataNodeRepository.ObservableCache.Connect()
            .WhereReasonsAre(ChangeReason.Update)
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    RecomputeCodesForNode(change.Current.NodeId);
                }
            });
    }

    public void RecomputeCodesForNode(string dataNodeId)
    {
        var node = _dataNodeRepository.GetElement(dataNodeId);
        if (node == null)
        {
            return;
        }

        var sources = _dataSourceRepository.Elements
            .Where(ds => ds.DataNodeId == dataNodeId)
            .OrderBy(ds => ds.Code)
            .ToList();

        var updates = new List<DataSource>();
        for (int i = 0; i < sources.Count; i++)
        {
            var newCode = $"{node.Code}{i + 1}";
            if (sources[i].Code != newCode)
            {
                sources[i].Code = newCode;
                updates.Add(sources[i]);
            }
        }

        if (updates.Count > 0)
        {
            _dataSourceRepository.AddOrUpdate(updates);
        }
    }

    public void Dispose()
    {
        _dataSourceSub.Dispose();
        _dataNodeSub.Dispose();
    }
}
