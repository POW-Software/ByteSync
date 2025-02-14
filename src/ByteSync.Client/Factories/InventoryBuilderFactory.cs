using System.Threading;
using Autofac;
using ByteSync.Business;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Business.Inventories;
using ByteSync.Business.PathItems;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Misc;
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
    
    public IInventoryBuilder CreateInventoryBuilder()
    {
        var sessionMemberRepository = _context.Resolve<ISessionMemberRepository>();
        var sessionService = _context.Resolve<ISessionService>();
        var inventoryService = _context.Resolve<IInventoryService>();
        var environmentService = _context.Resolve<IEnvironmentService>();
        var pathItemRepository = _context.Resolve<IPathItemRepository>();
        
        var sessionMember = sessionMemberRepository.GetCurrentSessionMember();
        var cloudSessionSettings = sessionService.CurrentSessionSettings!;
        var myPathItems = pathItemRepository.CurrentMemberPathItems.Items.ToList();
        
        var inventoryBuilder = _context.Resolve<IInventoryBuilder>(
            new TypedParameter(typeof(SessionMemberInfo), sessionMember),
            new TypedParameter(typeof(SessionSettings), cloudSessionSettings),
            new TypedParameter(typeof(InventoryProcessData), inventoryService.InventoryProcessData),
            new TypedParameter(typeof(OSPlatforms), environmentService.OSPlatform),
            new TypedParameter(typeof(FingerprintModes), FingerprintModes.Rsync));

        foreach (var pathItem in myPathItems)
        {
            inventoryBuilder.AddInventoryPart(pathItem);
        }
        
        return inventoryBuilder;
    }
}