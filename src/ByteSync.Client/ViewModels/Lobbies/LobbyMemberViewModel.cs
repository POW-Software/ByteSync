using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Lobbies;
using ByteSync.Business.PathItems;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Lobbies;
using ByteSync.Services.Lobbies;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Lobbies;

public class LobbyMemberViewModel : ActivatableViewModelBase
{
    private readonly ILobbyRepository _lobbyRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IEnvironmentService _environmentService;
    private readonly IPathItemProxyFactory _pathItemProxyFactory;

    public LobbyMemberViewModel(LobbyMember lobbyMember, ILobbyRepository lobbyRepository, 
        ILocalizationService localizationService,
        IEnvironmentService environmentService, 
        IPathItemProxyFactory pathItemProxyFactory)
    {
        _lobbyRepository = lobbyRepository;
        _localizationService = localizationService;
        _environmentService = environmentService;
        _pathItemProxyFactory = pathItemProxyFactory;
        
        MemberLetter = lobbyMember.MemberLetter;
        LobbyMember = lobbyMember;
        
    #if DEBUG
        if (Design.IsDesignMode)
        {
            return;
        }
    #endif

        LobbyId = _lobbyRepository.GetData()!.LobbyId;

        PathItems = new ObservableCollection<PathItemProxy>();
        foreach (var sessionProfilePathItem in lobbyMember.CloudSessionProfileMember.PathItems.OrderBy(pi => pi.Code))
        {
            var pathItem = new PathItem
            {
                Code = sessionProfilePathItem.Code,
                Type = sessionProfilePathItem.Type,
                Path = sessionProfilePathItem.Path,
            };
            
            var pathItemViewModel = _pathItemProxyFactory.CreatePathItemProxy(pathItem);
            
            PathItems.Add(pathItemViewModel);
        }


        var localProfilClientId = _lobbyRepository.Get(LobbyId, details => details.LocalProfileClientId);
        
        IsNonLobbyLocalMachine = LobbyId.StartsWith(LobbyRepository.LOCAL_LOBBY_PREFIX)
                                 && LobbyMember.ProfileClientId.Equals(localProfilClientId);
        IsNonLobbyOtherMachine = LobbyId.StartsWith(LobbyRepository.LOCAL_LOBBY_PREFIX)
                                 && ! LobbyMember.ProfileClientId.Equals(localProfilClientId);
        

        // Status = lobbyMember.Status;

        this.WhenAnyValue(x => x.LobbyMember.LobbyMemberInfo)
            .Select(x => x == null)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.IsOtherMemberNonConnected);
        
        this.WhenAnyValue(x => x.LobbyMember.LobbyMemberInfo)
            .Select(x => x != null && x.ClientInstanceId.Equals(_environmentService.ClientInstanceId))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.IsLobbyLocalMember);
        
        this.WhenAnyValue(x => x.LobbyMember.LobbyMemberInfo)
            .Select(x => x != null && ! x.ClientInstanceId.Equals(_environmentService.ClientInstanceId))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.IsOtherMemberConnected);

        this.WhenAnyValue(x => x.LobbyMember.LobbyMemberInfo)
            .Where(x => x != null)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => IpAddress = LobbyMember.LobbyMemberInfo?.IpAddress);
        
        this.WhenAnyValue(x => x.LobbyMember.LobbyMemberInfo)
            .Where(x => x == null)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => IpAddress = null);
        
        UpdateMachineDescription();
        
        this.WhenActivated(disposables =>
        {
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
                .DisposeWith(disposables);
        });
    }

    [Reactive]
    public LobbyMember LobbyMember { get; set; }
    
    [Reactive]
    public string LobbyId { get; set; }

    public extern bool IsLobbyLocalMember { [ObservableAsProperty] get; }
    
    public extern bool IsOtherMemberNonConnected { [ObservableAsProperty] get; }
    
    public extern bool IsOtherMemberConnected { [ObservableAsProperty] get; }

    public string MemberLetter { get; }
    
    public string MachineDescription { get; set; }

    [Reactive]
    public string? IpAddress { get; set; }
    
    [Reactive]
    public bool IsNonLobbyLocalMachine { get; set; }
    
    [Reactive]
    public bool IsNonLobbyOtherMachine { get; set; }
    
    public ObservableCollection<PathItemProxy> PathItems { get; }

    private void UpdateMachineDescription()
    {
        string machineDescription;
        if (IsLobbyLocalMember || IsNonLobbyLocalMachine)
        {
            machineDescription = $"{_localizationService[nameof(Resources.SessionMachineView_ThisComputer)]} " +
                                 $"({LobbyMember.MachineName})";
        }
        else
        {
            machineDescription = $"{LobbyMember.MachineName}";
        }

        MachineDescription = machineDescription;
    }

    private void OnLocaleChanged()
    {
        UpdateMachineDescription();
        
        foreach (var pathItemViewModel in PathItems)
        {
            pathItemViewModel.OnLocaleChanged(_localizationService);
        }
    }
}