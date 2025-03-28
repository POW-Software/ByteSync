﻿using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions;

public class CreateSessionService : ICreateSessionService
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IEnvironmentService _environmentService;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly IAfterJoinSessionService _afterJoinSessionService;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly ILogger<CreateSessionService> _logger;

    public CreateSessionService(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        IDataEncrypter dataEncrypter, 
        IEnvironmentService environmentService, 
        ICloudSessionApiClient cloudSessionApiClient, 
        IPublicKeysManager publicKeysManager, 
        ITrustProcessPublicKeysRepository trustProcessPublicKeysRepository, 
        IDigitalSignaturesRepository digitalSignaturesRepository,
        IAfterJoinSessionService afterJoinSessionService,
        ICloudSessionConnectionService cloudSessionConnectionService,
        ILogger<CreateSessionService> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _dataEncrypter = dataEncrypter;
        _environmentService = environmentService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _publicKeysManager = publicKeysManager;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _afterJoinSessionService = afterJoinSessionService;
        _cloudSessionConnectionService = cloudSessionConnectionService;
        _logger = logger;
    }
    
    public async Task<CloudSessionResult?> CreateCloudSession(CreateCloudSessionRequest request)
    {
        try
        {
            await _cloudSessionConnectionService.InitializeConnection(SessionConnectionStatus.CreatingSession);
            
            var createCloudSessionParameters = BuildCreateCloudSessionParameters(request);
            var cloudSessionResult = await _cloudSessionApiClient.CreateCloudSession(createCloudSessionParameters, 
                _cloudSessionConnectionRepository.CancellationToken);

            if (_cloudSessionConnectionRepository.CancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            
            await _trustProcessPublicKeysRepository.Start(cloudSessionResult.SessionId);
            await _digitalSignaturesRepository.Start(cloudSessionResult.SessionId);
    
            await _afterJoinSessionService.Process(new AfterJoinSessionRequest(cloudSessionResult, request.RunCloudSessionProfileInfo, true));
            
            _cloudSessionConnectionRepository.SetConnectionStatus(SessionConnectionStatus.InSession);
            
            _logger.LogInformation("Created Cloud Session {CloudSession}", cloudSessionResult.SessionId);

            return cloudSessionResult;
        }
        catch (Exception ex)
        {
            var status = CreateSessionStatus.Error;
            if (ex is TaskCanceledException || ex.InnerException is TaskCanceledException)
            {
                status = CreateSessionStatus.CanceledByUser;
            }
            
            var createSessionError = new CreateSessionError
            {
                Exception = ex,
                Status = status
            };
            
            await _cloudSessionConnectionService.OnCreateSessionError(createSessionError);
            
            return null;
        }
    }

    public Task CancelCreateCloudSession()
    {
        _logger.LogInformation("User requested to cancel Cloud Session creation");
        _cloudSessionConnectionRepository.CancellationTokenSource.Cancel();
        
        return Task.CompletedTask;
    }

    private CreateCloudSessionParameters BuildCreateCloudSessionParameters(CreateCloudSessionRequest request)
    {
        SessionSettings sessionSettings;
        if (request.RunCloudSessionProfileInfo == null)
        {
            sessionSettings = SessionSettings.BuildDefault();
        }
        else
        {
            sessionSettings = request.RunCloudSessionProfileInfo.ProfileDetails.Options.Settings;
        }
            
        var encryptedSessionSettings = _dataEncrypter.EncryptSessionSettings(sessionSettings);

        var sessionMemberPrivateData = new SessionMemberPrivateData
        {
            MachineName = _environmentService.MachineName
        };
        var encryptedSessionMemberPrivateData = _dataEncrypter.EncryptSessionMemberPrivateData(sessionMemberPrivateData);
            
        var parameters = new CreateCloudSessionParameters
        {
            LobbyId = request.RunCloudSessionProfileInfo?.LobbyId,
            CreatorProfileClientId = request.RunCloudSessionProfileInfo?.LocalProfileClientId,
            SessionSettings = encryptedSessionSettings,
            CreatorPublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo(),
            CreatorPrivateData = encryptedSessionMemberPrivateData
        };
        return parameters;
    }
}