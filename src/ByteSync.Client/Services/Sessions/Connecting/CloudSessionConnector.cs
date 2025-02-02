﻿using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

class CloudSessionConnector : ICloudSessionConnector
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<CloudSessionConnector> _logger;

    public CloudSessionConnector(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        IDigitalSignaturesRepository digitalSignaturesRepository, ITrustProcessPublicKeysRepository trustPublicKeysRepository, 
        ISessionService sessionService, ISynchronizationService synchronizationService, ILogger<CloudSessionConnector> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _trustProcessPublicKeysRepository = trustPublicKeysRepository;
        _sessionService = sessionService;
        _synchronizationService = synchronizationService;
        _logger = logger;
    }
    
    public async Task ClearConnectionData()
    {
        await Task.WhenAll(
            _cloudSessionConnectionRepository.ClearAsync(), 
            _trustProcessPublicKeysRepository.ClearAsync(), 
            _digitalSignaturesRepository.ClearAsync());
    }

    public IObservable<bool> CanLogOutOrShutdown 
    {
        get
        {
            return Observable.CombineLatest(_cloudSessionConnectionRepository.ConnectionStatusObservable,
                _sessionService.SessionObservable, _synchronizationService.SynchronizationProcessData.SynchronizationEnd, 
                _sessionService.SessionStatusObservable,
                (connectionStatus, session, synchronizationEnd, sessionStatus) =>
                    !connectionStatus.In(SessionConnectionStatus.CreatingSession, SessionConnectionStatus.JoiningSession) &&
                    (session == null || synchronizationEnd != null)
                    && sessionStatus.In(SessionStatus.FatalError, SessionStatus.None, SessionStatus.RegularEnd));
        }
    }

    public async Task InitializeConnection(SessionConnectionStatus sessionConnectionStatus)
    {
        await ClearConnectionData();
        _cloudSessionConnectionRepository.SetConnectionStatus(sessionConnectionStatus);
        
        if (sessionConnectionStatus == SessionConnectionStatus.CreatingSession)
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            _cloudSessionConnectionRepository.SetAesEncryptionKey(aes.Key);
        }
        
        if (sessionConnectionStatus.In(SessionConnectionStatus.CreatingSession, SessionConnectionStatus.JoiningSession))
        {
            _cloudSessionConnectionRepository.ClearErrors();
            _cloudSessionConnectionRepository.CancellationTokenSource = new CancellationTokenSource();
        }

        if (sessionConnectionStatus != SessionConnectionStatus.InSession)
        {
            _sessionService.ClearCloudSession();
        }
    }
    
    public async Task OnJoinSessionError(JoinSessionResult joinSessionResult)
    {
        _logger.LogError("Can not join the Cloud Session. Reason: {Reason}", joinSessionResult.Status);
        _cloudSessionConnectionRepository.SetJoinSessionError(joinSessionResult);
        
        await InitializeConnection(SessionConnectionStatus.NoSession);
        
        // _cloudSessionConnectionRepository.SetConnectionStatus(SessionConnectionStatus.NoSession);
        // await ClearConnectionData();
        
        // await _cloudSessionEventsHub.RaiseJoinCloudSessionFailed(joinSessionResult);
    }

    public async Task OnCreateSessionError(CreateSessionError createSessionError)
    {
        _logger.LogError("Can not create a Cloud Session. Reason: {Reason}", createSessionError.Status);
        _cloudSessionConnectionRepository.SetCreateSessionError(createSessionError);

        await InitializeConnection(SessionConnectionStatus.NoSession);
        
        // _cloudSessionConnectionRepository.SetConnectionStatus(SessionConnectionStatus.NoSession);
        // await ClearConnectionData();
    }
}