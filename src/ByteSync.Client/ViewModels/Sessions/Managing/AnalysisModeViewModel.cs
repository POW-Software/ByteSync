using ByteSync.Business.Sessions;
using ByteSync.Interfaces;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Managing;

public class AnalysisModeViewModel
{
    private readonly ILocalizationService _localizationService;

    public AnalysisModeViewModel(ILocalizationService localizationService, AnalysisModes analysisMode)
    {
        _localizationService = localizationService;
        AnalysisMode = analysisMode;

        UpdateDescription();
    }

    [Reactive]
    public AnalysisModes AnalysisMode { get; set; }
    
    [Reactive]
    public string? Description { get; set; }
    
    internal void UpdateDescription()
    {
        Description = AnalysisMode switch
        {
            AnalysisModes.Smart => _localizationService["AnalysisMode_Smart"],
            AnalysisModes.Checksum => _localizationService["AnalysisMode_Checksum"],
            _ => ""
        };
    }
}