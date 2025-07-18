using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.ViewModels.Sessions.Members;

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
        
        var dataNodeSourcesViewModel = _context.Resolve<DataNodeSourcesViewModel>(
            new TypedParameter(typeof(DataNode), dataNode),
            new TypedParameter(typeof(bool), isLocalMachine));
        
        var result = _context.Resolve<DataNodeViewModel>(
            new TypedParameter(typeof(DataNode), dataNode),
            new TypedParameter(typeof(SessionMember), sessionMember),
            new TypedParameter(typeof(bool), isLocalMachine),
            new TypedParameter(typeof(DataNodeSourcesViewModel), dataNodeSourcesViewModel));

        return result;
    }
}