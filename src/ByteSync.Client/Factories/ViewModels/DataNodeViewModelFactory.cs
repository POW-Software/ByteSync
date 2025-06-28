using Autofac;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Factories.ViewModels;

public class DataNodeViewModelFactory : IDataNodeViewModelFactory
{
    private readonly IComponentContext _context;

    public DataNodeViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public DataNodeViewModel CreateDataNodeViewModel(SessionMemberInfo sessionMemberInfo)
    {
        var result = _context.Resolve<DataNodeViewModel>(
            new TypedParameter(typeof(SessionMemberInfo), sessionMemberInfo));

        return result;
    }
}