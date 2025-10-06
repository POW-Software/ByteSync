using ByteSync.Assets.Resources;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Services.Localizations;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Managing;

public class MatchingModeViewModel
{
    private readonly ILocalizationService _localizationService;
    
    public MatchingModeViewModel(MatchingModes matchingMode, ILocalizationService localizationService)
    {
        MatchingMode = matchingMode;
        _localizationService = localizationService;
        
        UpdateDescription();
    }
    
    [Reactive]
    public MatchingModes MatchingMode { get; set; }
    
    [Reactive]
    public string? Description { get; set; }
    
    internal void UpdateDescription()
    {
        Description = MatchingMode switch
        {
            MatchingModes.Flat => _localizationService[nameof(Resources.MatchingModes_Flat)],
            MatchingModes.Tree => _localizationService[nameof(Resources.MatchingModes_Tree)],
            _ => ""
        };
    }
}