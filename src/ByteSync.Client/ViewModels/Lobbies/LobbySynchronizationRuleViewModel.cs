using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Loose;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Factories;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Lobbies;

public class LobbySynchronizationRuleViewModel : ActivatableViewModelBase
{
    private readonly ILocalizationService _localizationService;
    private readonly IDescriptionBuilderFactory _descriptionBuilderFactory;

    private const string ICON_FILE = "RegularFile";
    private const string ICON_FOLDER = "RegularFolder";

    // public LobbySynchronizationRuleViewModel(LooseSynchronizationRule looseSynchronizationRule,
    //     CloudSessionProfileDetails cloudSessionProfileDetails, ILocalizationService localizationService) 
    //     : this(looseSynchronizationRule, cloudSessionProfileDetails.Options.Settings.DataType == DataTypes.FilesDirectories,
    //         localizationService)
    // {
    //     // SynchronizationRule = looseSynchronizationRule;
    //     //
    //     // _localizationService = localizationService;
    //     //
    //     // IsIconVisible = cloudSessionProfileDetails.Options.Settings!.DataType == DataTypes.FilesDirectories;
    //     // IconName = SynchronizationRule.FileSystemType == FileSystemTypes.File
    //     //     ? ICON_FILE
    //     //     : ICON_FOLDER;
    //     //
    //     // BuildDescription();
    //     // UpdateElementType();
    // }
    
    public LobbySynchronizationRuleViewModel(LooseSynchronizationRule looseSynchronizationRule,
        bool isIconVisible, ILocalizationService localizationService, IDescriptionBuilderFactory descriptionBuilderFactory)
    {
        SynchronizationRule = looseSynchronizationRule;
        
        _localizationService = localizationService;
        _descriptionBuilderFactory = descriptionBuilderFactory;   
        
        IsIconVisible = isIconVisible;
        IconName = SynchronizationRule.FileSystemType == FileSystemTypes.File
            ? ICON_FILE
            : ICON_FOLDER;

        BuildDescription();
        UpdateElementType();
    }
    
    public LooseSynchronizationRule SynchronizationRule { get; }
    
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
    public string ElementType { get; set; }
    
    private void BuildDescription()
    {
        var synchronizationRuleDescriptionBuilder = _descriptionBuilderFactory.CreateSynchronizationRuleDescriptionBuilder(SynchronizationRule);
        synchronizationRuleDescriptionBuilder.BuildDescription(Environment.NewLine);
        
        // string mode;
        // if (SynchronizationRule.Conditions.Count == 1)
        // {
        //     mode = Resources.SynchronizationRuleSummary_If;
        // }
        // else
        // {
        //     if (SynchronizationRule.ConditionMode == ConditionModes.All)
        //     {
        //         mode = Resources.SynchronizationRuleSummary_IfAll;
        //     }
        //     else
        //     {
        //         mode = Resources.SynchronizationRuleSummary_IfAny;
        //     }
        // }
        //     
        //
        // StringBuilder sbConditions = new StringBuilder();
        // var atomicConditionDescriptionBuilder = new AtomicConditionDescriptionBuilder(_localizationService);
        // foreach (var atomicCondition in SynchronizationRule.Conditions)
        // {
        //     atomicConditionDescriptionBuilder.AppendDescription(sbConditions, atomicCondition);
        //     sbConditions.AppendLine();
        // }
        // sbConditions.TrimEnd();
        //
        // string then = _localizationService[nameof(Resources.SynchronizationRuleSummary_Then)];
        //
        // StringBuilder sbActions  = new StringBuilder();
        // var synchronizationActionDescriptionBuilder = new AtomicActionDescriptionBuilder(_localizationService);
        // foreach (var atomicAction in SynchronizationRule.Actions)
        // {
        //     synchronizationActionDescriptionBuilder.AppendDescription(sbActions, atomicAction);
        //     sbActions.AppendLine();
        // }
        // sbActions.TrimEnd();

        Mode = synchronizationRuleDescriptionBuilder.Mode!;
        Conditions = synchronizationRuleDescriptionBuilder.Conditions!;
        Then = synchronizationRuleDescriptionBuilder.Then!;
        Actions = synchronizationRuleDescriptionBuilder.Actions!;
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

    public void OnLocaleChanged()
    {
        UpdateElementType();
    }
}