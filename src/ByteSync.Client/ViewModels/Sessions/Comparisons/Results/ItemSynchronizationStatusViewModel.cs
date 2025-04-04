using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.Themes;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ItemSynchronizationStatusViewModel : ViewModelBase, IDisposable
{
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    
    private readonly CompositeDisposable _disposables = new();

    public ItemSynchronizationStatusViewModel()
    {

    }

    public ItemSynchronizationStatusViewModel(ComparisonItem comparisonItem, ISharedActionsGroupRepository sharedActionsGroupRepository)
    {
        ComparisonItem = comparisonItem;
        ItemSynchronizationStatus = comparisonItem.ItemSynchronizationStatus;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        
        InitializeStatuses();

        var sharedActionsGroups = _sharedActionsGroupRepository.ObservableCache.Connect()
            .Filter(sag => sag.PathIdentity.Equals(ItemSynchronizationStatus.PathIdentity))
            .AsObservableCache();
        
        var subscription = sharedActionsGroups.Connect()
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ =>
            {
                var allItems = sharedActionsGroups.Items.ToList();
                if (allItems.All(x => x.SynchronizationStatus == Business.Actions.Shared.SynchronizationStatus.Success))
                {
                    SetSynchronizationSuccess();
                }
                else if (allItems.All(x => x.SynchronizationStatus == Business.Actions.Shared.SynchronizationStatus.Error))
                {
                    SetSynchronizationError();
                }
                else
                {
                    if (ItemSynchronizationStatus.IsSuccessStatus || ItemSynchronizationStatus.IsErrorStatus)
                    {
                        ItemSynchronizationStatus.IsSuccessStatus = false;
                        ItemSynchronizationStatus.IsErrorStatus = false;
                        
                        SetUnfinishedStatus();
                    }
                }
            });
        
        _disposables.Add(subscription);
    }
    
    public ItemSynchronizationStatus ItemSynchronizationStatus { get; }
    
    public ComparisonItem ComparisonItem { get; }
    
    [Reactive]
    public bool ShowSyncSuccessStatus { get; set; }
    
    [Reactive]
    public bool ShowSyncErrorStatus { get; set; }
    
    public void SetSynchronizationSuccess()
    {
        ItemSynchronizationStatus.IsSuccessStatus = true;
        ShowSyncSuccessStatus = true;
    }
    
    public void SetSynchronizationError()
    {
        ItemSynchronizationStatus.IsErrorStatus = true;
        ShowSyncErrorStatus = true;
    }
    
    private void InitializeStatuses()
    {
        ItemSynchronizationStatus.IsSuccessStatus = ItemSynchronizationStatus.IsSuccessStatus;
        ItemSynchronizationStatus.IsErrorStatus =  ItemSynchronizationStatus.IsErrorStatus;
    }
    
    private void SetUnfinishedStatus()
    {
        ItemSynchronizationStatus.IsSuccessStatus = false;
        ShowSyncSuccessStatus = false;
        
        ItemSynchronizationStatus.IsErrorStatus =  ItemSynchronizationStatus.IsErrorStatus;
        ShowSyncErrorStatus = false;
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}