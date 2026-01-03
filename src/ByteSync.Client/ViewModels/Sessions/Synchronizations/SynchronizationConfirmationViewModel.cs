using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Actions;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Synchronizations;

public class SynchronizationConfirmationViewModel : FlyoutElementViewModel
{
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IFormatKbSizeConverter _formatKbSizeConverter;
    private readonly ILocalizationService _localizationService;
    
    private bool? _result;
    private readonly object _syncRoot = new();
    private readonly ManualResetEvent _resultSelected = new(false);
    
    public SynchronizationConfirmationViewModel()
    {
        _dataNodeRepository = null!;
        _sessionMemberRepository = null!;
        _formatKbSizeConverter = null!;
        _localizationService = null!;
        
        DestinationSummaryViewModels = new ObservableCollection<DestinationSummaryViewModel>();
    }
    
    public SynchronizationConfirmationViewModel(
        List<SharedAtomicAction> actions,
        IDataNodeRepository dataNodeRepository,
        ISessionMemberRepository sessionMemberRepository,
        IFormatKbSizeConverter formatKbSizeConverter,
        ILocalizationService localizationService)
    {
        _dataNodeRepository = dataNodeRepository;
        _sessionMemberRepository = sessionMemberRepository;
        _formatKbSizeConverter = formatKbSizeConverter;
        _localizationService = localizationService;
        
        DestinationSummaryViewModels = new ObservableCollection<DestinationSummaryViewModel>();
        
        ConfirmCommand = ReactiveCommand.Create(OnConfirm);
        CancelCommand = ReactiveCommand.Create(OnCancel);
        
        ComputeStatistics(actions);
    }
    
    [Reactive]
    public int TotalActionsCount { get; set; }
    
    [Reactive]
    public string TotalDataSize { get; set; } = string.Empty;
    
    [Reactive]
    public string TotalActionsText { get; set; } = string.Empty;
    
    [Reactive]
    public string TotalDataSizeText { get; set; } = string.Empty;
    
    public ObservableCollection<DestinationSummaryViewModel> DestinationSummaryViewModels { get; }
    
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; set; } = null!;
    
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; } = null!;
    
    private void ComputeStatistics(List<SharedAtomicAction> actions)
    {
        TotalActionsCount = actions.Count;
        TotalActionsText = string.Format(
            GetPluralizedKey("SynchronizationConfirmation_TotalActions", TotalActionsCount),
            TotalActionsCount);
        
        var totalBytes = actions.Where(a => a.Size.HasValue).Sum(a => a.Size!.Value);
        var deltaFilesCount = actions.Count(a => a.SynchronizationType == SynchronizationTypes.Delta);
        
        TotalDataSize = _formatKbSizeConverter.Convert(totalBytes);
        
        if (deltaFilesCount > 0)
        {
            TotalDataSizeText = string.Format(
                GetPluralizedKey("SynchronizationConfirmation_TotalSizeWithDelta", deltaFilesCount),
                TotalDataSize,
                deltaFilesCount);
        }
        else
        {
            TotalDataSizeText = string.Format(
                _localizationService[nameof(Resources.SynchronizationConfirmation_TotalSize)],
                TotalDataSize);
        }
        
        var actionsByDestination = actions
            .Where(a => a.Target != null)
            .GroupBy(a => (a.Target!.ClientInstanceId, a.Target.NodeId));
        
        foreach (var group in actionsByDestination)
        {
            var dataNodes = _dataNodeRepository.GetDataNodesByClientInstanceId(group.Key.ClientInstanceId);
            var dataNode = dataNodes.FirstOrDefault(dn => dn.Id == group.Key.NodeId);
            var sessionMember = _sessionMemberRepository.Elements
                .FirstOrDefault(sm => sm.ClientInstanceId == group.Key.ClientInstanceId);
            
            var summary = new DestinationActionsSummary
            {
                DestinationCode = dataNode?.Code ?? "?",
                MachineName = sessionMember?.MachineName ?? "Unknown",
                CreateCount = group.Count(a => a.IsCreate),
                SynchronizeContentCount = group.Count(a => a.IsCopyContent),
                SynchronizeDateCount = group.Count(a => a.IsCopyDate),
                DeleteCount = group.Count(a => a.IsDelete)
            };
            
            var summaryViewModel = new DestinationSummaryViewModel(summary, _localizationService);
            DestinationSummaryViewModels.Add(summaryViewModel);
        }
    }
    
    private void OnConfirm()
    {
        lock (_syncRoot)
        {
            _result ??= true;
        }
        
        _resultSelected.Set();
        RaiseCloseFlyoutRequested();
    }
    
    private void OnCancel()
    {
        lock (_syncRoot)
        {
            _result ??= false;
        }
        
        _resultSelected.Set();
        RaiseCloseFlyoutRequested();
    }
    
    public virtual async Task<bool> WaitForResponse()
    {
        return await Task.Run(() =>
        {
            _resultSelected.WaitOne();
            
            lock (_syncRoot)
            {
                return _result ?? false;
            }
        });
    }
    
    public override async Task CancelIfNeeded()
    {
        await Task.Run(() =>
        {
            lock (_syncRoot)
            {
                _result ??= false;
            }
            
            _resultSelected.Set();
        });
    }
    
    private string GetPluralizedKey(string baseKey, int count)
    {
        var suffix = count <= 1 ? "_One" : "_Many";
        
        return _localizationService[baseKey + suffix];
    }
}

public class DestinationSummaryViewModel
{
    private readonly DestinationActionsSummary _summary;
    private readonly ILocalizationService _localizationService;
    
    public DestinationSummaryViewModel(DestinationActionsSummary summary, ILocalizationService localizationService)
    {
        _summary = summary;
        _localizationService = localizationService;
    }
    
    public string HeaderText => string.Format(
        _localizationService[nameof(Resources.SynchronizationConfirmation_ToDestination)],
        _summary.DestinationCode,
        _summary.MachineName);
    
    public string CreateCountText => string.Format(
        GetPluralizedKey("SynchronizationConfirmation_CreateCount", _summary.CreateCount),
        _summary.CreateCount);
    
    public string SyncContentCountText => string.Format(
        GetPluralizedKey("SynchronizationConfirmation_SyncContentCount", _summary.SynchronizeContentCount),
        _summary.SynchronizeContentCount);
    
    public string SyncDateCountText => string.Format(
        GetPluralizedKey("SynchronizationConfirmation_SyncDateCount", _summary.SynchronizeDateCount),
        _summary.SynchronizeDateCount);
    
    public string DeleteCountText => string.Format(
        GetPluralizedKey("SynchronizationConfirmation_DeleteCount", _summary.DeleteCount),
        _summary.DeleteCount);
    
    public bool HasCreateActions => _summary.CreateCount > 0;
    
    public bool HasSyncContentActions => _summary.SynchronizeContentCount > 0;
    
    public bool HasSyncDateActions => _summary.SynchronizeDateCount > 0;
    
    public bool HasDeleteActions => _summary.DeleteCount > 0;
    
    public DestinationActionsSummary Summary => _summary;
    
    private string GetPluralizedKey(string baseKey, int count)
    {
        var suffix = count <= 1 ? "_One" : "_Many";
        
        return _localizationService[baseKey + suffix];
    }
}