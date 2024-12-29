using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Mixins;
using Avalonia.Threading;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Events;
using ByteSync.Business.Profiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Misc;

public class FlyoutContainerViewModel : ActivableViewModelBase, IDialogView
{
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly ILocalizationService _localizationService;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;

    public FlyoutContainerViewModel()
    {
            
    }
        
    public FlyoutContainerViewModel(INavigationEventsHub navigationEventsHub, ILocalizationService localizationService,
        IFlyoutElementViewModelFactory flyoutElementViewModelFactory)
    {
        // Required for “WhenActivated” activation to occur. Inverted at the start of WhenActivated
        IsFlyoutContainerVisible = true;
        HasBeenActivatedOnce = false;
        CanCloseCurrentFlyout = true;

        WaitingFlyoutCanNowBeOpened = new AutoResetEvent(false);

        _navigationEventsHub = navigationEventsHub;
        _localizationService = localizationService;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;
            
        Title = "";
        Content = null; 

        var canClose = this.WhenAnyValue(x => x.CanCloseCurrentFlyout);
        CloseCommand = ReactiveCommand.Create(Close, canClose);
        
        // https://stackoverflow.com/questions/29100381/getting-prior-value-on-change-of-property-using-reactiveui-in-wpf-mvvm
        // https://stackoverflow.com/questions/35784016/whenany-observableforproperty-how-to-access-previous-and-new-value
        // Pourrait se simplier avec une propriété bool/Reactive sur Content pour savoir si CloseFlyout a été requesté
        this.WhenAnyValue(x => x.Content)
            .StartWith(this.Content)
            .Buffer(2, 1)
            .Select(b => (Previous: b[0], Current: b[1]))
            .Subscribe(x =>
            {
                if (x.Previous != null)
                {
                    x.Previous.CloseFlyoutRequested -= OnCloseFlyoutRequested;
                }
                
                if (x.Current != null)
                {
                    x.Current.CloseFlyoutRequested += OnCloseFlyoutRequested;
                }
            });

        this.WhenActivated(disposables =>
        {
            // On repasse ici quand le thème est changé !
            // HasBeenActivatedOnce permet de ne traiter qu'une fois et d'éviter les problèmes de Flyout suite à changement de thème
            if (!HasBeenActivatedOnce)
            {
                DoCloseFlyout();
                HasBeenActivatedOnce = true;
            }
            
            Observable.FromEventPattern<TrustKeyDataRequestedArgs>(_navigationEventsHub, nameof(_navigationEventsHub.TrustKeyDataRequested))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => OnTrustKeyDataRequested(args.EventArgs))
                .DisposeWith(disposables);
            
            Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.CreateCloudSessionProfileRequested))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnCreateCloudSessionProfileRequested())
                .DisposeWith(disposables);
            
            Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.CreateLocalSessionProfileRequested))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnCreateLocalSessionProfileRequested())
                .DisposeWith(disposables);
        });
    }

    public AutoResetEvent WaitingFlyoutCanNowBeOpened { get; set; }

    private bool HasBeenActivatedOnce { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
        
    [Reactive]
    public string? Title { get; set; }

    [Reactive]
    public FlyoutElementViewModel? Content { get; set; }

    [Reactive]
    public bool IsFlyoutContainerVisible { get; set; }
    
    [Reactive]
    public bool CanCloseCurrentFlyout { get; set; }
    
    private void OnCloseFlyoutRequested(object? sender, EventArgs e)
    {
        CloseFlyout();
    }

    public async Task<MessageBoxResult?> ShowMessageBoxAsync(MessageBoxViewModel messageBoxViewModel)
    {
        ShowFlyout(messageBoxViewModel.TitleKey, false, messageBoxViewModel);

        var result = await messageBoxViewModel.WaitForResponse();

        return result;
    }
    
    private void OnTrustKeyDataRequested(TrustKeyDataRequestedArgs trustKeyDataRequestedArgs)
    {
        // Ici: contrôler qu'un affichage n'est pas déjà en cours ???
        
        ShowFlyout(nameof(Resources.Shell_TrustedClients), false, 
            _flyoutElementViewModelFactory.BuilAddTrustedClientViewModel(trustKeyDataRequestedArgs.PublicKeyCheckData, 
                trustKeyDataRequestedArgs.TrustDataParameters));
    }
    
    private void OnCreateCloudSessionProfileRequested()
    {
        ShowFlyout(nameof(Resources.Shell_CreateCloudSessionProfile), false, 
            _flyoutElementViewModelFactory.BuildCreateSessionProfileViewModel(ProfileTypes.Cloud));
    }
    
    private void OnCreateLocalSessionProfileRequested()
    {
        ShowFlyout(nameof(Resources.Shell_CreateLocalSessionProfile), false, 
            _flyoutElementViewModelFactory.BuildCreateSessionProfileViewModel(ProfileTypes.Local));
    }
    
    public void ShowFlyout(string titleKey, bool toggle, FlyoutElementViewModel flyoutElementViewModel) 
    {
        if (toggle && Content != null && Content.GetType() == flyoutElementViewModel.GetType())
        {
            CloseFlyout();
        }
        else
        {
            if (!CanCloseCurrentFlyout && Content != null)
            {
                // On relance la demande en Taské
                Task.Run(() =>
                {
                    WaitingFlyoutCanNowBeOpened.WaitOne();

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ShowFlyout(titleKey, toggle, flyoutElementViewModel);
                    });
                });

                return;
            }

            WaitingFlyoutCanNowBeOpened.Reset();
            
            // On désactive le contenu actuel
            Content?.CancelIfNeeded();
            
            DoShowFlyout(titleKey, flyoutElementViewModel);
        }
    }

    private void DoShowFlyout<T>(string titleKey, T instance) where T : FlyoutElementViewModel
    {
        IsFlyoutContainerVisible = true;
        Content = instance;
        instance.Container = this;

        var title = _localizationService[titleKey];
        Title = title;

        instance.OnDisplayed();
    }
    
    private void Close()
    {
        DoCloseFlyout();
    }

    public void CloseFlyout()
    {
        DoCloseFlyout();
    }

    private void DoCloseFlyout()
    {
        var needSetWaitingFlyoutCanNowBeOpened = HasBeenActivatedOnce && (Content != null || IsFlyoutContainerVisible);
        
        Content?.CancelIfNeeded();
        Content = null;

        IsFlyoutContainerVisible = false;

        if (needSetWaitingFlyoutCanNowBeOpened)
        {
            WaitingFlyoutCanNowBeOpened.Set();
        }
    }
}