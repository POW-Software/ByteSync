using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using ByteSync.Business;
using ByteSync.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;

namespace ByteSync.ViewModels.Misc;

class SelectLocaleViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly ILocalizationService _localizationService;

    public SelectLocaleViewModel() : this (null)
    {
    }

    public SelectLocaleViewModel(ILocalizationService? localizationService = null)
    {
        Activator = new ViewModelActivator();
        
        _localizationService = localizationService ?? Locator.Current.GetService<ILocalizationService>()!;

        CultureDefinitions = new ObservableCollection<CultureDefinition>(_localizationService.GetAvailableCultures());
        SelectedCulture = _localizationService.CurrentCultureDefinition;
        
        this.WhenAnyValue(x => x.SelectedCulture)
            .Skip(1)
            .Subscribe(culture =>
            {
                if (culture != null)
                {
                    Log.Information("SelectLocaleViewModel: user is changing the culture to {CultureCode}", culture.Code);
                    _localizationService.SetCurrentCulture(culture);
                }
            });
        
        this.WhenActivated(disposables =>
        {
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
                .DisposeWith(disposables);
        });
    }

    public ViewModelActivator Activator { get; }

    public ObservableCollection<CultureDefinition> CultureDefinitions { get; set; }

    [Reactive]
    public CultureDefinition? SelectedCulture { get; set; }

    private void OnLocaleChanged()
    {
        if (!Equals(_localizationService.CurrentCultureDefinition, SelectedCulture))
        {
            SelectedCulture = _localizationService.CurrentCultureDefinition;
        }
    }
}