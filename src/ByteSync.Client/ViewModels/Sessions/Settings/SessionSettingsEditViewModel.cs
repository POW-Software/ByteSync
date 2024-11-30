using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using ByteSync.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.ViewModels.Sessions.Cloud.Managing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Settings;

public class SessionSettingsEditViewModel : ActivableViewModelBase
{
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IDataInventoryStarter _dataInventoryStarter;
    private readonly AnalysisModeViewModelFactory _analysisModeViewModelFactory;
    private readonly DataTypeViewModelFactory _dataTypeViewModelFactory;
    private readonly LinkingKeyViewModelFactory _linkingKeyViewModelFactory;

#if DEBUG
    public SessionSettingsEditViewModel()
    {
        
    }
#endif
    
    // public SessionSettingsEditViewModel(SessionSettings sessionSettings) : this (null, null)
    // {
    //     ImportSettings(sessionSettings);
    //
    //     ListenEvents = false;
    // }

    public SessionSettingsEditViewModel(ICloudSessionEventsHub cloudSessionEventsHub, ISessionService sessionService,
        ILocalizationService localizationService, IDataInventoryStarter dataInventoryStarter,
        AnalysisModeViewModelFactory analysisModeViewModelFactory, DataTypeViewModelFactory dataTypeViewModelFactory, 
        LinkingKeyViewModelFactory linkingKeyViewModelFactory, SessionSettings? sessionSettings)
    {
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _sessionService = sessionService;
        _localizationService = localizationService;
        _dataInventoryStarter = dataInventoryStarter;
        _analysisModeViewModelFactory = analysisModeViewModelFactory;
        _dataTypeViewModelFactory = dataTypeViewModelFactory;
        _linkingKeyViewModelFactory = linkingKeyViewModelFactory;
        
        AvailableAnalysisModes = new ObservableCollection<AnalysisModeViewModel>();
        AvailableAnalysisModes.Add(_analysisModeViewModelFactory.Invoke(AnalysisModes.Smart));
        AvailableAnalysisModes.Add(_analysisModeViewModelFactory.Invoke(AnalysisModes.Checksum));

        AvailableDataTypes = new ObservableCollection<DataTypeViewModel>();
        AvailableDataTypes.Add(_dataTypeViewModelFactory.Invoke(DataTypes.FilesDirectories));
        AvailableDataTypes.Add(_dataTypeViewModelFactory.Invoke(DataTypes.Files));
        AvailableDataTypes.Add(_dataTypeViewModelFactory.Invoke(DataTypes.Directories));

        AvailableLinkingKeys = new ObservableCollection<LinkingKeyViewModel>();
        AvailableLinkingKeys.Add(_linkingKeyViewModelFactory.Invoke(LinkingKeys.RelativePath));
        AvailableLinkingKeys.Add(_linkingKeyViewModelFactory.Invoke(LinkingKeys.Name));
        
        // this.WhenAnyValue(
        //         x => x.IsSessionCreatedByMe, x => x.IsSessionActivated, x => x.IsCloudSession, x => x.IsProfileSession,
        //         (isSessionCreatedByMe, isSessionActivated, isCloudSession, isProfileSession) 
        //             => (!isCloudSession || isSessionCreatedByMe) && !isSessionActivated && !(isCloudSession && isProfileSession))
        //     .ToPropertyEx(this, x => x.CanEditSettings);

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
            
            _dataInventoryStarter.CanCurrentUserStartInventory()
                .ToPropertyEx(this, x => x.CanEditSettings)
                .DisposeWith(disposables);
            
            // Observable.FromEventPattern<CloudSessionSettingsEventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionSettingsUpdated))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnSessionSettingsUpdated(evt.EventArgs.SessionSettings))
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionActivated))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => IsSessionActivated = true)
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionResetted))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnSessionResetted())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.MemberQuittedSession))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnMemberQuittedSession())
            //     .DisposeWith(disposables);
            
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleUpdated())
                .DisposeWith(disposables);

            // IsCloudSession = _sessionService.IsCloudSession;
            // IsSessionCreatedByMe = _sessionService.IsCloudSessionCreatedByMe;
            // IsSessionActivated = _sessionService.IsSessionActivated;
            // IsProfileSession = _sessionService.IsProfileSession;

            ImportSettings(sessionSettings);
            
            this.WhenAnyValue(x => x.ExcludeHiddenFiles, x => x.ExcludeSystemFiles, x => x.LinkingKey,
                    x => x.DataType,
                    x => x.AnalysisMode, x => x.Extensions)
                // .Throttle(TimeSpan.FromSeconds(0.8), RxApp.TaskpoolScheduler)
                .Skip(1)
                .Subscribe(_ => SendUpdate());
        });
    }

    // private void OnSessionResetted()
    // {
    //     IsSessionActivated = false;
    // }

    private void OnLocaleUpdated()
    {
        // Les comboBox ne mettent pas à jour le "SelectedText" quand on change la Description d'un des Items
        // Pour rafraichir le "SelectedText" du comboBox, on doit forcer l'affectation
        //  => IsUpdatingLocale permet d'éviter d'envoyer la MAJ des Settings
        IsUpdatingLocale = true;
        
        foreach (var dataTypeViewModel in AvailableDataTypes)
        {
            dataTypeViewModel.UpdateDescription();
        }
        // On force pour rafraichir le combo
        var dataType = DataType;
        DataType = null;
        DataType = dataType;
        
        foreach (var linkingKeyViewModel in AvailableLinkingKeys)
        {
            linkingKeyViewModel.UpdateDescription();
        }
        // On force pour rafraichir le combo
        var linkingKey = LinkingKey;
        LinkingKey = null;
        LinkingKey = linkingKey;

        foreach (var analysisModeViewModel in AvailableAnalysisModes)
        {
            analysisModeViewModel.UpdateDescription();
        }
        // On force pour rafraichir le combo
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

    // [Reactive]
    // public bool IsCloudSession { get; set; }
    //
    // [Reactive]
    // public bool IsSessionCreatedByMe { get; set; }
    //
    // [Reactive]
    // public bool IsSessionActivated { get; set; }
    //
    // [Reactive]
    // public bool IsProfileSession { get; set; }
    
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
                await _sessionService.SetSessionSettings(sessionSettings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SendUpdate");
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
    
    // private void OnMemberQuittedSession()
    // {
    //     IsSessionCreatedByMe = _sessionService.IsCloudSessionCreatedByMe;
    // }
}