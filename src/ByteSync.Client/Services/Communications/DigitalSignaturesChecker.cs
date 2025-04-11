using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications;

public class DigitalSignaturesChecker : IDigitalSignaturesChecker
{
    private readonly IEnvironmentService _environmentService;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly ITrustApiClient _trustApiClient;
    private readonly IDigitalSignatureComputer _digitalSignatureComputer;
    private readonly ILogger<DigitalSignaturesChecker> _logger;

    public DigitalSignaturesChecker(IEnvironmentService environmentService, IPublicKeysManager publicKeysManager, 
        IDigitalSignaturesRepository digitalSignaturesRepository, ITrustApiClient trustApiClient, IDigitalSignatureComputer digitalSignatureComputer,
        ILogger<DigitalSignaturesChecker> logger)
    {
        _environmentService = environmentService;
        _publicKeysManager = publicKeysManager;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _trustApiClient = trustApiClient;
        _digitalSignatureComputer = digitalSignatureComputer;
        _logger = logger;
    }
    
    public async Task<bool> CheckExistingMembersDigitalSignatures(string dataId, ICollection<string> clientInstanceIds)
    {
        var signatureCheckInfos = new List<DigitalSignatureCheckInfo>();
        
        clientInstanceIds.Remove(_environmentService.ClientInstanceId);
        foreach (var memberInstanceId in clientInstanceIds)
        {
            var digitalSignatureCheckInfo = _digitalSignatureComputer
                .BuildDigitalSignatureCheckInfo(dataId, memberInstanceId, true);

            signatureCheckInfos.Add(digitalSignatureCheckInfo);
        }

        var parameters = new SendDigitalSignaturesParameters
        {
            DataId = dataId,
            DigitalSignatureCheckInfos = signatureCheckInfos,
            IsAuthCheckOK = false
        };
        await _trustApiClient.SendDigitalSignatures(parameters);
        
        await _digitalSignaturesRepository.SetExpectedDigitalSignaturesToCheck(dataId, signatureCheckInfos);

        await _digitalSignaturesRepository.WaitOrThrowAsync(dataId, data => data.ExpectedDigitalSignaturesCheckedEvent, TimeSpan.FromSeconds(30),
            "Digital Signatures check failed");

        return true;
    }
    
    public async Task CheckDigitalSignature(DigitalSignatureCheckInfo digitalSignatureCheckInfo)
    {
        var otherPartyPublicKeyInfo = digitalSignatureCheckInfo.PublicKeyInfo;
        if (!_publicKeysManager.IsTrusted(otherPartyPublicKeyInfo))
        {
            throw new Exception("Public key is not trusted");
        }
        
        var expectedSignature = _digitalSignatureComputer.ComputeOtherPartyExpectedSignature(digitalSignatureCheckInfo.DataId, 
            digitalSignatureCheckInfo.Issuer, otherPartyPublicKeyInfo);

        var isDataOK = _publicKeysManager.VerifyData(otherPartyPublicKeyInfo, digitalSignatureCheckInfo.Signature, 
            expectedSignature);

        if (isDataOK)
        {
            _logger.LogInformation("Digital Signature successfully checked for Client {ClientInstanceId} with Public Key {@PublicKeyInfo}", 
                digitalSignatureCheckInfo.Issuer, otherPartyPublicKeyInfo);
            
            await _digitalSignaturesRepository.SetDigitalSignatureChecked(digitalSignatureCheckInfo.DataId, digitalSignatureCheckInfo.Issuer);
            
            if (digitalSignatureCheckInfo.NeedsCrossCheck)
            {
                var myDigitalSignatureCheckInfo = _digitalSignatureComputer
                    .BuildDigitalSignatureCheckInfo(digitalSignatureCheckInfo.DataId, digitalSignatureCheckInfo.Issuer, 
                        false);
                
                var parameters = new SendDigitalSignaturesParameters
                {
                    DataId = digitalSignatureCheckInfo.DataId,
                    DigitalSignatureCheckInfos = new List<DigitalSignatureCheckInfo> {myDigitalSignatureCheckInfo},
                    IsAuthCheckOK = true
                };
                
                await _trustApiClient.SendDigitalSignatures(parameters);
            }
            else
            {
                var parameters = new SetAuthCheckedParameters
                {
                    SessionId = digitalSignatureCheckInfo.DataId,
                    CheckedClientInstanceId = digitalSignatureCheckInfo.Issuer
                };
                
                await _trustApiClient.SetAuthChecked(parameters);
            }
        }
        else
        {
            _logger.LogWarning("Digital Signature check failed for Client {ClientInstanceId}", digitalSignatureCheckInfo.Issuer);
        }
    }
}