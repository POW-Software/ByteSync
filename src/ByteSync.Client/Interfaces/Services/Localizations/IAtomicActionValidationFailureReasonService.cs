using ByteSync.Business.Comparisons;

namespace ByteSync.Interfaces.Services.Localizations;

public interface IAtomicActionValidationFailureReasonService
{
    string GetLocalizedMessage(AtomicActionValidationFailureReason reason);
}