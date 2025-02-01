using Autofac;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Factories.ViewModels;

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