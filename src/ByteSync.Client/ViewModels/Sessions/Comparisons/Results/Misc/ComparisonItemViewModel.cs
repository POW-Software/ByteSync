using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using DynamicData;
using ReactiveUI;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

public class ComparisonItemViewModel : IDisposable
{
    private readonly ITargetedActionsManager _targetedActionsManager;
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly IContentIdentityViewModelFactory _contentIdentityViewModelFactory;
    private readonly IStatusViewModelFactory _statusViewModelFactory;
    private ReadOnlyObservableCollection<SynchronizationActionViewModel> _data;
    private readonly ISynchronizationActionViewModelFactory _synchronizationActionViewModelFactory;
    private readonly CompositeDisposable _compositeDisposable;
    private readonly IFormatKbSizeConverter _formatKbSizeConverter;

    public ComparisonItemViewModel(ITargetedActionsManager targetedActionsManager,
        IAtomicActionRepository atomicActionRepository, IContentIdentityViewModelFactory contentIdentityViewModelFactory,
        IStatusViewModelFactory statusViewModelFactory, ComparisonItem comparisonItem, List<Inventory> inventories,
        ISynchronizationActionViewModelFactory synchronizationActionViewModelFactory, IFormatKbSizeConverter formatKbSizeConverter)
    {
        ComparisonItem = comparisonItem;
        Inventories = inventories;
        
        _targetedActionsManager = targetedActionsManager;
        _atomicActionRepository = atomicActionRepository;
        _contentIdentityViewModelFactory = contentIdentityViewModelFactory;
        _statusViewModelFactory = statusViewModelFactory;
        _synchronizationActionViewModelFactory = synchronizationActionViewModelFactory;
        _formatKbSizeConverter = formatKbSizeConverter;

        ContentIdentitiesA = new HashSet<ContentIdentityViewModel>();
        ContentIdentitiesB = new HashSet<ContentIdentityViewModel>();
        ContentIdentitiesC = new HashSet<ContentIdentityViewModel>();
        ContentIdentitiesD = new HashSet<ContentIdentityViewModel>();
        ContentIdentitiesE = new HashSet<ContentIdentityViewModel>();

        ContentIdentitiesList = new List<HashSet<ContentIdentityViewModel>>();
        ContentIdentitiesList.Add(ContentIdentitiesA);
        ContentIdentitiesList.Add(ContentIdentitiesB);
        ContentIdentitiesList.Add(ContentIdentitiesC);
        ContentIdentitiesList.Add(ContentIdentitiesD);
        ContentIdentitiesList.Add(ContentIdentitiesE);
        
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
        
        foreach (var contentIdentity in ComparisonItem.ContentIdentities)
        {
            foreach (var inventory in contentIdentity.GetInventories())
            {
                var collection = GetContentIdentityViews(inventory);

                var contentIdentityView = _contentIdentityViewModelFactory.CreateContentIdentityViewModel(this, contentIdentity, inventory);
                
                collection.Add(contentIdentityView);

            }
        }

        StatusViewModel = _statusViewModelFactory.CreateStatusViewModel(ComparisonItem, inventories);
        _compositeDisposable.Add(StatusViewModel);
    }

    internal ComparisonItem ComparisonItem { get; }

    internal List<Inventory> Inventories { get; }

    internal PathIdentity PathIdentity { get; }

    internal HashSet<ContentIdentityViewModel> ContentIdentitiesA { get; }

    internal HashSet<ContentIdentityViewModel> ContentIdentitiesB { get; }

    internal HashSet<ContentIdentityViewModel> ContentIdentitiesC { get; }

    internal HashSet<ContentIdentityViewModel> ContentIdentitiesD { get; }

    internal HashSet<ContentIdentityViewModel> ContentIdentitiesE { get; }

    internal List<HashSet<ContentIdentityViewModel>> ContentIdentitiesList { get; set; }
    
    public ReadOnlyObservableCollection<SynchronizationActionViewModel> SynchronizationActions => _data;

    internal StatusViewModel StatusViewModel { get; private set; }
    
    private List<string>? AtomicActionsIds { get; set; }
    
    internal string? LinkingKeyNameTooltip { get; set; }

    public FileSystemTypes FileSystemType
    {
        get
        {
            return PathIdentity.FileSystemType;
        }
    }

    internal HashSet<ContentIdentityViewModel> GetContentIdentityViews(Inventory inventory)
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
                Log.Error("GetContentIdentityViews: can not identify ContentIdentities for index:{Index}, inventory:{Inventory}", index, inventory.InventoryId);
                throw new ApplicationException($"GetContentIdentityViews: can not identify ContentIdentities, {index}:");
        }
    }

    internal HashSet<ContentIdentityViewModel> GetContentIdentityViews(InventoryPart inventoryPart)
    {
        var contentIdentityViewModelsByInventory =
            GetContentIdentityViews(inventoryPart.Inventory);

        var result = Enumerable.ToHashSet(contentIdentityViewModelsByInventory
                .Where(civm => civm.ContentIdentity.IsPresentIn(inventoryPart)).ToList());

        return result;
    }

    public void ClearTargetedActions()
    {
        _targetedActionsManager.ClearTargetedActions(this);
    }
    
    private void BuildLinkingKeyNameTooltip()
    {
        if (ComparisonItem.FileSystemType == FileSystemTypes.Directory)
        {
            return;
        }
        
        var linkingKeyNameTooltip = new StringBuilder();

        var isFirst = true;
        foreach (var contentIdentity in ComparisonItem.ContentIdentities)
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
            if (contentIdentity.Core!.SignatureHash.IsNotEmpty())
            {
                linkingKeyNameTooltip.AppendLine(contentIdentity.Core.SignatureHash); 
                
                linkingKeyNameTooltip.AppendLine("                                            ‾‾‾"); // Overline U+203E https://en.wikipedia.org/wiki/Overline
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
                    linkingKeyNameTooltip.AppendLine(inventoryPart.Inventory.MachineName + " " + inventoryPart.Code + " (" + inventoryPart.RootPath + ")");

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

    public void OnThemeChanged()
    {
        StatusViewModel.OnThemeChanged();
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