using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.DataNodes;

namespace ByteSync.Factories.ViewModels;

public class DataNodeViewModelFactory : IDataNodeViewModelFactory
{
    private readonly IComponentContext _context;

    public DataNodeViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public DataNodeViewModel CreateDataNodeViewModel(DataNode dataNode)
    {
        var sessionMemberRepository = _context.Resolve<ISessionMemberRepository>();
        var sessionMember = sessionMemberRepository.GetElement(dataNode.ClientInstanceId)!;
        
        var environmentService = _context.Resolve<IEnvironmentService>();
        bool isLocalMachine = sessionMember.ClientInstanceId.Equals(environmentService.ClientInstanceId);
        
        var dataNodeHeaderViewModel = _context.Resolve<DataNodeHeaderViewModel>(
            new TypedParameter(typeof(SessionMember), sessionMember),
            new TypedParameter(typeof(DataNode), dataNode),
            new TypedParameter(typeof(bool), isLocalMachine));
        
        var dataNodeSourcesViewModel = _context.Resolve<DataNodeSourcesViewModel>(
            new TypedParameter(typeof(DataNode), dataNode),
            new TypedParameter(typeof(bool), isLocalMachine));
        
        var dataNodeStatusViewModel = _context.Resolve<DataNodeStatusViewModel>(
            new TypedParameter(typeof(SessionMember), sessionMember),
            new TypedParameter(typeof(bool), isLocalMachine));
        
        var themeService = _context.Resolve<IThemeService>();
        var dataNodeService = _context.Resolve<IDataNodeService>();
        var dataNodeRepository = _context.Resolve<IDataNodeRepository>();
        var sessionService = _context.Resolve<ISessionService>();
        
        return new DataNodeViewModel(sessionMember, dataNode, isLocalMachine,
            dataNodeSourcesViewModel, dataNodeHeaderViewModel, dataNodeStatusViewModel,
            themeService, dataNodeService, dataNodeRepository, sessionService);
    }
}