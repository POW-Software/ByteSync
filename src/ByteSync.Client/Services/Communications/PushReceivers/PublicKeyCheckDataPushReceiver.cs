using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;

namespace ByteSync.Services.Communications.PushReceivers;

public class PublicKeyCheckDataPushReceiver : IPushReceiver
{
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly IPublicKeysTruster _publicKeysTruster;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly IDigitalSignaturesChecker _digitalSignaturesChecker;
    private readonly ILogger<PublicKeyCheckDataPushReceiver> _logger;

    public PublicKeyCheckDataPushReceiver(IHubPushHandler2 hubPushHandler2, IPublicKeysTruster publicKeysTruster, 
        ITrustProcessPublicKeysRepository trustProcessPublicKeysRepository, IDigitalSignaturesChecker digitalSignaturesChecker, 
        ILogger<PublicKeyCheckDataPushReceiver> logger)
    {
        _hubPushHandler2 = hubPushHandler2;
        _publicKeysTruster = publicKeysTruster;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _digitalSignaturesChecker = digitalSignaturesChecker;
        _logger = logger;

        _hubPushHandler2.AskPublicKeyCheckData.Subscribe(OnPublicKeyCheckDataAsked);
        _hubPushHandler2.GiveMemberPublicKeyCheckData.Subscribe(OnMemberPublicKeyCheckDataGiven);
        _hubPushHandler2.RequestTrustPublicKey.Subscribe(OnTrustPublicKeyRequested);
        _hubPushHandler2.InformPublicKeyValidationIsFinished.Subscribe(OnPublicKeyValidationIsFinished);
        _hubPushHandler2.RequestCheckDigitalSignature.Subscribe(OnRequestCheckDigitalSignature);
        _hubPushHandler2.InformProtocolVersionIncompatible.Subscribe(OnProtocolVersionIncompatible);
    }

    private async void OnPublicKeyCheckDataAsked((string sessionId, string clientInstanceId, PublicKeyInfo publicKeyInfo) tuple)
    {
        try
        {
            await _publicKeysTruster.OnPublicKeyCheckDataAskedAsync(tuple);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPublicKeyCheckDataAsked");
        }
    }
    
    private async void OnMemberPublicKeyCheckDataGiven((string sessionId, PublicKeyCheckData publicKeyCheckData) tuple)
    {
        try
        {
            await _trustProcessPublicKeysRepository.RunAsync(tuple.sessionId, 
                data => data.JoinerTrustProcessData.StoreMemberPublicKeyCheckData(tuple.publicKeyCheckData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnMemberPublicKeyCheckDataGiven");
        }
    }
    
    private async void OnTrustPublicKeyRequested(RequestTrustProcessParameters requestTrustProcessParameters)
    {
        try
        {
            await _publicKeysTruster.OnTrustPublicKeyRequestedAsync(requestTrustProcessParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnTrustPublicKeyRequested");
        }
    }
    
    private async void OnPublicKeyValidationIsFinished(PublicKeyValidationParameters publicKeyValidationParameters)
    {
        try
        {
            await _publicKeysTruster.OnPublicKeyValidationIsFinishedAsync(publicKeyValidationParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPublicKeyValidationIsFinished");
        }
    }
    
    private async void OnRequestCheckDigitalSignature(DigitalSignatureCheckInfo digitalSignatureCheckInfo)
    {
        try
        {
            await _digitalSignaturesChecker.CheckDigitalSignature(digitalSignatureCheckInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnRequestCheckDigitalSignature");
        }
    }
    
    private async void OnProtocolVersionIncompatible(InformProtocolVersionIncompatibleParameters parameters)
    {
        try
        {
            await _trustProcessPublicKeysRepository.SetProtocolVersionIncompatible(parameters.SessionId, parameters.MemberClientInstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnProtocolVersionIncompatible");
        }
    }
}