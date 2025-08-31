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
    
    public long TotalAtomicActionsCount { get; set; }
    
    public long FinishedAtomicActionsCount { get; set; }
    
    public int ErrorsCount { get; set; }
    
    public List<string> CompletedMembers { get; set; } 
    
    public List<string> Members { get; set; }
    
    public bool AllActionsDone => FinishedAtomicActionsCount >= TotalAtomicActionsCount;
    
    public bool AllMembersCompleted => CompletedMembers.Count == Members.Count;
}