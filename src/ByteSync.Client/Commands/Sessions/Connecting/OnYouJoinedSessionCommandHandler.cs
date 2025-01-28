using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Communications;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Sessions;
using MediatR;
using Serilog;
using ConnectionStatuses = ByteSync.Business.Sessions.ConnectionStatuses;

namespace ByteSync.Commands.Sessions.Connecting;

public class OnYouJoinedSessionCommandHandler : IRequestHandler<OnYouJoinedSessionRequest>
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
    private readonly ILogger<OnYouJoinedSessionCommandHandler> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";
    private const string PUBLIC_KEY_IS_NOT_TRUSTED = "Public key is not trusted";

    public OnYouJoinedSessionCommandHandler(ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        IEnvironmentService environmentService, IPublicKeysTruster publicKeysTruster, IDigitalSignaturesChecker digitalSignaturesChecker,
        IDataEncrypter dataEncrypter, ICloudSessionApiClient cloudSessionApiClient, IPublicKeysManager publicKeysManager, ISessionService sessionService,
        ICloudSessionConnector cloudSessionConnector, ILogger<OnYouJoinedSessionCommandHandler> logger)
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
        _logger = logger;
    }
    
    public async Task Handle(OnYouJoinedSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(request.CloudSessionResult.CloudSession.SessionId))
            {
                _logger.LogError(UNKNOWN_RECEIVED_SESSION_ID, request.CloudSessionResult.CloudSession.SessionId);
                return;
            }

            if (!_environmentService.ClientInstanceId.Equals(request.Parameters.JoinerClientInstanceId))
            {
                _logger.LogWarning("unexpected session event received with JoinerId {joinerId}", request.Parameters.JoinerClientInstanceId);
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
                
                var sessionMembersClientInstanceIds = await _publicKeysTruster.TrustMissingMembersPublicKeys(request.CloudSessionResult.CloudSession.SessionId);
                if (sessionMembersClientInstanceIds == null)
                {
                    _logger.LogWarning($"can not check trust");
                    return;
                }
                
                isAuthOK = await _digitalSignaturesChecker.CheckExistingMembersDigitalSignatures(request.CloudSessionResult.CloudSession.SessionId, 
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
                
                var aesEncryptionKey = _publicKeysManager.DecryptBytes(request.Parameters.EncryptedAesKey);
                _cloudSessionConnectionRepository.SetAesEncryptionKey(aesEncryptionKey);
                
                var encryptedSessionMemberPrivateData = _dataEncrypter.EncryptSessionMemberPrivateData(sessionMemberPrivateData);
                var finalizeParameters = new FinalizeJoinCloudSessionParameters(request.Parameters, encryptedSessionMemberPrivateData);

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
                var aesEncryptionKey = _publicKeysManager.DecryptBytes(request.Parameters.EncryptedAesKey);
                _cloudSessionConnectionRepository.SetAesEncryptionKey(aesEncryptionKey);

                _logger.LogDebug("...EncryptionKey received successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "...Error during EncryptionKey reception");
                throw;
            }

            var lobbySessionDetails = await _cloudSessionConnectionRepository
                .GetTempLobbySessionDetails(request.CloudSessionResult.CloudSession.SessionId);
            
            await AfterSessionCreatedOrJoined(request.CloudSessionResult, lobbySessionDetails, false);
            
            await _cloudSessionConnectionRepository.SetJoinSessionResultReceived(request.CloudSessionResult.CloudSession.SessionId);

            // ReSharper disable once PossibleNullReferenceException
            Log.Information("JoinSession: {CloudSession}", request.CloudSessionResult.SessionId);
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