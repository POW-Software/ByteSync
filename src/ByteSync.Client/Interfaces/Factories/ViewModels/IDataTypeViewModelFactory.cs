using ByteSync.Business.Sessions;
using ByteSync.ViewModels.Sessions.Cloud.Managing;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IDataTypeViewModelFactory
{   
    DataTypeViewModel CreateDataTypeViewModel(DataTypes dataType);
}