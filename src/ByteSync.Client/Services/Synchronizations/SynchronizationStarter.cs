using System.Threading.Tasks;
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
using Serilog;

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

    public SynchronizationStarter(ISessionService sessionService, ISynchronizationStartFactory synchronizationStartFactory, 
        ICloudSessionLocalDataManager cloudSessionLocalDataManager, ISynchronizationApiClient synchronizationApiClient, 
        IFileUploaderFactory fileUploaderFactory, IConnectionService connectionService,
        ISynchronizationService synchronizationService, ISharedActionsGroupRepository sharedActionsGroupRepository,
        ISynchronizationDataLogger synchronizationDataLogger)
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
    }
    
    public async Task StartSynchronization(bool isLaunchedByUser)
    {
        // await _synchronizationLooper.InitializeData();

        var session = _sessionService.CurrentSession;
        
        if (session != null)
        {
            if (isLaunchedByUser)
            {
                Log.Information("The current user has requested to start the Data Synchronization");
            }
            else
            {
                Log.Information("The Data Synchronization has been automatically started");
            }
        }
        else
        {
            Log.Information("The Data Synchronization has started");
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
        // await _synchronizationStartFactory.Clear();
        var synchronizationData = await _synchronizationStartFactory.PrepareSharedData();  
        // BuildSynchronizationStartData(out var sharedActionsGroups, out var synchronizationData);

        // Log.Information("Starting the Data Synchronization");
        
        
        
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
            // await _synchronizationService.SetSynchronizationStartData(synchronizationData);
            // await _sessionDataHolder.SetSynchronizationStarted(synchronizationStart);

            await _synchronizationService.OnSynchronizationStarted(synchronization);

            // var synchronizationProgress = 
            //     MiscBuilder.BuildSynchronizationProgress(cloudSession, actionsGroupDefinitions, synchronization);
            //
            // await _synchronizationLooper.CloudSessionSynchronizationLoop(synchronizationData.SharedActionsGroups, synchronizationProgress);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during synchronization loop");
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
    
    private async Task StartLocalSessionSynchronization()
    {
        /* todo 050523
        try
        {
            IsLocalSynchronizationAbortRequested = false;

            BuildSynchronizationStartData(out var sharedActionsGroups, out var synchronizationData);
            
            // On traite les fichiers en premier, puis les répertoires
            // False en premier == FileSystemTypes.File
            // True en second == FileSystemTypes.Directory
            sharedActionsGroups = sharedActionsGroups.OrderBy(g => g.IsDirectory).ToList();
            
            // Log.Information("Starting the Synchronization");

            var synchronizationActionDefinitions = new List<ActionsGroupDefinition>();
            foreach (var sharedActionsGroup in sharedActionsGroups)
            {
                synchronizationActionDefinitions.Add(sharedActionsGroup.GetDefinition());
            }

            var synchronizationStart = MiscBuilder.BuildSynchronizationStart(LocalSession!, CurrentMachineEndpoint);
            var synchronizationProgress =
                MiscBuilder.BuildSynchronizationProgress(LocalSession!, synchronizationActionDefinitions, synchronizationStart);

            await SetSynchronizationStartData(synchronizationData);
            await _sessionDataHolder.SetSynchronizationStarted(synchronizationStart);

            await LocalSessionSynchronizationLoop(sharedActionsGroups, synchronizationProgress);
            
            SynchronizationProgressInfo synchronizationProgressInfo = MiscBuilder.BuildSynchronizationProgressData(synchronizationProgress, null);
            _sessionDataHolder.OnSynchronizationProgressChanged(synchronizationProgressInfo);

            SynchronizationEnd synchronizationEnd;
            if (IsLocalSynchronizationAbortRequested)
            {
                synchronizationEnd = MiscBuilder.BuildSynchronizationEnd(LocalSession!, SynchronizationEndStatuses.Abortion);
            }
            else
            {
                synchronizationEnd = MiscBuilder.BuildSynchronizationEnd(LocalSession!, SynchronizationEndStatuses.Regular);
            }

            _sessionDataHolder.SetSynchronizationEnded(synchronizationEnd);
        }
        catch (Exception)
        {
            var synchronizationEnd = MiscBuilder.BuildSynchronizationEnd(LocalSession!, SynchronizationEndStatuses.Error);
            _sessionDataHolder.SetSynchronizationEnded(synchronizationEnd);
            
            throw;
        }
        */
    }
    

}