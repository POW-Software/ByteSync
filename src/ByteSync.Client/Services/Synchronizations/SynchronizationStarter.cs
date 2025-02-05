using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Actions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationStarter : ISynchronizationStarter
{
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationStartFactory _synchronizationStartFactory;
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly IFileUploaderFactory _fileUploaderFactory;
    private readonly IConnectionService _connectionService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly ISynchronizationDataLogger _synchronizationDataLogger;
    private readonly ILogger<SynchronizationStarter> _logger;

    public SynchronizationStarter(ISessionService sessionService, ISynchronizationStartFactory synchronizationStartFactory, 
        ICloudSessionLocalDataManager cloudSessionLocalDataManager, ISynchronizationApiClient synchronizationApiClient, 
        IFileUploaderFactory fileUploaderFactory, IConnectionService connectionService,
        ISynchronizationService synchronizationService, ISharedActionsGroupRepository sharedActionsGroupRepository,
        ISynchronizationDataLogger synchronizationDataLogger, ILogger<SynchronizationStarter> logger)
    {
        _sessionService = sessionService;
        _synchronizationStartFactory = synchronizationStartFactory;
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _synchronizationApiClient = synchronizationApiClient;
        _fileUploaderFactory = fileUploaderFactory;
        _connectionService = connectionService;
        _synchronizationService = synchronizationService;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _synchronizationDataLogger = synchronizationDataLogger;
        _logger = logger;
    }
    
    public async Task StartSynchronization(bool isLaunchedByUser)
    {
        var session = _sessionService.CurrentSession;
        
        if (session != null)
        {
            if (isLaunchedByUser)
            {
                _logger.LogInformation("The current user has requested to start the Data Synchronization");
            }
            else
            {
                _logger.LogInformation("The Data Synchronization has been automatically started");
            }
        }
        else
        {
            _logger.LogInformation("The Data Synchronization has started");
        }

        if (session is CloudSession cloudSession)
        {
            await Task.Run(() => StartCloudSessionSynchronization(cloudSession));
        }
        else if (session is LocalSession)
        {
            await Task.Run(StartLocalSessionSynchronization);
        }
        else
        {
            throw new ApplicationException("Unable to start synchronization");
        }
    }
    
    private async Task StartCloudSessionSynchronization(CloudSession cloudSession)
    {
        var synchronizationData = await _synchronizationStartFactory.PrepareSharedData();
        
        await UploadSynchronizationStartData(cloudSession, synchronizationData);

        var actionsGroupDefinitions = _sharedActionsGroupRepository.GetActionsGroupsDefinitions();

        var synchronizationStartRequest = new SynchronizationStartRequest
        {
            SessionId = cloudSession.SessionId,
            ActionsGroupDefinitions = actionsGroupDefinitions,
        };
        var synchronization = await _synchronizationApiClient.StartSynchronization(synchronizationStartRequest);

        try
        {
            await _synchronizationService.OnSynchronizationStarted(synchronization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during synchronization loop");
        }
    }

    private async Task UploadSynchronizationStartData(CloudSession cloudSession, SharedSynchronizationStartData sharedSynchronizationStartData)
    {
        var localSharedFile = BuildSynchronizationStartDataLocalSharedFile(cloudSession);

        var synchronizationDataSaver = new SynchronizationDataSaver();
        synchronizationDataSaver.Save(localSharedFile.LocalPath, sharedSynchronizationStartData);
        var fileUploader = _fileUploaderFactory.Build(localSharedFile.LocalPath, localSharedFile.SharedFileDefinition);
        await fileUploader.Upload();
        
        await _synchronizationDataLogger.LogSentSynchronizationData(sharedSynchronizationStartData);

        await _synchronizationService.OnSynchronizationDataTransmitted(sharedSynchronizationStartData);
    }

    private LocalSharedFile BuildSynchronizationStartDataLocalSharedFile(CloudSession cloudSession)
    {
        var synchronizationDataPath = _cloudSessionLocalDataManager.GetSynchronizationStartDataPath();

        var sharedFileDefinition = new SharedFileDefinition();
        sharedFileDefinition.SharedFileType = SharedFileTypes.SynchronizationStartData;
        sharedFileDefinition.ClientInstanceId = _connectionService.ClientInstanceId!;
        sharedFileDefinition.SessionId = cloudSession.SessionId;

        var localSharedFile = new LocalSharedFile(sharedFileDefinition, synchronizationDataPath);

        return localSharedFile;
    }
    
    private Task StartLocalSessionSynchronization()
    {
        return Task.CompletedTask;
    }
}