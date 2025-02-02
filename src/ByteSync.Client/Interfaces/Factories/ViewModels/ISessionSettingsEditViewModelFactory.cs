using ByteSync.Business.Sessions;
using ByteSync.ViewModels.Sessions.Managing;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface ISessionSettingsEditViewModelFactory
{
    SessionSettingsEditViewModel CreateSessionSettingsEditViewModel(SessionSettings? sessionSettings);
}