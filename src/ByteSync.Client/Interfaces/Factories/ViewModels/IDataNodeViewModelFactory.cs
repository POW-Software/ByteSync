using ByteSync.Business.SessionMembers;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IDataNodeViewModelFactory
{
    public DataNodeViewModel CreateDataNodeViewModel(SessionMemberInfo sessionMemberInfo);
}