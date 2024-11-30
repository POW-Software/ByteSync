using Autofac;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Factories;
using ByteSync.ViewModels.Sessions.Cloud.Members;

namespace ByteSync.Factories;

public class SessionMachineViewModelFactory : ISessionMachineViewModelFactory
{
    private readonly IComponentContext _context;

    public SessionMachineViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public SessionMachineViewModel CreateSessionMachineViewModel(SessionMemberInfo sessionMemberInfo)
    {
        var result = _context.Resolve<SessionMachineViewModel>(
            new TypedParameter(typeof(SessionMemberInfo), sessionMemberInfo));

        return result;
    }
}