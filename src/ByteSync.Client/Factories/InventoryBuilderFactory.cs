using Autofac;
using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using Microsoft.Extensions.Logging;

namespace ByteSync.Factories;

public class InventoryBuilderFactory : IInventoryBuilderFactory
{
    private readonly IComponentContext _context;
    private readonly ILogger<InventoryBuilderFactory> _logger;
    
    public InventoryBuilderFactory(IComponentContext context, ILogger<InventoryBuilderFactory> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public IInventoryBuilder CreateInventoryBuilder(DataNode dataNode)
    {
        var sessionMemberRepository = _context.Resolve<ISessionMemberRepository>();
        var sessionService = _context.Resolve<ISessionService>();
        var inventoryService = _context.Resolve<IInventoryService>();
        var environmentService = _context.Resolve<IEnvironmentService>();
        var dataSourceRepository = _context.Resolve<IDataSourceRepository>();
        
        var sessionMember = sessionMemberRepository.GetCurrentSessionMember();
        var cloudSessionSettings = sessionService.CurrentSessionSettings!;
        var myDataSources = dataSourceRepository.SortedCurrentMemberDataSources
            .Where(ds => ds.DataNodeId == dataNode.Id)
            .ToList();
        
        var saver = _context.Resolve<IInventorySaver>();
        var analyzer = _context.Resolve<IInventoryFileAnalyzer>(
            new TypedParameter(typeof(FingerprintModes), FingerprintModes.Rsync),
            new TypedParameter(typeof(InventoryProcessData), inventoryService.InventoryProcessData),
            new TypedParameter(typeof(IInventorySaver), saver));
        
        var inventoryBuilder = _context.Resolve<IInventoryBuilder>(
            new TypedParameter(typeof(SessionMember), sessionMember),
            new TypedParameter(typeof(DataNode), dataNode),
            new TypedParameter(typeof(SessionSettings), cloudSessionSettings),
            new TypedParameter(typeof(InventoryProcessData), inventoryService.InventoryProcessData),
            new TypedParameter(typeof(OSPlatforms), environmentService.OSPlatform),
            new TypedParameter(typeof(FingerprintModes), FingerprintModes.Rsync),
            new TypedParameter(typeof(IInventoryFileAnalyzer), analyzer),
            new TypedParameter(typeof(IInventorySaver), saver));
        
        foreach (var dataSource in myDataSources)
        {
            try
            {
                inventoryBuilder.AddInventoryPart(dataSource);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "InventoryBuilderFactory: Failed to add data source {Path} for DataNode {DataNodeId}",
                    dataSource.Path, dataNode.Id);
            }
        }
        
        return inventoryBuilder;
    }
}
