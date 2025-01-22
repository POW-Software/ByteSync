using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Business.Updates;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Updates;
using ByteSync.ViewModels.Misc;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Headers;

public class UpdateDetailsViewModel : FlyoutElementViewModel
{
    private readonly IUpdateService _updateService;
    private readonly IAvailableUpdateRepository _availableUpdateRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IWebAccessor _webAccessor;
    private readonly IUpdateRepository _updateRepository;
    private readonly ISoftwareVersionProxyFactory _softwareVersionProxyFactory;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<UpdateDetailsViewModel> _logger;
    
    private ReadOnlyObservableCollection<SoftwareVersionProxy> _bindingData;


    public UpdateDetailsViewModel()
    {

    }

    public UpdateDetailsViewModel(IUpdateService updateService, IAvailableUpdateRepository availableAvailableUpdateRepository, 
        ILocalizationService localizationService, IWebAccessor webAccessor, IUpdateRepository updateRepository,
        ISoftwareVersionProxyFactory softwareVersionProxyFactory, IEnvironmentService environmentService, 
        ErrorViewModel errorViewModel, ILogger<UpdateDetailsViewModel> logger)
    {
        AvailableUpdatesMessage = "";
        Progress = "";

        CancellationTokenSource = new CancellationTokenSource();

        _updateService = updateService;
        _availableUpdateRepository = availableAvailableUpdateRepository;
        _localizationService = localizationService;
        _webAccessor = webAccessor;
        _updateRepository = updateRepository;
        _softwareVersionProxyFactory = softwareVersionProxyFactory;
        _environmentService = environmentService;
        _logger = logger;

        Error = errorViewModel;
        
        SelectedVersion = null;
        IsAutoUpdating = false;
        
        ShowReleaseNotesCommand = ReactiveCommand.CreateFromTask<SoftwareVersionProxy>(ShowReleaseNotes);
        RunUpdateCommand = ReactiveCommand.CreateFromTask<SoftwareVersionProxy>(RunUpdate);

        _availableUpdateRepository.ObservableCache
            .Connect()
            .Transform(sw => _softwareVersionProxyFactory.CreateSoftwareVersionProxy(sw))
            .Sort(SortExpressionComparer<SoftwareVersionProxy>.Descending(proxy => proxy.Version))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _bindingData)
            .DisposeMany()
            .Subscribe();
        
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.SoftwareVersions, x => x.SoftwareVersions.Count)
                .Subscribe(_ => SetAvailableUpdate())
                .DisposeWith(disposables);
            
            _updateRepository.Progress.ProgressChanged += UpdateManager_ProgressReported;
        });
    }

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
    
    [Reactive]
    public bool IsAutoUpdating { get; set; }

    public bool CanAutoUpdate
    {
        get
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                   (_environmentService.IsPortableApplication && RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
        }
    }

    private void SetAvailableUpdate()
    {
        if (SoftwareVersions.Count == 1)
        {
            AvailableUpdatesMessage = _localizationService[nameof(Resources.UpdateDetails_AvailableUpdate)];
        }
        else
        {
            AvailableUpdatesMessage = String.Format(_localizationService[nameof(Resources.UpdateDetails_AvailableUpdates)], SoftwareVersions.Count);
        }

        if (!CanAutoUpdate)
        {
            AvailableUpdatesMessage += Environment.NewLine + 
                                       _localizationService[nameof(Resources.UpdateDetails_AutoUpdateNotSupported)];
        }
    }
    
    private async Task ShowReleaseNotes(SoftwareVersionProxy softwareVersionProxy)
    {
        try
        {
            var version = new Version(softwareVersionProxy.SoftwareVersion.Version);
            await _webAccessor.OpenReleaseNotes(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateDetailsViewModel: An error occurred while opening the release notes");

            Error.SetException(ex);
        }
    }
        
    private async Task RunUpdate(SoftwareVersionProxy? softwareVersionViewModel)
    {
        IsAutoUpdating = true;
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
            _logger.LogError(ex, "UpdateDetailsViewModel: An error occurred during the update");

            Error.SetException(ex);
        }
        finally
        {
            IsAutoUpdating = false;
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
            UpdateProgressStatus.MovingNewFiles => _localizationService[nameof(Resources.UpdateDetails_MovingNewFiles)].UppercaseFirst(),
            _ => throw new ArgumentOutOfRangeException(nameof(e.Status), e.Status, null)
        };

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