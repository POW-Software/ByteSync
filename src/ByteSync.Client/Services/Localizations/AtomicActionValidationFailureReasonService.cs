using ByteSync.Business.Comparisons;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Localizations;

public class AtomicActionValidationFailureReasonService : IAtomicActionValidationFailureReasonService
{
    private readonly ILocalizationService _localizationService;

    public AtomicActionValidationFailureReasonService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string GetLocalizedMessage(AtomicActionValidationFailureReason reason)
    {
        var key = $"ValidationFailure_{reason}";
        return _localizationService[key];
    }
}
