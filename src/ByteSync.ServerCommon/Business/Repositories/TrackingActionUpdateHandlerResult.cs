namespace ByteSync.ServerCommon.Business.Repositories;

public class TrackingActionUpdateHandlerResult
{
    public TrackingActionUpdateHandlerResult(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; set; }
    
    public int FinishedActionsCount { get; set; }
    
    public int ErrorsCount { get; set; }
    
    public long ProcessedVolume { get; set; }
    
    public long ExchangedVolume { get; set; }

    public bool IsAChange
    {
        get
        {
            return IsSuccess && (FinishedActionsCount > 0 || ErrorsCount > 0 || ProcessedVolume > 0 || ExchangedVolume > 0);
        }
    }
}