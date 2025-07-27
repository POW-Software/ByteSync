using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.ViewModels.Misc;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.DataNodes;

public class DataNodeHeaderViewModel : ActivatableViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly IDataNodeService _dataNodeService;
    private readonly SessionMember _sessionMember;
    private readonly DataNode _dataNode;

    private IBrush _currentMemberLetterBackGround = null!;
    private IBrush _otherMemberLetterBackGround = null!;
    private IBrush _currentMemberLetterBorder = null!;
    private IBrush _otherMemberLetterBorder = null!;

    public DataNodeHeaderViewModel()
    {

    }

    public DataNodeHeaderViewModel(SessionMember sessionMember,
        DataNode dataNode,
        bool isLocalMachine,
        ILocalizationService localizationService,
        IThemeService themeService,
        IDataNodeRepository dataNodeRepository,
        IDataNodeService dataNodeService,
        ErrorViewModel errorViewModel)
    {
        _sessionMember = sessionMember;
        _dataNode = dataNode;
        _localizationService = localizationService;
        _themeService = themeService;
        _dataNodeRepository = dataNodeRepository;
        _dataNodeService = dataNodeService;

        IsLocalMachine = isLocalMachine;
        ClientInstanceId = sessionMember.ClientInstanceId;
        Code = dataNode.Code;
        RemoveDataNodeError = errorViewModel;

        RemoveDataNodeCommand = ReactiveCommand.CreateFromTask(RemoveDataNode);

        InitializeBrushes();
        SetLetterBrushes();
        UpdateMachineDescription();

        this.WhenAnyValue(x => x.IsLocalMachine)
            .Subscribe(_ => SetLetterBrushes());

        this.WhenAnyValue(x => x.IsLocalMachine, x => x.HasQuittedSessionAfterActivation)
            .Subscribe(_ => UpdateMachineDescription());

        this.WhenActivated(disposables =>
        {
            var codeSubscription = _dataNode
                .WhenAnyValue(x => x.Code)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(newCode => Code = newCode);
            codeSubscription.DisposeWith(disposables);

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

            // Watch for DataNode changes to update CanRemoveDataNode
            _dataNodeRepository.ObservableCache.Connect()
                .WhereReasonsAre(ChangeReason.Add, ChangeReason.Remove)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateCanRemoveDataNode())
                .DisposeWith(disposables);

            UpdateCanRemoveDataNode();

            this.WhenAnyValue(x => x.IsLocalMachine, x => x.CanRemoveDataNode)
                .Select(tuple => tuple.Item1 && tuple.Item2)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.ShowRemoveButton)
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
    public string Code { get; set; } = string.Empty;

    [Reactive]
    public bool CanRemoveDataNode { get; private set; }

    [Reactive]
    public ErrorViewModel RemoveDataNodeError { get; set; } = null!;

    public extern bool ShowRemoveButton { [ObservableAsProperty] get; }

    public ReactiveCommand<Unit, Unit> RemoveDataNodeCommand { get; }

    private void UpdateCanRemoveDataNode()
    {
        if (!IsLocalMachine)
        {
            CanRemoveDataNode = false;
            return;
        }

        var currentMemberDataNodes = _dataNodeRepository.SortedCurrentMemberDataNodes;
        CanRemoveDataNode = currentMemberDataNodes.Count > 1;
    }

    private async Task RemoveDataNode()
    {
        try
        {
            RemoveDataNodeError.Clear();
            
            var success = await _dataNodeService.TryRemoveDataNode(_dataNode);
            
            if (!success)
            {
                // Create a generic exception for removal failure
                var exception = new InvalidOperationException("Failed to remove DataNode. Please try again.");
                RemoveDataNodeError.SetException(exception);
            }
        }
        catch (Exception ex)
        {
            RemoveDataNodeError.SetException(ex);
        }
    }

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