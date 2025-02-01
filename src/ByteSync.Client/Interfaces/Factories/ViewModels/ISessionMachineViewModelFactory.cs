using ByteSync.Business.SessionMembers;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface ISessionMachineViewModelFactory
{
    public SessionMachineViewModel CreateSessionMachineViewModel(SessionMemberInfo sessionMemberInfo);
}