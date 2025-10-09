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
using ByteSync.Services.Inventories;

namespace ByteSync.Factories;

public class InventoryBuilderFactory : IInventoryBuilderFactory
{
    private readonly IComponentContext _context;
    
    public InventoryBuilderFactory(IComponentContext context)
    {
        _context = context;
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
        
        var saver = new InventorySaver();
        var analyzer = new InventoryFileAnalyzer(saver, FingerprintModes.Rsync, inventoryService.InventoryProcessData);
        var indexer = new InventoryIndexer();
        
        var inventoryBuilder = _context.Resolve<IInventoryBuilder>(
            new TypedParameter(typeof(SessionMember), sessionMember),
            new TypedParameter(typeof(DataNode), dataNode),
            new TypedParameter(typeof(SessionSettings), cloudSessionSettings),
            new TypedParameter(typeof(InventoryProcessData), inventoryService.InventoryProcessData),
            new TypedParameter(typeof(OSPlatforms), environmentService.OSPlatform),
            new TypedParameter(typeof(FingerprintModes), FingerprintModes.Rsync),
            new TypedParameter(typeof(IInventoryFileAnalyzer), analyzer),
            new TypedParameter(typeof(IInventorySaver), saver),
            new TypedParameter(typeof(InventoryIndexer), indexer));
        
        foreach (var dataSource in myDataSources)
        {
            inventoryBuilder.AddInventoryPart(dataSource);
        }
        
        return inventoryBuilder;
    }
}