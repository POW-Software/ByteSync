using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members;

public class DataNodeHeaderViewModel : ActivatableViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly SessionMember _sessionMember;

    private IBrush _currentMemberLetterBackGround = null!;
    private IBrush _otherMemberLetterBackGround = null!;
    private IBrush _currentMemberLetterBorder = null!;
    private IBrush _otherMemberLetterBorder = null!;

    public DataNodeHeaderViewModel(SessionMember sessionMember,
        bool isLocalMachine,
        ILocalizationService localizationService,
        IThemeService themeService,
        ISessionMemberRepository sessionMemberRepository)
    {
        _sessionMember = sessionMember;
        _localizationService = localizationService;
        _themeService = themeService;
        _sessionMemberRepository = sessionMemberRepository;

        IsLocalMachine = isLocalMachine;
        ClientInstanceId = sessionMember.ClientInstanceId;

        InitializeBrushes();
        SetLetterBrushes();
        UpdateMachineDescription();

        this.WhenAnyValue(x => x.IsLocalMachine)
            .Subscribe(_ => SetLetterBrushes());

        this.WhenAnyValue(x => x.IsLocalMachine, x => x.HasQuittedSessionAfterActivation)
            .Subscribe(_ => UpdateMachineDescription());

        this.WhenActivated(disposables =>
        {
            _sessionMemberRepository.Watch(sessionMember)
                .Subscribe(item => PositionInList = item.Current.PositionInList)
                .DisposeWith(disposables);

            Observable.FromEventPattern<PropertyChangedEventArgs>(_localizationService, nameof(_localizationService.PropertyChanged))
                .Subscribe(_ => UpdateMachineDescription())
                .DisposeWith(disposables);

            _themeService.SelectedTheme.Skip(1)
                .Subscribe(_ =>
                {
                    InitializeBrushes();
                    SetLetterBrushes();
                })
                .DisposeWith(disposables);
        });
    }

    [Reactive]
    public string ClientInstanceId { get; private set; }

    [Reactive]
    public string MachineDescription { get; private set; } = string.Empty;

    [Reactive]
    public bool IsLocalMachine { get; private set; }

    [Reactive]
    public bool HasQuittedSessionAfterActivation { get; set; }

    [Reactive]
    public IBrush LetterBackBrush { get; private set; } = null!;

    [Reactive]
    public IBrush LetterBorderBrush { get; private set; } = null!;

    [Reactive]
    public int PositionInList { get; private set; }

    private void InitializeBrushes()
    {
        _currentMemberLetterBackGround = _themeService.GetBrush("ConnectedMemberLetterBackGround")!;
        _otherMemberLetterBackGround = _themeService.GetBrush("DisabledMemberLetterBackGround")!;
        _currentMemberLetterBorder = _themeService.GetBrush("ConnectedMemberLetterBorder")!;
        _otherMemberLetterBorder = _themeService.GetBrush("DisabledMemberLetterBorder")!;
    }

    private void SetLetterBrushes()
    {
        LetterBackBrush = IsLocalMachine ? _currentMemberLetterBackGround : _otherMemberLetterBackGround;
        LetterBorderBrush = IsLocalMachine ? _currentMemberLetterBorder : _otherMemberLetterBorder;
    }

    private void UpdateMachineDescription()
    {
        string machineDescription;
        if (IsLocalMachine)
        {
            machineDescription = $"{_localizationService[nameof(Resources.SessionMachineView_ThisComputer)]} " +
                                 $"({_sessionMember.MachineName}, {_sessionMember.IpAddress})";
#if DEBUG
            machineDescription += " - PID:" + Process.GetCurrentProcess().Id;
#endif
        }
        else
        {
            machineDescription = $"{_sessionMember.MachineName}, {_sessionMember.IpAddress}";
        }

        if (HasQuittedSessionAfterActivation)
        {
            machineDescription += " - " + _localizationService[nameof(Resources.SessionMachineView_LeftSession)];
        }

        MachineDescription = machineDescription;
    }
} 