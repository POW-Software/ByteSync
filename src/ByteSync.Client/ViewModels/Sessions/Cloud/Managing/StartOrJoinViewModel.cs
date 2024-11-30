using System.Reactive;
using ByteSync.Business.Navigations;
using ByteSync.Interfaces.Controls.Navigations;
using ReactiveUI;
using Splat;

namespace ByteSync.ViewModels.Sessions.Cloud.Managing;

public class StartOrJoinViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public StartOrJoinViewModel() : this (null)
    {
    }

    public StartOrJoinViewModel(INavigationService? navigationService = null)
    {
        _navigationService = navigationService ?? Locator.Current.GetService<INavigationService>()!;
        
        CancelCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));
    }

    public ReactiveCommand<Unit, Unit>? StartComparisonCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? JoinComparisonCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }
}