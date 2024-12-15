using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business.Updates;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;
using ByteSync.ViewModels.Misc;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Headers;

public class UpdateDetailsViewModel : FlyoutElementViewModel
{
    private readonly IUpdateService _updateService;
    private readonly IAvailableUpdateRepository _availableUpdateRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IWebAccessor _webAccessor;
    private readonly IUpdateProgressRepository _updateProgressRepository;
    private readonly ISoftwareVersionProxyFactory _softwareVersionProxyFactory;

    public UpdateDetailsViewModel()
    {

    }

    public UpdateDetailsViewModel(IUpdateService updateService, IAvailableUpdateRepository availableAvailableUpdateRepository, 
        ILocalizationService localizationService, IWebAccessor webAccessor, IUpdateProgressRepository updateProgressRepository,
        ISoftwareVersionProxyFactory softwareVersionProxyFactory)
    {
        AvailableUpdatesMessage = "";
        Progress = "";

        CancellationTokenSource = new CancellationTokenSource();

        _updateService = updateService;
        _availableUpdateRepository = availableAvailableUpdateRepository;
        _localizationService = localizationService;
        _webAccessor = webAccessor;
        _updateProgressRepository = updateProgressRepository;
        _softwareVersionProxyFactory = softwareVersionProxyFactory;

        Error = new ErrorViewModel();
        
        SelectedVersion = null;
        
        ShowReleaseNotesCommand = ReactiveCommand.CreateFromTask<SoftwareVersionProxy>(ShowReleaseNotes);
        RunUpdateCommand = ReactiveCommand.CreateFromTask<SoftwareVersionProxy>(RunUpdate);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.SoftwareVersions, x => x.SoftwareVersions.Count)
                .Subscribe(_ => SetAvailableUpdate())
                .DisposeWith(disposables);

            _availableUpdateRepository.ObservableCache
                .Connect() // make the source an observable change set
                .Transform(sw => _softwareVersionProxyFactory.CreateSoftwareVersionProxy(sw))
                .Sort(SortExpressionComparer<SoftwareVersionProxy>.Descending(proxy => proxy.Version))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _bindingData)
                .DisposeMany()
                .Subscribe();
            
            // _updateService.NextAvailableVersionsObservable
            //     .Subscribe(FillSoftwareVersions)
            //     .DisposeWith(disposables);

            _updateProgressRepository.Progress.ProgressChanged += UpdateManager_ProgressReported;
        });
    }
    
    private ReadOnlyObservableCollection<SoftwareVersionProxy> _bindingData;

    private CancellationTokenSource CancellationTokenSource { get; }

    public ReactiveCommand<SoftwareVersionProxy, Unit> ShowReleaseNotesCommand { get; }
    
    public ReactiveCommand<SoftwareVersionProxy, Unit> RunUpdateCommand { get; }
    
    public ReadOnlyObservableCollection<SoftwareVersionProxy> SoftwareVersions => _bindingData;
        
    [Reactive]
    public string AvailableUpdatesMessage { get; set; }
        
    [Reactive]
    public SoftwareVersionProxy? SelectedVersion { get; set; }
    
    [Reactive]
    public string Progress { get; set; }

    [Reactive]
    public ErrorViewModel Error { get; set; }
        
    private void SetAvailableUpdate()
    {
        if (SoftwareVersions.Count == 1)
        {
            AvailableUpdatesMessage = _localizationService[nameof(Resources.Login_AvailableUpdate)];
        }
        else
        {
            AvailableUpdatesMessage = String.Format(_localizationService[nameof(Resources.Login_AvailableUpdates)], SoftwareVersions.Count);
        }
    }
    
    // private void FillSoftwareVersions(List<SoftwareVersion>? softwareVersions)
    // {
    //     SoftwareVersions.Clear();
    //
    //     if (softwareVersions == null)
    //     {
    //         return;
    //     }
    //     
    //     foreach (var softwareVersion in softwareVersions)
    //     {
    //         if (!SoftwareVersions.Any(sv => sv.SoftwareVersion.Version.Equals(softwareVersion.Version)))
    //         {
    //             SoftwareVersionViewModel softwareVersionViewModel = new SoftwareVersionViewModel(softwareVersion);
    //             SoftwareVersions.Add(softwareVersionViewModel);
    //         }
    //     }
    // }
        
    // private void UpdateServiceNextVersionChanged(object? sender, SoftwareVersionEventArgs e)
    // {
    //     FillSoftwareVersions(e.SoftwareVersions);
    // }
    
    private async Task ShowReleaseNotes(SoftwareVersionProxy softwareVersionProxy)
    {
        try
        {
            var version = new Version(softwareVersionProxy.SoftwareVersion.Version);
            await _webAccessor.OpenReleaseNotes(version);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UpdateDetailsViewModel: An error occurred while opening the release notes");

            Error.SetException(ex);
        }
    }
        
    private async Task RunUpdate(SoftwareVersionProxy? softwareVersionViewModel)
    {
        Container.CanCloseCurrentFlyout = false;
        
        SelectedVersion = softwareVersionViewModel;

        try
        {
            if (softwareVersionViewModel?.SoftwareVersion != null)
            {
                await _updateService.UpdateAsync(softwareVersionViewModel.SoftwareVersion, CancellationTokenSource.Token);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UpdateDetailsViewModel: An error occurred during the update");

            Error.SetException(ex);
        }
        finally
        {
            Container.CanCloseCurrentFlyout = true;
        }
    }
        
    private void UpdateManager_ProgressReported(object? sender, UpdateProgress e)
    {
        var progress = e.Status switch
        {
            UpdateProgressStatus.Downloading => _localizationService[nameof(Resources.UpdateDetails_Downloading)].UppercaseFirst(),
            UpdateProgressStatus.Extracting => _localizationService[nameof(Resources.UpdateDetails_Extracting)].UppercaseFirst(),
            UpdateProgressStatus.RestartingApplication => _localizationService[nameof(Resources.UpdateDetails_RestartingApplication)].UppercaseFirst(),
            UpdateProgressStatus.UpdatingFiles => _localizationService[nameof(Resources.UpdateDetails_UpdatingFiles)].UppercaseFirst(),
            UpdateProgressStatus.BackingUpExistingFiles => _localizationService[nameof(Resources.UpdateDetails_BackingUpExistingFiles)].UppercaseFirst(),
            _ => throw new ArgumentOutOfRangeException(nameof(e.Status), e.Status, null)
        };

        // string progress = e.Status.ToString();

        if (e.Percentage != null)
        {
            progress += $" - {e.Percentage}%";
        }

        Progress = progress;
    }

    public override Task CancelIfNeeded()
    {
        CancellationTokenSource.Cancel();
        
        return Task.CompletedTask;
    }
}