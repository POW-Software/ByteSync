using System.Reactive.Linq;
using System.Text;
using ByteSync.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Inventories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

public class DateAndInventoryPartsViewModel : ViewModelBase
{
    private DateTime _lastWriteTimeUtc;
    private readonly ILocalizationService _localizationService;

    public DateAndInventoryPartsViewModel(ContentIdentityViewModel contentIdentityViewModel, DateTime lastWriteTimeUtc, HashSet<InventoryPart> inventoryParts,
        ISessionService sessionDataHolder, ILocalizationService localizationService)
    {
        _lastWriteTimeUtc = lastWriteTimeUtc;

        _localizationService = localizationService;

        if (sessionDataHolder.CurrentSessionSettings!.LinkingKey == LinkingKeys.Name ||
                contentIdentityViewModel.ContentIdentity.HasManyFileSystemDescriptionOnAnInventoryPart)
        {
            var sb = new StringBuilder();
            var isFirst = true;
            foreach (var inventoryPart in inventoryParts)
            {
                if (!isFirst)
                {
                    sb.Append(", ");
                }
                
                var count = contentIdentityViewModel.ContentIdentity.GetFileSystemDescriptions(inventoryPart).Count;

                sb.Append(inventoryPart.Code).Append(" (").Append(count).Append(')');

                isFirst = false;
            }

            InventoryParts = sb.ToString();
        }
        else
        {
            InventoryParts = inventoryParts.Select(ip => ip.Code).ToList().JoinToString(", ");
        }

        _localizationService.CurrentCultureObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => OnLocaleChanged());
    }

    [Reactive]
    public string LastWriteTimeUtc { get; set; } = null!;

    [Reactive]
    public string InventoryParts { get; set; }
    
    private void OnLocaleChanged()
    {
        LastWriteTimeUtc = _lastWriteTimeUtc.ToString("G", _localizationService.CurrentCultureDefinition!.CultureInfo);
    }
}