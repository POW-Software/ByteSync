using ByteSync.Assets.Resources;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Localizations;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Managing;

public class LinkingKeyViewModel
{
    private readonly ILocalizationService _localizationService;

    public LinkingKeyViewModel(LinkingKeys linkingKey, ILocalizationService localizationService)
    {
        LinkingKey = linkingKey;
        _localizationService = localizationService;

        UpdateDescription();
    }

    [Reactive]
    public LinkingKeys LinkingKey { get; set; }
    
    [Reactive]
    public string? Description { get; set; }
    
    internal void UpdateDescription()
    {
        Description = LinkingKey switch
        {
            LinkingKeys.Name => _localizationService[nameof(Resources.LinkingKeys_Name)],
            LinkingKeys.RelativePath => _localizationService[nameof(Resources.LinkingKeys_RelativePath)],
            _ => ""
        };
    }
}