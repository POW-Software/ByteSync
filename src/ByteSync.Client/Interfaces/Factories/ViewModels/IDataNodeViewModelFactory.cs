using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.ViewModels.Sessions.DataNodes;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IDataNodeViewModelFactory
{
    public DataNodeViewModel CreateDataNodeViewModel(DataNode dataNode);
}