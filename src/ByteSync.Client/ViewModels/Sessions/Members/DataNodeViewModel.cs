using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members;

public class DataNodeViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IThemeService _themeService;
    private readonly ILogger<DataNodeViewModel> _logger;
    
    
    private IBrush _currentMemberBackGround = null!;
    private IBrush _otherMemberBackGround = null!;
    private IBrush _currentMemberLetterBackGround = null!;
    private IBrush _otherMemberLetterBackGround = null!;
    private IBrush _currentMemberLetterBorder = null!;
    private IBrush _otherMemberLetterBorder = null!;

    public DataNodeSourcesViewModel SourcesViewModel { get; }

    public DataNodeViewModel()
    {

    }

    public DataNodeViewModel(SessionMember sessionMember, DataNode dataNode, bool isLocalMachine, 
        DataNodeSourcesViewModel dataNodeSourcesViewModel, ISessionService sessionService, 
        ILocalizationService localizationService, ISessionMemberRepository sessionMemberRepository, 
        IThemeService themeService, ILogger<DataNodeViewModel> logger)
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
        _sessionMemberRepository = sessionMemberRepository;
        _themeService = themeService;
        _logger = logger;
        
        SourcesViewModel = dataNodeSourcesViewModel;

        SessionMember = sessionMember;
        DataNode = dataNode;

        IsLocalMachine = isLocalMachine;
        JoinedSessionOn = sessionMember.JoinedSessionOn;
        
        this.WhenAnyValue(x => x.IsLocalMachine)
            .Subscribe(_ => UpdateMachineDescription());
        
        this.WhenAnyValue(x => x.HasQuittedSessionAfterActivation)
            .Where(b => b)
            .Subscribe(_ => UpdateMachineDescription());

        ClientInstanceId = sessionMember.ClientInstanceId;
        
        this.WhenActivated(disposables =>
        {
            SourcesViewModel.Activator.Activate()
                .DisposeWith(disposables);

            _sessionService.SessionStatusObservable.CombineLatest(_sessionService.RunSessionProfileInfoObservable)
                .DistinctUntilChanged()
                .Select(tuple => tuple.First == SessionStatus.Preparation && tuple.Second == null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsFileSystemSelectionEnabled)
                .DisposeWith(disposables);
            
            _sessionMemberRepository.Watch(sessionMember)
                .Subscribe(item =>
                {
                    UpdateStatus(item.Current.SessionMemberGeneralStatus);
                    PositionInList = item.Current.PositionInList;
                })
                .DisposeWith(disposables);

            Observable.FromEventPattern<PropertyChangedEventArgs>(_localizationService, nameof(_localizationService.PropertyChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(evt => OnLocaleChanged(evt.EventArgs))
                .DisposeWith(disposables);
            
            _themeService.SelectedTheme
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    InitializeBrushes();
                    SetMainGridBrush();
                })
                .DisposeWith(disposables);
        });

        InitializeBrushes();
        SetMainGridBrush();

        UpdateMachineDescription();
    }

    private void SetMainGridBrush()
    {
        MainGridBrush = IsLocalMachine switch
        {
            true => CurrentMemberBackGround,
            false => OtherMemberBackGround,
        };
        
        LetterBackBrush = IsLocalMachine switch
        {
            true => CurrentMemberLetterBackGround,
            false => OtherMemberLetterBackGround,
        };
        
        LetterBorderBrush = IsLocalMachine switch
        {
            true => CurrentMemberLetterBorder,
            false => OtherMemberLetterBorder,
        };
    }
    
    private void InitializeBrushes()
    {
        _currentMemberBackGround = _themeService.GetBrush("CurrentMemberBackGround")!;
        _otherMemberBackGround = _themeService.GetBrush("OtherMemberBackGround")!;
        
        _currentMemberLetterBackGround = _themeService.GetBrush("ConnectedMemberLetterBackGround")!;
        _otherMemberLetterBackGround = _themeService.GetBrush("DisabledMemberLetterBackGround")!;
        
        _currentMemberLetterBorder = _themeService.GetBrush("ConnectedMemberLetterBorder")!;
        _otherMemberLetterBorder = _themeService.GetBrush("DisabledMemberLetterBorder")!;
    }

    private IBrush CurrentMemberBackGround => _currentMemberBackGround;
    
    private IBrush OtherMemberBackGround => _otherMemberBackGround;
    
    private IBrush CurrentMemberLetterBackGround => _currentMemberLetterBackGround;
    
    private IBrush OtherMemberLetterBackGround => _otherMemberLetterBackGround;
    
    private IBrush CurrentMemberLetterBorder => _currentMemberLetterBorder;
    
    private IBrush OtherMemberLetterBorder => _otherMemberLetterBorder;

    private void OnLocaleChanged(PropertyChangedEventArgs _)
    {
        UpdateMachineDescription();
        UpdateStatus(SessionMember.SessionMemberGeneralStatus);
    }

    [Reactive]
    public string ClientInstanceId { get; set; }

    [Reactive]
    public string MachineDescription { get; set; }
        
    [Reactive]
    public bool IsLocalMachine { get; set; }
    
    [Reactive]
    public IBrush MainGridBrush { get; set; }
    
    [Reactive]
    public IBrush LetterBackBrush { get; set; }
        
    [Reactive]
    public IBrush LetterBorderBrush { get; set; }
    
    [Reactive]
    public DateTimeOffset JoinedSessionOn { get; set; } 
    
    [Reactive]
    public bool HasQuittedSessionAfterActivation { get; set; }
    
    public extern bool IsFileSystemSelectionEnabled { [ObservableAsProperty] get; }

    [Reactive]
    public string Status { get; set; }
    
    [Reactive]
    public int PositionInList { get; set; }
    
    internal SessionMember SessionMember { get; private set; }
    
    internal DataNode DataNode { get; private set; }

    private void UpdateMachineDescription()
    {
        string machineDescription;
        if (IsLocalMachine)
        {
            machineDescription = $"{_localizationService[nameof(Resources.SessionMachineView_ThisComputer)]} " +
                                 $"({SessionMember.MachineName}, {SessionMember.IpAddress})";

#if DEBUG
            machineDescription += " - PID:" + Process.GetCurrentProcess().Id;
#endif
        }
        else
        {
            machineDescription = $"{SessionMember.MachineName}, {SessionMember.IpAddress}";
        }
        
        if (HasQuittedSessionAfterActivation)
        {
            machineDescription += " - " + _localizationService[nameof(Resources.SessionMachineView_LeftSession)];
        }

        MachineDescription = machineDescription;
    }

    private void UpdateStatus(SessionMemberGeneralStatus localInventoryStatus)
    {
        switch (localInventoryStatus)
        {
            case SessionMemberGeneralStatus.InventoryWaitingForStart:
                Status = Resources.SessionMachine_Status_WaitingForStart;
                break;
            case SessionMemberGeneralStatus.InventoryRunningIdentification:
                Status = Resources.SessionMachine_Status_RunningIdentification;
                break;
            case SessionMemberGeneralStatus.InventoryWaitingForAnalysis:
                Status = Resources.SessionMachine_Status_WaitingForAnalysis;
                break;
            case SessionMemberGeneralStatus.InventoryRunningAnalysis:
                Status = Resources.SessionMachine_Status_RunningAnalysis;
                break;
            case SessionMemberGeneralStatus.InventoryCancelled:
                Status = Resources.SessionMachine_Status_InventoryCancelled;
                break;
            case SessionMemberGeneralStatus.InventoryError:
                Status = Resources.SessionMachine_Status_InventoryError;
                break;
            case SessionMemberGeneralStatus.InventoryFinished:
                Status = Resources.SessionMachine_Status_Finished;
                break;
            case SessionMemberGeneralStatus.SynchronizationRunning:
                Status = Resources.SessionMachine_Status_SynchronizationRunning;
                break;
            case SessionMemberGeneralStatus.SynchronizationSourceActionsInitiated:
                Status = Resources.SessionMachine_Status_SynchronizationSourceActionsInitiated;
                break;
            case SessionMemberGeneralStatus.SynchronizationError:
                Status = Resources.SessionMachine_Status_SynchronizationError;
                break;
            case SessionMemberGeneralStatus.SynchronizationFinished:
                Status = Resources.SessionMachine_Status_SynchronizationFinished;
                break;
            default:
                Status = Resources.SessionMachine_Status_UnknownStatus;
                break;
        }
    }
}