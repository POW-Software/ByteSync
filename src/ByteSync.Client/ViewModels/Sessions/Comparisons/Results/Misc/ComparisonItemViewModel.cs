using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using DynamicData;
using ReactiveUI;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

public class ComparisonItemViewModel : IDisposable
{
    private readonly ITargetedActionsService _targetedActionsService;
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly IContentIdentityViewModelFactory _contentIdentityViewModelFactory;
    private readonly IContentRepartitionViewModelFactory _contentRepartitionViewModelFactory;
    private readonly IItemSynchronizationStatusViewModelFactory _itemSynchronizationStatusViewModelFactory;
    private ReadOnlyObservableCollection<SynchronizationActionViewModel> _data;
    private readonly ISynchronizationActionViewModelFactory _synchronizationActionViewModelFactory;
    private readonly CompositeDisposable _compositeDisposable;
    private readonly IFormatKbSizeConverter _formatKbSizeConverter;
    
    public ComparisonItemViewModel(ITargetedActionsService targetedActionsService,
        IAtomicActionRepository atomicActionRepository, IContentIdentityViewModelFactory contentIdentityViewModelFactory,
        IContentRepartitionViewModelFactory contentRepartitionViewModelFactory,
        IItemSynchronizationStatusViewModelFactory itemSynchronizationStatusViewModelFactory, ComparisonItem comparisonItem,
        List<Inventory> inventories,
        ISynchronizationActionViewModelFactory synchronizationActionViewModelFactory, IFormatKbSizeConverter formatKbSizeConverter)
    {
        ComparisonItem = comparisonItem;
        Inventories = inventories;
        
        _targetedActionsService = targetedActionsService;
        _atomicActionRepository = atomicActionRepository;
        _contentIdentityViewModelFactory = contentIdentityViewModelFactory;
        _contentRepartitionViewModelFactory = contentRepartitionViewModelFactory;
        _itemSynchronizationStatusViewModelFactory = itemSynchronizationStatusViewModelFactory;
        _synchronizationActionViewModelFactory = synchronizationActionViewModelFactory;
        _formatKbSizeConverter = formatKbSizeConverter;
        
        ContentIdentitiesA = new List<ContentIdentityViewModel>();
        ContentIdentitiesB = new List<ContentIdentityViewModel>();
        ContentIdentitiesC = new List<ContentIdentityViewModel>();
        ContentIdentitiesD = new List<ContentIdentityViewModel>();
        ContentIdentitiesE = new List<ContentIdentityViewModel>();
        
        ContentIdentitiesList =
        [
            ContentIdentitiesA,
            ContentIdentitiesB,
            ContentIdentitiesC,
            ContentIdentitiesD,
            ContentIdentitiesE
        ];
        
        _compositeDisposable = new CompositeDisposable();
        
        _atomicActionRepository.ObservableCache.Connect()
            .Filter(sa => sa.PathIdentity != null && Equals(sa.PathIdentity, ComparisonItem.PathIdentity))
            .Transform(sa => _synchronizationActionViewModelFactory.CreateSynchronizationActionViewModel(sa, this))
            .DisposeMany() // dispose when no longer required
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _data)
            .Subscribe()
            .DisposeWith(_compositeDisposable);
        
        PathIdentity = comparisonItem.PathIdentity;
        
        BuildLinkingKeyNameTooltip();
        
        // Create all ContentIdentityViewModels
        foreach (var contentIdentity in ComparisonItem.ContentIdentities)
        {
            foreach (var inventory in contentIdentity.GetInventories())
            {
                var collection = GetContentIdentityViews(inventory);
                
                var contentIdentityView = _contentIdentityViewModelFactory.CreateContentIdentityViewModel(this, contentIdentity, inventory);
                
                collection.Add(contentIdentityView);
            }
        }
        
        // Sort each collection by inventory part codes (A1, A2, B1, B2, etc.)
        foreach (var contentIdentitiesCollection in ContentIdentitiesList)
        {
            contentIdentitiesCollection.Sort((a, b) =>
            {
                var aMinCode = a.ContentIdentity.GetInventoryParts()
                    .Where(ip => ip.Inventory.Equals(a.Inventory))
                    .Min(ip => ip.Code);
                var bMinCode = b.ContentIdentity.GetInventoryParts()
                    .Where(ip => ip.Inventory.Equals(b.Inventory))
                    .Min(ip => ip.Code);
                
                return string.Compare(aMinCode, bMinCode, StringComparison.Ordinal);
            });
        }
        
        ContentRepartitionViewModel = _contentRepartitionViewModelFactory.CreateContentRepartitionViewModel(ComparisonItem, inventories);
        _compositeDisposable.Add(ContentRepartitionViewModel);
        
        ItemSynchronizationStatusViewModel =
            _itemSynchronizationStatusViewModelFactory.CreateItemSynchronizationStatusViewModel(ComparisonItem, inventories);
        _compositeDisposable.Add(ItemSynchronizationStatusViewModel);
    }
    
    internal ComparisonItem ComparisonItem { get; }
    
    internal List<Inventory> Inventories { get; }
    
    internal PathIdentity PathIdentity { get; }
    
    public List<ContentIdentityViewModel> ContentIdentitiesA { get; }
    
    public List<ContentIdentityViewModel> ContentIdentitiesB { get; }
    
    public List<ContentIdentityViewModel> ContentIdentitiesC { get; }
    
    public List<ContentIdentityViewModel> ContentIdentitiesD { get; }
    
    public List<ContentIdentityViewModel> ContentIdentitiesE { get; }
    
    public List<List<ContentIdentityViewModel>> ContentIdentitiesList { get; set; }
    
    public ReadOnlyObservableCollection<SynchronizationActionViewModel> SynchronizationActions => _data;
    
    internal ContentRepartitionViewModel ContentRepartitionViewModel { get; private set; }
    
    internal ItemSynchronizationStatusViewModel ItemSynchronizationStatusViewModel { get; private set; }
    
    private List<string>? AtomicActionsIds { get; set; }
    
    internal string? LinkingKeyNameTooltip { get; set; }
    
    public FileSystemTypes FileSystemType
    {
        get { return PathIdentity.FileSystemType; }
    }
    
    public List<ContentIdentityViewModel> GetContentIdentityViews(Inventory inventory)
    {
        var index = Inventories.IndexOf(inventory);
        
        switch (index)
        {
            case 0:
                return ContentIdentitiesA;
            case 1:
                return ContentIdentitiesB;
            case 2:
                return ContentIdentitiesC;
            case 3:
                return ContentIdentitiesD;
            case 4:
                return ContentIdentitiesE;
            default:
                Log.Error("GetContentIdentityViews: can not identify ContentIdentities for index:{Index}, inventory:{Inventory}", index,
                    inventory.InventoryId);
                
                throw new ApplicationException($"GetContentIdentityViews: can not identify ContentIdentities, {index}:");
        }
    }
    
    public void ClearTargetedActions()
    {
        _targetedActionsService.ClearTargetedActions(this);
    }
    
    private void BuildLinkingKeyNameTooltip()
    {
        if (ComparisonItem.FileSystemType == FileSystemTypes.Directory)
        {
            return;
        }
        
        var linkingKeyNameTooltip = new StringBuilder();
        
        // Sort ContentIdentities by inventory parts codes (A1, A2, B1, B2, etc.)
        var sortedContentIdentities = ComparisonItem.ContentIdentities
            .OrderBy(ci => ci.GetInventoryParts().Min(ip => ip.Code))
            .ToList();
        
        var isFirst = true;
        foreach (var contentIdentity in sortedContentIdentities)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                linkingKeyNameTooltip.AppendLine();
            }
            
            linkingKeyNameTooltip.AppendLine("___________________________________________________________");
            
            // Handle virtual ContentIdentity (inaccessible files with null Core)
            if (contentIdentity.Core == null)
            {
                linkingKeyNameTooltip.AppendLine("Inaccessible");
                linkingKeyNameTooltip.AppendLine("‾‾‾‾‾‾‾‾‾‾‾‾");
            }
            else if (contentIdentity.Core.SignatureHash.IsNotEmpty())
            {
                linkingKeyNameTooltip.AppendLine(contentIdentity.Core.SignatureHash);
                
                linkingKeyNameTooltip.AppendLine(
                    "                                            ‾‾‾"); // Overline U+203E https://en.wikipedia.org/wiki/Overline
            }
            else
            {
                var size = _formatKbSizeConverter.Convert(contentIdentity.Core.Size);
                
                linkingKeyNameTooltip.AppendLine(size);
                
                linkingKeyNameTooltip.AppendLine("‾‾‾‾‾‾"); // Overline U+203E https://en.wikipedia.org/wiki/Overline
            }
            
            
            var inventoryParts = contentIdentity.GetInventoryParts().OrderBy(ip => ip.Code);
            foreach (var inventoryPart in inventoryParts)
            {
                var fileDescriptions = contentIdentity.GetFileSystemDescriptions(inventoryPart);
                
                if (fileDescriptions.Count > 0)
                {
                    linkingKeyNameTooltip.AppendLine(inventoryPart.Inventory.MachineName + " " + inventoryPart.Code + " (" +
                                                     inventoryPart.RootPath + ")");
                    
                    foreach (var fileDescription in fileDescriptions)
                    {
                        linkingKeyNameTooltip.AppendLine(" - " + fileDescription.RelativePath);
                    }
                    
                    linkingKeyNameTooltip.AppendLine();
                }
            }
        }
        
        linkingKeyNameTooltip.TrimEnd();
        
        LinkingKeyNameTooltip = linkingKeyNameTooltip.ToString();
    }
    
    public void Dispose()
    {
        _compositeDisposable.Dispose();
        
        AtomicActionsIds = null;
    }
    
    public void OnLocaleChanged(ILocalizationService localizationService)
    {
        foreach (var contentIdentityViewModel in ContentIdentitiesA)
        {
            contentIdentityViewModel.OnLocaleChanged();
        }
        
        foreach (var contentIdentityViewModel in ContentIdentitiesB)
        {
            contentIdentityViewModel.OnLocaleChanged();
        }
        
        foreach (var contentIdentityViewModel in ContentIdentitiesC)
        {
            contentIdentityViewModel.OnLocaleChanged();
        }
        
        foreach (var contentIdentityViewModel in ContentIdentitiesD)
        {
            contentIdentityViewModel.OnLocaleChanged();
        }
        
        foreach (var contentIdentityViewModel in ContentIdentitiesE)
        {
            contentIdentityViewModel.OnLocaleChanged();
        }
    }
}