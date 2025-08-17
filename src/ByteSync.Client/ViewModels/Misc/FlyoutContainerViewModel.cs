using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Avalonia.Threading;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Business.Profiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Misc;

public class FlyoutContainerViewModel : ActivatableViewModelBase, IDialogView
{
    private readonly ILocalizationService _localizationService;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;

    public FlyoutContainerViewModel()
    {
            
    }
        
    public FlyoutContainerViewModel(ILocalizationService localizationService,
        IFlyoutElementViewModelFactory flyoutElementViewModelFactory)
    {
        // Required for “WhenActivated” activation to occur. Inverted at the start of WhenActivated
        IsFlyoutContainerVisible = true;
        HasBeenActivatedOnce = false;
        CanCloseCurrentFlyout = true;

        WaitingFlyoutCanNowBeOpened = new AutoResetEvent(false);
        
        _localizationService = localizationService;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;
            
        Title = "";
        Content = null; 

        var canClose = this.WhenAnyValue(x => x.CanCloseCurrentFlyout);
        CloseCommand = ReactiveCommand.Create(Close, canClose);
        
        // https://stackoverflow.com/questions/29100381/getting-prior-value-on-change-of-property-using-reactiveui-in-wpf-mvvm
        // https://stackoverflow.com/questions/35784016/whenany-observableforproperty-how-to-access-previous-and-new-value
        // Could be simplified with a bool/Reactive property on Content to know if CloseFlyout has been requested
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

        this.WhenActivated(HandleActivation);
    }

    private void HandleActivation(IDisposable compositeDisposable)
    {
        // We switch back here when the theme is changed!
        // HasBeenActivatedOnce allows you to process only once and avoid flyout problems following a theme change
        
        if (!HasBeenActivatedOnce)
        {
            DoCloseFlyout();
            HasBeenActivatedOnce = true;
        }
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
    
    private void OnCreateCloudSessionProfileRequested()
    {
        // Keep until feature is implemented
        ShowFlyout(nameof(Resources.Shell_CreateCloudSessionProfile), false, 
            _flyoutElementViewModelFactory.BuildCreateSessionProfileViewModel(ProfileTypes.Cloud));
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
                // We relaunch the request with a task
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
            
            // Disable current content
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