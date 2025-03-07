using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Business.Settings;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IClientSoftwareVersionService
{
    Task<ClientSoftwareVersionSettings?> GetClientSoftwareVersionSettings();
    
    Task<bool> IsClientVersionAllowed(LoginData loginData);
}