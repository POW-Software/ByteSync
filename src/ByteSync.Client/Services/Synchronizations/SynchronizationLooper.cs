using ByteSync.Business.Arguments;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using Serilog;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationLooper : ISynchronizationLooper
{
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly ISynchronizationActionHandler _synchronizationActionHandler;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;

    public SynchronizationLooper(ISessionService sessionService, ISessionMemberService sessionMemberService, 
        ISynchronizationActionHandler synchronizationActionHandler, 
        ISynchronizationApiClient synchronizationApiClient, ISharedActionsGroupRepository sharedActionsGroupRepository)
    {
        _sessionService = sessionService;
        _sessionMemberService = sessionMemberService;
        _synchronizationActionHandler = synchronizationActionHandler;
        _synchronizationApiClient = synchronizationApiClient;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;

        Session = null;
        _sessionService.SessionObservable.Subscribe(value => Session = value); 
    }

    private AbstractSession? Session { get; set; }

    private CloudSession? CloudSession
    {
        get
        {
            return Session as CloudSession;
        }
    }
    
    private LocalSession? LocalSession
    {
        get
        {
            return Session as LocalSession;
        }
    }
    
    public bool IsSynchronizationAbortRequested { get; private set; }
    
    public async Task CloudSessionSynchronizationLoop()
    {
        var preparedSharedActionsGroups = _sharedActionsGroupRepository.OrganizedSharedActionsGroups;
        
        foreach (var sharedActionsGroup in preparedSharedActionsGroups)
        {
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
                
                await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Synchronization exception");
            }
        }

        try
        {
            await _synchronizationActionHandler.RunPendingSynchronizationActions();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SynchronizationManager.StartLocalSessionSynchronization");
        }
        
        try
        {
            await _synchronizationApiClient.InformCurrentMemberHasFinishedSynchronization(CloudSession!);
            
            _ = _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.SynchronizationSourceActionsInitiated);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while informing server");
        }
    }
}