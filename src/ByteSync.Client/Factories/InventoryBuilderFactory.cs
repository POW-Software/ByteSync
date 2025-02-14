using System.Threading;
using Autofac;
using ByteSync.Business;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Business.PathItems;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using ByteSync.Services.Sessions;

namespace ByteSync.Factories;

public class InventoryBuilderFactory : IInventoryBuilderFactory
{
    private readonly IComponentContext _context;

    public InventoryBuilderFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public IInventoryBuilder CreateInventoryBuilder(List<PathItem> myPathItems)
    {
        var sessionMemberRepository = _context.Resolve<ISessionMemberRepository>();
        var sessionService = _context.Resolve<ISessionService>();
        var inventoryService = _context.Resolve<IInventoryService>();
        var environmentService = _context.Resolve<IEnvironmentService>();
        var connectionService = _context.Resolve<IConnectionService>();
        
        var sessionMember = sessionMemberRepository.GetCurrentSessionMember();
        var cloudSessionSettings = sessionService.CurrentSessionSettings!;
        
        var inventoryBuilder = new InventoryBuilder(sessionMember.GetLetter(), cloudSessionSettings,
            inventoryService.InventoryProcessData, connectionService.CurrentEndPoint!, sessionMember.MachineName,
            environmentService.OSPlatform, FingerprintModes.Rsync);

        foreach (var pathItem in myPathItems)
        {
            inventoryBuilder.AddInventoryPart(pathItem);
        }
        
        return inventoryBuilder;
    }
}