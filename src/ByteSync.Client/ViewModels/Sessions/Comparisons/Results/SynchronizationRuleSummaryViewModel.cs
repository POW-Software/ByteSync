using System.Reactive;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class SynchronizationRuleSummaryViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IDescriptionBuilderFactory _descriptionBuilderFactory;
    private readonly ISynchronizationRuleRepository _synchronizationRuleRepository;
    private readonly IDialogService _dialogService;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;

    private const string ICON_FILE = "RegularFile";
    private const string ICON_FOLDER = "RegularFolder";
    
    public SynchronizationRuleSummaryViewModel()
    {

    }

    public SynchronizationRuleSummaryViewModel(SynchronizationRule synchronizationRule, ISessionService sessionService, ILocalizationService localizationService, 
        IDescriptionBuilderFactory descriptionBuilderFactory, ISynchronizationRuleRepository synchronizationRuleRepository, 
        IDialogService dialogService, IFlyoutElementViewModelFactory flyoutElementViewModelFactory) 
        : this()
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
        _descriptionBuilderFactory = descriptionBuilderFactory;
        _synchronizationRuleRepository = synchronizationRuleRepository;
        _dialogService = dialogService;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;
        
        var canEditOrRemove = this
            .WhenAnyValue(x => x.HasSynchronizationStarted,
                (isSyncStarted) => !isSyncStarted)
            .ObserveOn(RxApp.MainThreadScheduler);
        
        RemoveCommand = ReactiveCommand.Create(Remove, canEditOrRemove);
        DuplicateCommand = ReactiveCommand.Create(Duplicate, canEditOrRemove);
        EditCommand = ReactiveCommand.Create<Avalonia.Input.PointerPressedEventArgs>(Edit, canEditOrRemove);
        
    #if DEBUG
        Mode = "Mode";
        Conditions = "Condition 1" + Environment.NewLine + "Condition 2";
        IconName = ICON_FILE;
        IsIconVisible = true;
    #endif

        IsIconVisible = _sessionService.CurrentSessionSettings!.DataType == DataTypes.FilesDirectories;
        IconName = synchronizationRule.FileSystemType == FileSystemTypes.File
            ? ICON_FILE
            : ICON_FOLDER;
        
        UpdateAutomaticAction(synchronizationRule);
        
        UpdateElementType();
    }

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; set; }

    public ReactiveCommand<Avalonia.Input.PointerPressedEventArgs, Unit> EditCommand { get; set; }

    public SynchronizationRule SynchronizationRule { get; private set; }

    [Reactive]
    public string Mode { get; set; }

    [Reactive]
    public string Conditions { get; set; }

    [Reactive]
    public string Then { get; set; }

    [Reactive]
    public string Actions { get; set; }

    [Reactive]
    public string IconName { get; set; }

    [Reactive]
    public bool IsIconVisible { get; set; }
    
    [Reactive]
    public bool HasSynchronizationStarted { get; set; }

    [Reactive]
    public string ElementType { get; set; }

    private void BuildDescription()
    {
        var synchronizationRuleDescriptionBuilder = _descriptionBuilderFactory.CreateSynchronizationRuleDescriptionBuilder(SynchronizationRule);
        synchronizationRuleDescriptionBuilder.BuildDescription(Environment.NewLine);
        
        Mode = synchronizationRuleDescriptionBuilder.Mode!;
        Conditions = synchronizationRuleDescriptionBuilder.Conditions!;
        Then = synchronizationRuleDescriptionBuilder.Then!;
        Actions = synchronizationRuleDescriptionBuilder.Actions!;
    }

    private void Remove()
    {
        _synchronizationRuleRepository.Remove(SynchronizationRule);
    }
    
    private void Duplicate()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_DuplicateSynchronizationRule), false,
            _flyoutElementViewModelFactory.BuilSynchronizationRuleGlobalViewModel(SynchronizationRule, true));
    }

    private void Edit(Avalonia.Input.PointerPressedEventArgs _)
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_DuplicateSynchronizationRule), false,
            _flyoutElementViewModelFactory.BuilSynchronizationRuleGlobalViewModel(SynchronizationRule, false));
    }

    public void UpdateAutomaticAction(SynchronizationRule synchronizationRule)
    {
        SynchronizationRule = synchronizationRule;

        BuildDescription();
    }
    
    private void UpdateElementType()
    {
        if (SynchronizationRule.FileSystemType == FileSystemTypes.Directory)
        {
            ElementType = _localizationService[nameof(Resources.General_Directory)];
        }
        else
        {
            ElementType = _localizationService[nameof(Resources.General_File)];
        }
    }
    
    internal void OnLocaleChanged()
    {
        BuildDescription();

        UpdateElementType();
    }

    internal void OnSynchronizationStarted()
    {
        HasSynchronizationStarted = true;
    }

    public void OnSessionResetted()
    {
        HasSynchronizationStarted = false;
    }
}