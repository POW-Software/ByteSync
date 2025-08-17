namespace ByteSync.Business.Comparisons;

public class AtomicActionValidationResult
{
    private AtomicActionValidationResult(bool isValid, AtomicActionValidationFailureReason? failureReason)
    {
        IsValid = isValid;
        FailureReason = failureReason;
    }
    
    public bool IsValid { get; }
    
    public AtomicActionValidationFailureReason? FailureReason { get; }
    
    public static AtomicActionValidationResult Success()
    {
        return new AtomicActionValidationResult(true, null);
    }
    
    public static AtomicActionValidationResult Failure(AtomicActionValidationFailureReason reason)
    {
        return new AtomicActionValidationResult(false, reason);
    }
}
