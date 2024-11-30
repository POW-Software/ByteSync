using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Repositories;
using ByteSync.ViewModels.Misc;

namespace ByteSync.Services.Sessions;

public class SessionInterruptor : ISessionInterruptor
{
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISessionResetter _sessionResetter;
    private readonly ISessionMemberRepository _sessionMemberRepository;


    public SessionInterruptor(ICloudSessionConnector cloudSessionConnector, ISessionService sessionService, 
        IDialogService dialogService, ISynchronizationService synchronizationService,
        ISessionMemberRepository sessionMemberRepository, ISessionResetter sessionResetter)
    {
        _cloudSessionConnector = cloudSessionConnector;
        _sessionService = sessionService;
        _dialogService = dialogService;
        _synchronizationService = synchronizationService;
        _sessionMemberRepository = sessionMemberRepository;
        _sessionResetter = sessionResetter;
    }

    public async Task RequestQuitSession()
    {
        var canQuit = await CanQuitOrRestart(false);

        if (!canQuit)
        {
            var messageBoxViewModel = _dialogService
                .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_CanNotQuit_Title), nameof(Resources.SessionQuitChecker_CanNotQuit_Message));
            messageBoxViewModel.ShowOK = true;
            await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

            return;
        }

        var canQuitWithoutMessage = await _synchronizationService.SynchronizationProcessData.IsSynchronizationEnd() ||
                                    _sessionService.CurrentSessionStatus == SessionStatus.FatalError;

        if (canQuitWithoutMessage)
        {
            await _cloudSessionConnector.QuitSession();
        }
        else
        {
            MessageBoxViewModel messageBoxViewModel;
            
            if (_sessionService.IsSessionActivated)
            {
                if (_sessionService.IsCloudSession)
                {
                    messageBoxViewModel = _dialogService
                        .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_QuitCloudActivated_Title), nameof(Resources.SessionQuitChecker_QuitCloudActivated_Message));
                }
                else
                {
                    messageBoxViewModel = _dialogService
                        .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_QuitLocalActivated_Title), nameof(Resources.SessionQuitChecker_QuitLocalActivated_Message));
                }
            }
            else
            {
                messageBoxViewModel = _dialogService
                    .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_QuitNonActivated_Title), nameof(Resources.SessionQuitChecker_QuitNonActivated_Message));
            }

            messageBoxViewModel.ShowYesNo = true;
            var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

            if (result == MessageBoxResult.Yes)
            {
                await _cloudSessionConnector.QuitSession();
            }
        }
    }

    public async Task RequestRestartSession()
    {
        var canReset = await CanQuitOrRestart(true);

        if (!canReset)
        {
            var messageBoxViewModel = _dialogService
                .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_CanNotRestart_Title), nameof(Resources.SessionQuitChecker_CanNotRestart_Message));
            messageBoxViewModel.ShowOK = true;
            await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

            return;
        }

        var canResetWithoutMessage = await _synchronizationService.SynchronizationProcessData.IsSynchronizationEnd() ||
                                     _sessionService.CurrentSessionStatus == SessionStatus.FatalError;

        if (canResetWithoutMessage)
        {
            await _sessionResetter.ResetSession();
        }
        else
        {
            MessageBoxViewModel messageBoxViewModel;
            
            if (_sessionService.IsSessionActivated)
            {
                if (_sessionService.IsCloudSession)
                {
                    messageBoxViewModel = _dialogService
                        .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_RestartCloudActivated_Title), nameof(Resources.SessionQuitChecker_RestartCloudActivated_Message));
                }
                else
                {
                    messageBoxViewModel = _dialogService
                        .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_RestartLocalActivated_Title), nameof(Resources.SessionQuitChecker_RestartLocalActivated_Message));
                }
            }
            else
            {
                messageBoxViewModel = _dialogService
                    .CreateMessageBoxViewModel(nameof(Resources.SessionQuitChecker_RestartNonActivated_Title), nameof(Resources.SessionQuitChecker_RestartNonActivated_Message));
            }

            messageBoxViewModel.ShowYesNo = true;
            var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);

            if (result == MessageBoxResult.Yes)
            {
                await _sessionResetter.ResetSession();
            }
        }
    }

    private async Task<bool> CanQuitOrRestart(bool checkOtherMembersStatuses)
    {
        var canQuitOrRestart = true;
        var localInventoryStatus = _sessionMemberRepository.GetCurrentSessionMember().SessionMemberGeneralStatus;
        
        switch (localInventoryStatus)
        {
            case SessionMemberGeneralStatus.InventoryRunningIdentification:
            case SessionMemberGeneralStatus.InventoryRunningAnalysis:
            case SessionMemberGeneralStatus.InventoryWaitingForAnalysis:
                canQuitOrRestart = false;
                break;
        }

        if (await _synchronizationService.SynchronizationProcessData.IsSynchronizationRunning())
        {
            canQuitOrRestart = false;
        }

        if (checkOtherMembersStatuses)
        {
            foreach (var sessionMember in _sessionMemberRepository.SortedOtherSessionMembers)
            {
                switch (sessionMember.SessionMemberGeneralStatus)
                {
                    case SessionMemberGeneralStatus.InventoryRunningIdentification:
                    case SessionMemberGeneralStatus.InventoryRunningAnalysis:
                    case SessionMemberGeneralStatus.InventoryWaitingForAnalysis:
                        canQuitOrRestart = false;
                        break;
                }
            }
        }

        return canQuitOrRestart;
    }
}