using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Business.Arguments;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationLooper : ISynchronizationLooper
{
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly ISynchronizationActionHandler _synchronizationActionHandler;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<SynchronizationLooper> _logger;
    
    private readonly CompositeDisposable _disposables = new();

    public SynchronizationLooper(ISessionService sessionService, ISessionMemberService sessionMemberService, 
        ISynchronizationActionHandler synchronizationActionHandler, 
        ISynchronizationApiClient synchronizationApiClient, ISharedActionsGroupRepository sharedActionsGroupRepository,
        ISynchronizationService synchronizationService, ILogger<SynchronizationLooper> logger)
    {
        _sessionService = sessionService;
        _sessionMemberService = sessionMemberService;
        _synchronizationActionHandler = synchronizationActionHandler;
        _synchronizationApiClient = synchronizationApiClient;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _synchronizationService = synchronizationService;
        _logger = logger;

        Session = null;
        
        var subscription = _sessionService.SessionObservable
            .Subscribe(value => Session = value); 
        _disposables.Add(subscription);

        subscription = _synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest.DistinctUntilChanged()
            .Subscribe(synchronizationAbortRequest => IsSynchronizationAbortRequested = synchronizationAbortRequest != null);
        _disposables.Add(subscription);
    }

    private AbstractSession? Session { get; set; }

    private CloudSession? CloudSession
    {
        get
        {
            return Session as CloudSession;
        }
    }
    
    public bool IsSynchronizationAbortRequested { get; private set; }
    
    public async Task CloudSessionSynchronizationLoop()
    {
        var cancellationToken = _synchronizationService.SynchronizationProcessData.CancellationTokenSource.Token;
        var preparedSharedActionsGroups = _sharedActionsGroupRepository.OrganizedSharedActionsGroups;
        
        foreach (var sharedActionsGroup in preparedSharedActionsGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (IsSynchronizationAbortRequested)
            {
                break;
            }
            
        #if DEBUG
            if (DebugArguments.ForceSlow)
            {
                await DebugUtils.DebugTaskDelay(3);
            }
        #endif

            try
            {
            #if DEBUG
                if (DebugArguments.ForceSlow)
                {
                    await DebugUtils.DebugTaskDelay(3);
                }
            #endif
                
                await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Synchronization cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Synchronization exception");
            }
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _synchronizationActionHandler.RunPendingSynchronizationActions(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Pending synchronization actions cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SynchronizationManager.StartLocalSessionSynchronization");
        }
        
        try
        {
            await _synchronizationApiClient.InformCurrentMemberHasFinishedSynchronization(CloudSession!);
            
            _ = _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.SynchronizationSourceActionsInitiated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing server");
        }
    }

    public ValueTask DisposeAsync()
    {
        _disposables.Dispose();
        
        return ValueTask.CompletedTask;
    }
}