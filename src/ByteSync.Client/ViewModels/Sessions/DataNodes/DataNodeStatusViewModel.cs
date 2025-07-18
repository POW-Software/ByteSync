using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Repositories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.DataNodes;

public class DataNodeStatusViewModel : ActivatableViewModelBase
{
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly SessionMember _sessionMember;

    public DataNodeStatusViewModel(SessionMember sessionMember,
        bool isLocalMachine,
        ISessionMemberRepository sessionMemberRepository,
        ILocalizationService localizationService)
    {
        _sessionMember = sessionMember;
        IsLocalMachine = isLocalMachine;
        _sessionMemberRepository = sessionMemberRepository;

        UpdateStatus(sessionMember.SessionMemberGeneralStatus);

        this.WhenActivated(disposables =>
        {
            _sessionMemberRepository.Watch(sessionMember)
                .Subscribe(item => UpdateStatus(item.Current.SessionMemberGeneralStatus))
                .DisposeWith(disposables);

            // react to locale changes to translate label
            Observable.FromEventPattern<PropertyChangedEventArgs>(localizationService, nameof(localizationService.PropertyChanged))
                .Subscribe(_ => UpdateStatus(_sessionMember.SessionMemberGeneralStatus))
                .DisposeWith(disposables);
        });
    }

    [Reactive]
    public string Status { get; private set; } = string.Empty;

    [Reactive]
    public bool IsLocalMachine { get; private set; }

    private void UpdateStatus(SessionMemberGeneralStatus status)
    {
        Status = status switch
        {
            SessionMemberGeneralStatus.InventoryWaitingForStart => Resources.SessionMachine_Status_WaitingForStart,
            SessionMemberGeneralStatus.InventoryRunningIdentification => Resources.SessionMachine_Status_RunningIdentification,
            SessionMemberGeneralStatus.InventoryWaitingForAnalysis => Resources.SessionMachine_Status_WaitingForAnalysis,
            SessionMemberGeneralStatus.InventoryRunningAnalysis => Resources.SessionMachine_Status_RunningAnalysis,
            SessionMemberGeneralStatus.InventoryCancelled => Resources.SessionMachine_Status_InventoryCancelled,
            SessionMemberGeneralStatus.InventoryError => Resources.SessionMachine_Status_InventoryError,
            SessionMemberGeneralStatus.InventoryFinished => Resources.SessionMachine_Status_Finished,
            SessionMemberGeneralStatus.SynchronizationRunning => Resources.SessionMachine_Status_SynchronizationRunning,
            SessionMemberGeneralStatus.SynchronizationSourceActionsInitiated => Resources.SessionMachine_Status_SynchronizationSourceActionsInitiated,
            SessionMemberGeneralStatus.SynchronizationError => Resources.SessionMachine_Status_SynchronizationError,
            SessionMemberGeneralStatus.SynchronizationFinished => Resources.SessionMachine_Status_SynchronizationFinished,
            _ => Resources.SessionMachine_Status_UnknownStatus,
        };
    }
} 