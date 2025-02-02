using Autofac;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Managing;

namespace ByteSync.Factories.ViewModels;

public class SessionSettingsEditViewModelFactory : ISessionSettingsEditViewModelFactory
{
    private readonly IComponentContext _context;

    public SessionSettingsEditViewModelFactory(IComponentContext context)
    {
        _context = context;
    }

    public SessionSettingsEditViewModel CreateSessionSettingsEditViewModel(SessionSettings? sessionSettings)
    {
        var result = _context.Resolve<SessionSettingsEditViewModel>(
            new TypedParameter(typeof(SessionSettings), sessionSettings));

        return result;
    }
}