using Autofac;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Cloud.Managing;

namespace ByteSync.Factories.ViewModels;

public class DataTypeViewModelFactory : IDataTypeViewModelFactory
{
    private readonly IComponentContext _context;

    public DataTypeViewModelFactory(IComponentContext context)
    {
        _context = context;
    }

    public DataTypeViewModel CreateDataTypeViewModel(DataTypes dataType)
    {
        var result = _context.Resolve<DataTypeViewModel>(
            new TypedParameter(typeof(DataTypes), dataType));

        return result;
    }
}