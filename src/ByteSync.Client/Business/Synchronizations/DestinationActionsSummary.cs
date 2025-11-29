namespace ByteSync.Business.Synchronizations;

public class DestinationActionsSummary
{
    public string DestinationCode { get; set; } = string.Empty;
    
    public string MachineName { get; set; } = string.Empty;
    
    public int CreateCount { get; set; }
    
    public int SynchronizeContentCount { get; set; }
    
    public int SynchronizeDateCount { get; set; }
    
    public int DeleteCount { get; set; }
    
    public int TotalActionsCount => CreateCount + SynchronizeContentCount + SynchronizeDateCount + DeleteCount;
}
