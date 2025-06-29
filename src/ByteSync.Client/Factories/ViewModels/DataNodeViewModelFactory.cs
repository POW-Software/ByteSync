using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
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
        
        var result = _context.Resolve<DataNodeViewModel>(
            new TypedParameter(typeof(DataNode), dataNode),
            new TypedParameter(typeof(SessionMember), sessionMember));

        return result;
    }
}