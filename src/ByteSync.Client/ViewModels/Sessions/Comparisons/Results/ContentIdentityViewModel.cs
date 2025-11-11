using System.Collections.ObjectModel;
using ByteSync.Assets.Resources;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ContentIdentityViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IDateAndInventoryPartsViewModelFactory _dateAndInventoryPartsViewModelFactory;
    
    public ContentIdentityViewModel()
    {
    #if DEBUG
        var contentIdentityCore = new ContentIdentityCore();
        contentIdentityCore.SignatureHash = "SignatureHash";
        contentIdentityCore.Size = DateTime.Now.Millisecond;
        ContentIdentity = new ContentIdentity(contentIdentityCore);
        
        DateAndInventoryParts = new ObservableCollection<DateAndInventoryPartsViewModel>();
        
        FillDateAndInventoryParts();
        FillStringData();
        SetHashOrWarnIcon();
    #endif
    }
    
    public ContentIdentityViewModel(ComparisonItemViewModel comparisonItemViewModel,
        ContentIdentity contentIdentity, Inventory inventory, ISessionService sessionService,
        ILocalizationService localizationService,
        IDateAndInventoryPartsViewModelFactory dateAndInventoryPartsViewModelFactory)
    {
        ComparisonItemViewModel = comparisonItemViewModel;
        ContentIdentity = contentIdentity;
        Inventory = inventory;
        
        IsFile = ComparisonItemViewModel.FileSystemType == FileSystemTypes.File;
        IsDirectory = !IsFile;
        
        _sessionService = sessionService;
        _localizationService = localizationService;
        _dateAndInventoryPartsViewModelFactory = dateAndInventoryPartsViewModelFactory;
        
        DateAndInventoryParts = new ObservableCollection<DateAndInventoryPartsViewModel>();
        
        FillDateAndInventoryParts();
        FillStringData();
        SetHashOrWarnIcon();
        
        ShowInventoryParts = _sessionService.IsCloudSession;
        
        HasAnalysisError = ContentIdentity.HasAnalysisError;
        HasAccessIssue = ContentIdentity.HasAccessIssueFor(Inventory) || ContentIdentity.HasAccessIssue;
        if (HasAnalysisError)
        {
            ShowToolTipDelay = 400;
        }
        else if (HasAccessIssue)
        {
            ShowToolTipDelay = 400;
        }
        else if (LinkingKeyNameTooltip.IsNotEmpty())
        {
            ShowToolTipDelay = 400;
        }
        else
        {
            ShowToolTipDelay = int.MaxValue;
        }
    }
    
    public ComparisonItemViewModel ComparisonItemViewModel { get; }
    
    public ContentIdentity ContentIdentity { get; }
    
    public Inventory Inventory { get; }
    
    [Reactive]
    public string? SignatureHash { get; set; }
    
    [Reactive]
    public string? HashOrWarnIcon { get; set; }
    
    [Reactive]
    public string? ErrorType { get; set; }
    
    [Reactive]
    public string? ErrorDescription { get; set; }
    
    [Reactive]
    public bool HasAnalysisError { get; set; }
    
    [Reactive]
    public bool HasAccessIssue { get; set; }
    
    [Reactive]
    public int ShowToolTipDelay { get; set; }
    
    [Reactive]
    public bool ShowInventoryParts { get; set; }
    
    [Reactive]
    public bool IsFile { get; set; }
    
    [Reactive]
    public bool IsDirectory { get; set; }
    
    [Reactive]
    public string PresenceParts { get; set; }
    
    public ObservableCollection<DateAndInventoryPartsViewModel> DateAndInventoryParts { get; private set; }
    
    public long? Size
    {
        get { return ContentIdentity.Core?.Size; }
    }
    
    [Reactive]
    public string? LinkingKeyNameTooltip { get; set; }
    
    private void FillStringData()
    {
        if (IsFile)
        {
            if (ContentIdentity.HasAnalysisError)
            {
                var onErrorFileDescription = ContentIdentity.FileSystemDescriptions
                        .First(fsd => fsd is FileDescription { HasAnalysisError: true })
                    as FileDescription;
                
                SignatureHash = onErrorFileDescription!.AnalysisErrorType!.Truncate(32);
                ErrorType = onErrorFileDescription.AnalysisErrorType;
                ErrorDescription = onErrorFileDescription.AnalysisErrorDescription;
            }
            else
            {
                if (ContentIdentity.HasAccessIssueFor(Inventory))
                {
                    SignatureHash = _localizationService[nameof(Resources.ContentIdentity_AccessIssueShortLabel)];
                }
                else if (ContentIdentity.Core == null || ContentIdentity.Core.SignatureHash.IsNullOrEmpty())
                {
                    SignatureHash = "";
                }
                else
                {
                    if (ContentIdentity.Core.SignatureHash!.Length > 32)
                    {
                        SignatureHash = ContentIdentity.Core.SignatureHash.Substring(0, 8) + "...";
                        
                        var lastIndexOfSlash = ContentIdentity.Core.SignatureHash.LastIndexOf("/", StringComparison.Ordinal);
                        if (lastIndexOfSlash == -1 || lastIndexOfSlash <= 56)
                        {
                            SignatureHash += ContentIdentity.Core.SignatureHash.Substring(56);
                        }
                        else
                        {
                            SignatureHash += ContentIdentity.Core.SignatureHash.Substring(56, lastIndexOfSlash - 56);
                        }
                    }
                    else
                    {
                        SignatureHash = ContentIdentity.Core.SignatureHash;
                    }
                }
            }
        }
        
        if (_sessionService.CurrentSessionSettings!.MatchingMode == MatchingModes.Flat
            || ContentIdentity.HasManyFileSystemDescriptionOnAnInventoryPart)
        {
            LinkingKeyNameTooltip = ComparisonItemViewModel.LinkingKeyNameTooltip;
        }
        
        if (IsDirectory)
        {
            PresenceParts = ContentIdentity.GetInventoryParts()
                .Where(ip => ip.Inventory.Equals(Inventory))
                .OrderBy(ip => ip.Code)
                .Select(ip => ip.Code)
                .ToList()
                .JoinToString(", ");
        }
    }
    
    private void SetHashOrWarnIcon()
    {
        if (ContentIdentity.HasAnalysisError || ContentIdentity.HasAccessIssueFor(Inventory))
        {
            HashOrWarnIcon = "RegularError";
        }
        else
        {
            HashOrWarnIcon = "RegularHash";
        }
    }
    
    private void FillDateAndInventoryParts()
    {
        DateAndInventoryParts.Clear();
        
        foreach (var pair in ContentIdentity.InventoryPartsByLastWriteTimes)
        {
            var inventoryPartsOK =
                Enumerable.ToHashSet(pair.Value.Where(ip => ip.Inventory.Equals(Inventory)).ToList());
            
            if (inventoryPartsOK.Count > 0)
            {
                var dateAndInventoryPartsViewModel =
                    _dateAndInventoryPartsViewModelFactory.CreateDateAndInventoryPartsViewModel(this, pair.Key.ToLocalTime(),
                        inventoryPartsOK);
                
                DateAndInventoryParts.Add(dateAndInventoryPartsViewModel);
            }
        }
    }
    
    internal void OnLocaleChanged()
    {
        this.RaisePropertyChanged(nameof(Size));
    }
}