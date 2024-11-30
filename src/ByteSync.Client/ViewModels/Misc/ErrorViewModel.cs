using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Misc;

public class ErrorViewModel : ActivableViewModelBase 
{
    private readonly ILocalizationService _localizationService;

    public ErrorViewModel()
    {
        
    }

    public ErrorViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        
        Exception = null;

        this.WhenAnyValue(x => x.ErrorMessageKey)
            .Skip(1)
            .Subscribe(_ => UpdateErrorMessage());

        this.WhenActivated(disposables =>
        {
            // Observable.FromEventPattern<EventArgs>(_localizationService, nameof(_localizationService.PropertyChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateErrorMessage())
                .DisposeWith(disposables);
        });
    }

    private Exception? Exception { get; set; }
    
    [Reactive]
    public string? ErrorMessage { get; set; }
    
    [Reactive]
    public string? ErrorMessageKey { get; set; }

    public void SetException(Exception exception)
    {
        Exception = exception;
        ErrorMessageKey = null;

        UpdateErrorMessage();
    }
    
    public void Clear()
    {
        Exception = null;
        ErrorMessageKey = null;

        UpdateErrorMessage();
    }

    private void UpdateErrorMessage()
    {
        if (Exception != null)
        {
            ErrorMessage = String.Format(_localizationService[nameof(Resources.ErrorView_ErrorMessage)], Exception.GetType().Name + " - " + Exception.Message);
        }
        else if (ErrorMessageKey.IsNotEmpty())
        {
            ErrorMessage = String.Format(_localizationService[ErrorMessageKey!]);
        }
        else
        {
            ErrorMessage = null;
        }
    }
}