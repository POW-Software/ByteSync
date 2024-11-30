namespace ByteSync.ServerCommon.Entities;

public class SynchronizationProgressEntity
{
    public SynchronizationProgressEntity()
    {
        Members = new List<string>();
        CompletedMembers = new List<string>();
    }
    
    public long ProcessedVolume { get; set; }
    
    public long ExchangedVolume { get; set; }
    
    public long VersionNumber { get; set; }
    
    public long TotalActionsCount { get; set; }
    
    public long FinishedActionsCount { get; set; }
    
    public int ErrorsCount { get; set; }
    
    public List<string> CompletedMembers { get; set; } 
    
    public List<string> Members { get; set; }
    
    public bool AllActionsDone => FinishedActionsCount >= TotalActionsCount;
    
    public bool AllMembersCompleted => CompletedMembers.Count == Members.Count;
}