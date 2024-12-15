﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions;

public class SynchronizationRuleGlobalViewModel : FlyoutElementViewModel
{
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IActionEditViewModelFactory _actionEditViewModelFactory;
    private readonly ISynchronizationRulesService _synchronizationRulesService;

    public SynchronizationRuleGlobalViewModel() 
    {
    }
    
    public SynchronizationRuleGlobalViewModel(INavigationEventsHub navigationEventsHub, 
        ISessionService sessionService, ILocalizationService localizationService, IActionEditViewModelFactory actionEditViewModelFactory, 
        ISynchronizationRulesService synchronizationRulesService, SynchronizationRule? baseAutomaticAction, bool isCloneMode)
    {
        _navigationEventsHub = navigationEventsHub;
        _sessionService = sessionService;
        _localizationService = localizationService;
        _actionEditViewModelFactory = actionEditViewModelFactory;
        _synchronizationRulesService = synchronizationRulesService;

        ShowFileSystemTypeSelection = _sessionService.CurrentSessionSettings!.DataType == DataTypes.FilesDirectories;

        Conditions = new ObservableCollection<AtomicConditionEditViewModel>();
        Actions = new ObservableCollection<AtomicActionEditViewModel>();
            
        AddConditionCommand = ReactiveCommand.Create(AddCondition);
        AddActionCommand = ReactiveCommand.Create(AddAction);
        SaveCommand = ReactiveCommand.Create(Save);
        ResetCommand = ReactiveCommand.Create(Reset);
        CancelCommand = ReactiveCommand.Create(Cancel);

        FileSystemTypes = BuildFileSystemTypes();
        SelectedFileSystemType =
            _sessionService.CurrentSessionSettings!.DataType == DataTypes.Directories
                ? FileSystemTypes.Single(fst => fst.IsDirectory)
                : FileSystemTypes.Single(fst => fst.IsFile);
        
        ConditionModes = BuildConditionModes();
        SelectedConditionMode = ConditionModes.Single(cm => cm.IsAll);

        BaseAutomaticAction = baseAutomaticAction;
        IsCloneMode = isCloneMode;
        // ComparisonResult = _sessionDataHolder.ComparisonResult; 

        Reset();

        this.WhenAnyValue(x => x.SelectedFileSystemType)
            .Skip(1)
            .Subscribe(_ => Reset());

        this.WhenAnyValue(x => x.SelectedConditionMode)
            .Subscribe(x =>
            {
                if (x.IsAny)
                {
                    TextAfterConditionModesComboBox = Resources.SynchronizationRulesGlobal_TextAfterAny;
                }
                else
                {
                    TextAfterConditionModesComboBox = Resources.SynchronizationRulesGlobal_TextAfterAll;
                }
            });
        
        this.WhenActivated(disposables =>
        {
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
                .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<AtomicActionEditViewModel>>(_actionEditionEventsHub, nameof(_actionEditionEventsHub.AtomicActionRemoved))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnAtomicActionRemoved(evt.EventArgs.Value))
            //     .DisposeWith(disposables);
            //
            // Observable.FromEventPattern<GenericEventArgs<AtomicConditionEditViewModel>>(_actionEditionEventsHub, nameof(_actionEditionEventsHub.AtomicConditionRemoved))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnAtomicConditionRemoved(evt.EventArgs.Value))
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<AtomicActionEditViewModel>>(_actionEditionEventsHub, 
            //         nameof(_actionEditionEventsHub.AtomicActionInputChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnAtomicInputChanged())
            //     .DisposeWith(disposables);
            //
            // Observable.FromEventPattern<GenericEventArgs<AtomicConditionEditViewModel>>(_actionEditionEventsHub, 
            //         nameof(_actionEditionEventsHub.AtomicConditionInputChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnAtomicInputChanged())
            //     .DisposeWith(disposables);
        });
        
        // TranslationSource.GetInstance().PropertyChanged += TranslationSource_PropertyChanged;
    }

    public ReactiveCommand<Unit, Unit> AddConditionCommand { get; set; }
    public ReactiveCommand<Unit, Unit> AddActionCommand { get; set; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; set; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    public ObservableCollection<FileSystemTypeViewModel> FileSystemTypes { get; set; }
        
    public ObservableCollection<ConditionModeViewModel> ConditionModes { get; set; }
        
    public ObservableCollection<AtomicConditionEditViewModel> Conditions { get; }
        
    public ObservableCollection<AtomicActionEditViewModel> Actions { get; }

    [Reactive]
    public FileSystemTypeViewModel SelectedFileSystemType { get; set; }
    
    [Reactive]
    public ConditionModeViewModel SelectedConditionMode { get; set; }

    [Reactive]
    public string TextAfterConditionModesComboBox { get; set; }
    
    [Reactive]
    public bool ShowFileSystemTypeSelection { get; set; }
    
    [Reactive]
    public bool ShowWarning { get; set; }
    
    [Reactive]
    public string SaveWarning { get; set; }

    // private ComparisonResult ComparisonResult { get; }

    public SynchronizationRule? BaseAutomaticAction { get; }
    
    
    public bool IsCloneMode { get; }
    
    private ObservableCollection<FileSystemTypeViewModel> BuildFileSystemTypes()
    {
        var fileSystemTypes = new ObservableCollection<FileSystemTypeViewModel>();

        var file = new FileSystemTypeViewModel { 
            FileSystemType = Common.Business.Inventories.FileSystemTypes.File, 
            Description = Resources.General_Files};
        
        var directory = new FileSystemTypeViewModel { 
            FileSystemType = Common.Business.Inventories.FileSystemTypes.Directory, 
            Description = Resources.General_Directories};
        
        fileSystemTypes.Add(file);
        fileSystemTypes.Add(directory);

        return fileSystemTypes;
    }
    
    private ObservableCollection<ConditionModeViewModel> BuildConditionModes()
    {
        var conditionModes = new ObservableCollection<ConditionModeViewModel>();

        var any = new ConditionModeViewModel
        {
            Mode = ByteSync.Business.Comparisons.ConditionModes.Any, 
            Description = Resources.SynchronizationRulesGlobal_Any
        };

        var all = new ConditionModeViewModel
        {
            Mode = ByteSync.Business.Comparisons.ConditionModes.All, 
            Description = Resources.SynchronizationRulesGlobal_All
        };

        conditionModes.Add(any);
        conditionModes.Add(all);

        return conditionModes;
    }
        
    private void AddCondition()
    {
        var atomicConditionEditViewModel = _actionEditViewModelFactory.BuildAtomicConditionEditViewModel(
            SelectedFileSystemType.FileSystemType);
        
        atomicConditionEditViewModel.PropertyChanged += OnConditionOrActionPropertyChanged;
        atomicConditionEditViewModel.RemoveRequested += OnConditionRemoveRequested;
        
        Conditions.Add(atomicConditionEditViewModel);

        //region.Activate();

        //_regionManager.RequestNavigate(RegionNames.SynchronizationRulesConditionsRegion, nameof(SynchronizationRuleEditView));
    }

    private void AddAction()
    {
        var atomicActionEditViewModel = _actionEditViewModelFactory.BuildAtomicActionEditViewModel(
            SelectedFileSystemType.FileSystemType, true);

        atomicActionEditViewModel.PropertyChanged += OnConditionOrActionPropertyChanged;
        atomicActionEditViewModel.RemoveRequested += OnActionRemoveRequested;
        
        Actions.Add(atomicActionEditViewModel);

        //region.Activate();

        //_regionManager.RequestNavigate(RegionNames.SynchronizationRulesConditionsRegion, nameof(SynchronizationRuleEditView));
    }

    private void Save()
    {
        try
        {
            var synchronizationRule = Export();

            if (synchronizationRule != null)
            {
                ShowWarning = false;
                
                _synchronizationRulesService.AddSynchronizationRule(synchronizationRule);

                // _actionEditionEventsHub.RaiseSynchronizationRuleSaved(synchronizationRule);

                _navigationEventsHub.RaiseCloseFlyoutRequested();
            }
            else
            {
                ShowMissingFieldsWarning();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during saving");
        }
    }

    private void ShowMissingFieldsWarning()
    {
        ShowWarning = true;
        SaveWarning = Resources.TargetedActionEditionGlobal_MissingFields;
    }

    private SynchronizationRule? Export()
    {
        var synchronizationRule = new SynchronizationRule(SelectedFileSystemType.FileSystemType, SelectedConditionMode.Mode);
        if (BaseAutomaticAction != null && ! IsCloneMode)
        {
            synchronizationRule.SynchronizationRuleId = BaseAutomaticAction.SynchronizationRuleId;
        }

        var isMissingField = false;
        
        // Conditions
        foreach (var atomicConditionEditViewModel in Conditions)
        {
            var atomicCondition = atomicConditionEditViewModel.ExportAtomicCondition();
            if (atomicCondition != null)
            {
                synchronizationRule.Conditions.Add(atomicCondition);
            }
            else
            {
                isMissingField = true;
            }
        }

        // Actions
        foreach (var automaticActionsActionEditViewModel in Actions)
        {
            var atomicAction = automaticActionsActionEditViewModel.ExportSynchronizationAction();

            if (atomicAction != null)
            {
                synchronizationRule.AddAction(atomicAction);
            }
            else
            {
                isMissingField = true;
            }
        }

        if (isMissingField)
        {
            return null;
        }
        else
        {
            return synchronizationRule;
        }
    }

    private void Reset()
    {
        ShowWarning = false;
        
        if (BaseAutomaticAction == null)
        {
            ResetToCreation();
        }
        else
        {
            ResetToEdition();
        }
    }

    private void ResetToCreation()
    {
        Conditions.Clear();
        Actions.Clear();


        AddCondition();
        AddAction();
    }


    private void ResetToEdition()
    {
        Conditions.Clear();
        Actions.Clear();

        SelectedFileSystemType = FileSystemTypes.FirstOrDefault(fst => Equals(fst.FileSystemType, BaseAutomaticAction!.FileSystemType));
        SelectedConditionMode = ConditionModes.FirstOrDefault(cm => Equals(cm.Mode, BaseAutomaticAction!.ConditionMode));

        foreach (var condition in BaseAutomaticAction!.Conditions)
        {
            var atomicConditionEditViewModel = _actionEditViewModelFactory.BuildAtomicConditionEditViewModel(SelectedFileSystemType.FileSystemType);
            atomicConditionEditViewModel.SetAtomicCondition(condition);
            Conditions.Add(atomicConditionEditViewModel);
        }

        foreach (var action in BaseAutomaticAction.Actions)
        {
            var automaticActionsActionEditViewModel = _actionEditViewModelFactory.BuildAtomicActionEditViewModel(
                SelectedFileSystemType.FileSystemType, true);
            
            automaticActionsActionEditViewModel.SetSynchronizationAction(action);
            Actions.Add(automaticActionsActionEditViewModel);
        }
    }

    // private void OnAtomicActionRemoved(AtomicActionEditViewModel atomicActionEditViewModel)
    // {
    //     Actions.Remove(atomicActionEditViewModel);
    // }
    //
    // private void OnAtomicConditionRemoved(AtomicConditionEditViewModel atomicConditionEditViewModel)
    // {
    //     Conditions.Remove(atomicConditionEditViewModel);
    // }
    
    // private void OnAtomicInputChanged()
    // {
    //     ShowWarning = false;
    // }
    //
    private void OnConditionOrActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ShowWarning = false;
    }
    
    private void OnActionRemoveRequested(object? sender, BaseAtomicEditViewModel e)
    {
        var atomicActionEditViewModel = (e as AtomicActionEditViewModel)!;
        
        atomicActionEditViewModel.PropertyChanged -= OnConditionOrActionPropertyChanged;
        atomicActionEditViewModel.RemoveRequested -= OnActionRemoveRequested;
        
        Actions.Remove(atomicActionEditViewModel);
    }
    
    private void OnConditionRemoveRequested(object? sender, BaseAtomicEditViewModel e)
    {
        var atomicConditionEditViewModel = (e as AtomicConditionEditViewModel)!;
        
        atomicConditionEditViewModel.PropertyChanged -= OnConditionOrActionPropertyChanged;
        atomicConditionEditViewModel.RemoveRequested -= OnConditionRemoveRequested;
        
        Conditions.Remove(atomicConditionEditViewModel);
    }

    private void Cancel()
    {
        _navigationEventsHub.RaiseCloseFlyoutRequested();

        Reset();
    }
    
    private void OnLocaleChanged()
    {
        ConditionModes.Single(cm => cm.IsAny).Description = Resources.SynchronizationRulesGlobal_Any;

        ConditionModes.Single(cm => cm.IsAll).Description = Resources.SynchronizationRulesGlobal_All;
    }
}