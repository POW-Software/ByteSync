using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using Serilog;

namespace ByteSync.Services.Sessions.Connecting;

public class JoinSessionService : IJoinSessionService
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly IPublicKeysTruster _publicKeysTruster;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ICloudSessionConnector _cloudSessionConnector;


    public JoinSessionService(
        ISessionService sessionService,
        ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        ITrustProcessPublicKeysRepository trustProcessPublicKeysRepository,
        IDigitalSignaturesRepository digitalSignaturesRepository,
        IPublicKeysTruster publicKeysTruster,
        IPublicKeysManager publicKeysManager,
        ICloudSessionApiClient cloudSessionApiClient,
        ICloudSessionEventsHub cloudSessionEventsHub,
        ICloudSessionConnector cloudSessionConnector)
    {
        _sessionService = sessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _publicKeysTruster = publicKeysTruster;
        _publicKeysManager = publicKeysManager;
        _cloudSessionApiClient = cloudSessionApiClient;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _cloudSessionConnector = cloudSessionConnector;
    }
    
    public async Task JoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        try
        {
            await DoStartJoinSession(sessionId, sessionPassword, lobbySessionDetails);
        }
        catch (Exception ex)
        {
            var joinSessionResult = JoinSessionResult.BuildFrom(JoinSessionStatuses.UnexpectedError);
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);

            throw;
        }
    }

    private async Task DoStartJoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        if (sessionId.IsNotEmpty(true) && sessionId.Equals(_sessionService.SessionId))
        {
            return;
        }

        _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.JoiningSession);
        await _cloudSessionConnector.ClearConnectionData();

        await _trustProcessPublicKeysRepository.Start(sessionId);
        await _digitalSignaturesRepository.Start(sessionId);

        Log.Information("Start joining the Cloud Session {sessionId}: getting password exchange encryption key", sessionId);

        await _cloudSessionConnectionRepository.SetCloudSessionConnectionData(sessionId, sessionPassword, lobbySessionDetails);

        JoinSessionResult joinSessionResult;
        // On Fait un processus de Trust pour les clés qui ne sont pas trustées

        joinSessionResult = await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId);
        if (!joinSessionResult.IsOK)
        {
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);
            return;
        }

        // Quand tout est trusté, on peut contrôler les clés
        // Contruction digital signature : sessionId, monClientInstanceId, 
        // Protection: mix clientInstanceId / InstallationId / SessionId en SHA 256

        var parameters = new AskCloudSessionPasswordExchangeKeyParameters(sessionId, _publicKeysManager.GetMyPublicKeyInfo());
        parameters.LobbyId = lobbySessionDetails?.LobbyId;
        parameters.ProfileClientId = lobbySessionDetails?.LocalProfileClientId;
        joinSessionResult = await _cloudSessionApiClient.AskPasswordExchangeKey(parameters);

        if (!joinSessionResult.IsOK)
        {
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);
        }
        else
        {
            await _cloudSessionConnectionRepository.WaitOrThrowAsync(sessionId,
                data => data.WaitForPasswordExchangeKeyEvent, data => data.WaitTimeSpan, "Keys exchange failed: no key received");
        }
    }
    
    // public async Task OnJoinSessionError(JoinSessionResult joinSessionResult)
    // {
    //     _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
    //     await _cloudSessionConnector.ClearConnectionData();
    //         
    //     Log.Error("Can not join the Cloud Session. Reason: {Reason}", joinSessionResult.Status);
    //     await _cloudSessionEventsHub.RaiseJoinCloudSessionFailed(joinSessionResult);
    // }
}