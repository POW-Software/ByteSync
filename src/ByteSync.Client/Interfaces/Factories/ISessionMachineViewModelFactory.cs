using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ViewModels.Sessions.Cloud.Members;

namespace ByteSync.Interfaces.Factories;

public interface ISessionMachineViewModelFactory
{
    public SessionMachineViewModel CreateSessionMachineViewModel(SessionMemberInfo sessionMemberInfo);
}