using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Managing;

public class SessionSettingsEditViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IDataInventoryStarter _dataInventoryStarter;
    private readonly IAnalysisModeViewModelFactory _analysisModeViewModelFactory;
    private readonly IDataTypeViewModelFactory _dataTypeViewModelFactory;
    private readonly ILinkingKeyViewModelFactory _linkingKeyViewModelFactory;
    private readonly ILogger<SessionSettingsEditViewModel> _logger;

#if DEBUG
    public SessionSettingsEditViewModel()
    {
        
    }
#endif

    public SessionSettingsEditViewModel(ISessionService sessionService, ILocalizationService localizationService, IDataInventoryStarter dataInventoryStarter,
        IAnalysisModeViewModelFactory analysisModeViewModelFactory, IDataTypeViewModelFactory dataTypeViewModelFactory, 
        ILinkingKeyViewModelFactory linkingKeyViewModelFactory, SessionSettings? sessionSettings, ILogger<SessionSettingsEditViewModel> logger)
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
        _dataInventoryStarter = dataInventoryStarter;
        _analysisModeViewModelFactory = analysisModeViewModelFactory ?? throw new ArgumentNullException(nameof(analysisModeViewModelFactory));
        _dataTypeViewModelFactory = dataTypeViewModelFactory;
        _linkingKeyViewModelFactory = linkingKeyViewModelFactory;
        _logger = logger;
        
        AvailableAnalysisModes =
        [
            _analysisModeViewModelFactory.CreateAnalysisModeViewModel(AnalysisModes.Smart),
            _analysisModeViewModelFactory.CreateAnalysisModeViewModel(AnalysisModes.Checksum)
        ];

        AvailableDataTypes =
        [
            _dataTypeViewModelFactory.CreateDataTypeViewModel(DataTypes.FilesDirectories),
            _dataTypeViewModelFactory.CreateDataTypeViewModel(DataTypes.Files),
            _dataTypeViewModelFactory.CreateDataTypeViewModel(DataTypes.Directories)
        ];

        AvailableLinkingKeys =
        [
            _linkingKeyViewModelFactory.CreateLinkingKeyViewModel(LinkingKeys.RelativePath),
            _linkingKeyViewModelFactory.CreateLinkingKeyViewModel(LinkingKeys.Name)
        ];

        Extensions = "";
        
        ListenEvents = true;

        this.WhenActivated(disposables =>
        {
            if (!ListenEvents)
            {
                return;
            }
            
            _sessionService.SessionSettingsObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnSessionSettingsUpdated)
                .DisposeWith(disposables);
            
            _dataInventoryStarter.CanCurrentUserStartInventory().CombineLatest(_sessionService.SessionStatusObservable,
                    (canCurrentUserStartInventory, sessionStatus) => canCurrentUserStartInventory && sessionStatus == SessionStatus.Preparation)
                .ToPropertyEx(this, x => x.CanEditSettings)
                .DisposeWith(disposables);
            
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleUpdated())
                .DisposeWith(disposables);

            ImportSettings(sessionSettings);
            
            this.WhenAnyValue(x => x.ExcludeHiddenFiles, x => x.ExcludeSystemFiles, x => x.LinkingKey,
                    x => x.DataType,
                    x => x.AnalysisMode, x => x.Extensions)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.MainThreadScheduler)
                .Subscribe(_ => SendUpdate());
        });
    }

    private void OnLocaleUpdated()
    {
        // comboBoxes don't update the SelectedText when you change the Description of one of the Items
        // To refresh the “SelectedText” of the comboBox, you must force the assignment
        // => IsUpdatingLocale avoids sending the Settings Update
        IsUpdatingLocale = true;
        
        foreach (var dataTypeViewModel in AvailableDataTypes)
        {
            dataTypeViewModel.UpdateDescription();
        }
        // Force to refresh the combo
        var dataType = DataType;
        DataType = null;
        DataType = dataType;
        
        foreach (var linkingKeyViewModel in AvailableLinkingKeys)
        {
            linkingKeyViewModel.UpdateDescription();
        }
        // Force to refresh the combo
        var linkingKey = LinkingKey;
        LinkingKey = null;
        LinkingKey = linkingKey;

        foreach (var analysisModeViewModel in AvailableAnalysisModes)
        {
            analysisModeViewModel.UpdateDescription();
        }
        // Force to refresh the combo
        var analysisMode = AnalysisMode;
        AnalysisMode = null;
        AnalysisMode = analysisMode;
        
        IsUpdatingLocale = false;
    }

    private bool ListenEvents { get; set; }

    [Reactive]
    public bool ExcludeHiddenFiles { get; set; }
    
    [Reactive]
    public bool ExcludeSystemFiles { get; set; }
    
    [Reactive]
    public AnalysisModeViewModel? AnalysisMode { get; set; }
    
    [Reactive]
    public DataTypeViewModel? DataType { get; set; }
    
    [Reactive]
    public LinkingKeyViewModel? LinkingKey { get; set; }

    [Reactive]
    public string? Extensions { get; set; }
    
    private bool IsUpdatingLocale { get; set; }

    public ObservableCollection<AnalysisModeViewModel> AvailableAnalysisModes { get; set; }
    
    public ObservableCollection<DataTypeViewModel> AvailableDataTypes { get; set; }
    
    public ObservableCollection<LinkingKeyViewModel> AvailableLinkingKeys { get; set; }
    
    public extern bool CanEditSettings { [ObservableAsProperty] get; }
    
    private async void SendUpdate()
    {
        if (IsUpdatingLocale)
        {
            return;
        }
        
        if (CanEditSettings)
        {
            try
            {
                var sessionSettings = ExportSettings();
                await _sessionService.SetSessionSettings(sessionSettings, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendUpdate");
            }
        }
    }

    public SessionSettings ExportSettings()
    {
        var settings = new SessionSettings();

        settings.DataType = DataType!.DataType;
        settings.LinkingKey = LinkingKey!.LinkingKey;
        // settings.LinkingCase = LinkingCase!.LinkingCase;
        
        settings.ExcludeHiddenFiles = ExcludeHiddenFiles;
        settings.ExcludeSystemFiles = ExcludeSystemFiles;

        settings.AnalysisMode = AnalysisMode!.AnalysisMode;

        settings.Extensions = Extensions;

        return settings;
    }
    
    private void OnSessionSettingsUpdated(SessionSettings? sessionSettings)
    {
        ImportSettings(sessionSettings);
    }

    public void ImportSettings(SessionSettings? cloudSessionSettings)
    {
        if (cloudSessionSettings == null)
        {
            return;
        }

        if (ExcludeHiddenFiles != cloudSessionSettings.ExcludeHiddenFiles)
        {
            ExcludeHiddenFiles = cloudSessionSettings.ExcludeHiddenFiles;
        }

        if (ExcludeSystemFiles != cloudSessionSettings.ExcludeSystemFiles)
        {
            ExcludeSystemFiles = cloudSessionSettings.ExcludeSystemFiles;
        }

        var newAnalysisMode = AvailableAnalysisModes.Single(aam => aam.AnalysisMode == cloudSessionSettings.AnalysisMode);
        if (!Equals(AnalysisMode, newAnalysisMode))
        {
            AnalysisMode = newAnalysisMode;
        }

        var newDataType = AvailableDataTypes.SingleOrDefault(aam => aam.DataType == cloudSessionSettings.DataType);
        if (!Equals(DataType, newDataType))
        {
            DataType = newDataType ?? AvailableDataTypes.SingleOrDefault(aam => aam.DataType == DataTypes.FilesDirectories);
        }
        
        var newLinkingKey = AvailableLinkingKeys.SingleOrDefault(abm => abm.LinkingKey == cloudSessionSettings.LinkingKey);
        if (!Equals(LinkingKey, newLinkingKey))
        {
            LinkingKey = newLinkingKey ?? AvailableLinkingKeys.SingleOrDefault(abm => abm.LinkingKey == LinkingKeys.Name);
        }

        if (Extensions.IsNotEmpty() && cloudSessionSettings.Extensions.IsNotEmpty()
                                    && ! Extensions!.Equals(cloudSessionSettings.Extensions, StringComparison.InvariantCultureIgnoreCase))
        {
            Extensions = cloudSessionSettings.Extensions;
        }
    }
}