namespace ByteSync.Common.Business.Synchronizations;

public class TrackingActionSummary
{
    public string ActionsGroupId { get; set; } = null!;

    public bool IsSuccess { get; set; }
    
    public bool IsError { get; set; }
}