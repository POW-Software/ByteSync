using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using Serilog;

namespace ByteSync.Services.Sessions.Connecting;

public class CloudSessionPasswordExchangeKeyAskedService : ICloudSessionPasswordExchangeKeyAskedService
{
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly IEnvironmentService _environmentService;
    
    private const string PUBLIC_KEY_IS_NOT_TRUSTED = "Public key is not trusted";

    public CloudSessionPasswordExchangeKeyAskedService(
        ICloudSessionApiClient cloudSessionApiClient,
        IPublicKeysManager publicKeysManager,
        IEnvironmentService environmentService)
    {
        _cloudSessionApiClient = cloudSessionApiClient;
        _publicKeysManager = publicKeysManager;
        _environmentService = environmentService;
    }
    
    public async Task Process(AskCloudSessionPasswordExchangeKeyPush request)
    {
        try
        {
            var isTrusted = _publicKeysManager.IsTrusted(request.PublicKeyInfo);
            if (!isTrusted)
            {
                throw new Exception(PUBLIC_KEY_IS_NOT_TRUSTED);
            }
                
            var parameters = new GiveCloudSessionPasswordExchangeKeyParameters(request.SessionId, 
                request.RequesterInstanceId, _environmentService.ClientInstanceId, 
                _publicKeysManager.GetMyPublicKeyInfo());
            await _cloudSessionApiClient.GiveCloudSessionPasswordExchangeKey(parameters);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnCloudSessionPasswordExchangeKeyAsked");
        }
    }
}