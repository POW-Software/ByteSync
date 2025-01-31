using System.Threading.Tasks;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using Serilog;
using ConnectionStatuses = ByteSync.Business.Sessions.ConnectionStatuses;

namespace ByteSync.Services.Sessions.Connecting;

public class YouJoinedSessionService : IYouJoinedSessionService
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly IPublicKeysTruster _publicKeysTruster;
    private readonly IDigitalSignaturesChecker _digitalSignaturesChecker;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly IAfterJoinSessionService _afterJoinSessionService;
    private readonly ILogger<YouJoinedSessionService> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";
    private const string PUBLIC_KEY_IS_NOT_TRUSTED = "Public key is not trusted";

    public YouJoinedSessionService(ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        IEnvironmentService environmentService, IPublicKeysTruster publicKeysTruster, IDigitalSignaturesChecker digitalSignaturesChecker,
        IDataEncrypter dataEncrypter, ICloudSessionApiClient cloudSessionApiClient, IPublicKeysManager publicKeysManager, ISessionService sessionService,
        ICloudSessionConnector cloudSessionConnector, IAfterJoinSessionService afterJoinSessionService, ILogger<YouJoinedSessionService> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _environmentService = environmentService;
        _publicKeysTruster = publicKeysTruster;
        _digitalSignaturesChecker = digitalSignaturesChecker;
        _dataEncrypter = dataEncrypter;
        _cloudSessionApiClient = cloudSessionApiClient;
        _publicKeysManager = publicKeysManager;
        _sessionService = sessionService;
        _cloudSessionConnector = cloudSessionConnector;
        _afterJoinSessionService = afterJoinSessionService;
        _logger = logger;
    }
    
    public async Task Process(CloudSessionResult cloudSessionResult, ValidateJoinCloudSessionParameters parameters)
    {
        try
        {
            if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(cloudSessionResult.CloudSession.SessionId))
            {
                _logger.LogError(UNKNOWN_RECEIVED_SESSION_ID, cloudSessionResult.CloudSession.SessionId);
                return;
            }

            if (!_environmentService.ClientInstanceId.Equals(parameters.JoinerClientInstanceId))
            {
                _logger.LogWarning("unexpected session event received with JoinerId {joinerId}", parameters.JoinerClientInstanceId);
                return;
            }

            if (_cloudSessionConnectionRepository.CurrentConnectionStatus != ConnectionStatuses.JoiningSession)
            {
                _logger.LogWarning("no longer trying to join session");
                return;
            }
            
            var isAuthOK = false;
            var cpt = 0;
            while (! isAuthOK)
            {
                cpt += 1;
                if (cpt == 5)
                {
                    _logger.LogWarning($"can not check auth. Too many tries");
                    return;
                }
                
                var sessionMembersClientInstanceIds = await _publicKeysTruster.TrustMissingMembersPublicKeys(cloudSessionResult.CloudSession.SessionId);
                if (sessionMembersClientInstanceIds == null)
                {
                    _logger.LogWarning($"can not check trust");
                    return;
                }
                
                isAuthOK = await _digitalSignaturesChecker.CheckExistingMembersDigitalSignatures(cloudSessionResult.CloudSession.SessionId, 
                    sessionMembersClientInstanceIds);
                if (!isAuthOK)
                {
                    _logger.LogWarning($"can not check auth");
                    return;
                }

                var sessionMemberPrivateData = new SessionMemberPrivateData
                {
                    MachineName = _environmentService.MachineName
                };
                
                var aesEncryptionKey = _publicKeysManager.DecryptBytes(parameters.EncryptedAesKey);
                _cloudSessionConnectionRepository.SetAesEncryptionKey(aesEncryptionKey);
                
                var encryptedSessionMemberPrivateData = _dataEncrypter.EncryptSessionMemberPrivateData(sessionMemberPrivateData);
                var finalizeParameters = new FinalizeJoinCloudSessionParameters(parameters, encryptedSessionMemberPrivateData);

                var finalizeJoinSessionResult = await _cloudSessionApiClient.FinalizeJoinCloudSession(finalizeParameters);

                if (finalizeJoinSessionResult.Status == FinalizeJoinSessionStatuses.AuthIsNotChecked)
                {
                    isAuthOK = false;
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                else if (!finalizeJoinSessionResult.IsOK)
                {
                    _logger.LogWarning($"error during join session finalization");
                    return;
                }
            }

            try
            {
                var aesEncryptionKey = _publicKeysManager.DecryptBytes(parameters.EncryptedAesKey);
                _cloudSessionConnectionRepository.SetAesEncryptionKey(aesEncryptionKey);

                _logger.LogDebug("...EncryptionKey received successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "...Error during EncryptionKey reception");
                throw;
            }

            var lobbySessionDetails = await _cloudSessionConnectionRepository
                .GetTempLobbySessionDetails(cloudSessionResult.CloudSession.SessionId);
            
            await _afterJoinSessionService.Process(
                new AfterJoinSessionRequest(cloudSessionResult, lobbySessionDetails, false));
            
            
            await _cloudSessionConnectionRepository.SetJoinSessionResultReceived(cloudSessionResult.CloudSession.SessionId);

            // ReSharper disable once PossibleNullReferenceException
            Log.Information("JoinSession: {CloudSession}", cloudSessionResult.SessionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnYouJoinedSession");
            
            _sessionService.ClearCloudSession();
        }
        finally
        {
            _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
        }
    }
}