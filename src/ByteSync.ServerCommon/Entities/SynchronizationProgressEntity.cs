namespace ByteSync.ServerCommon.Entities;

public class SynchronizationProgressEntity
{
    public SynchronizationProgressEntity()
    {
        Members = new List<string>();
        CompletedMembers = new List<string>();
    }
    
    // New volume tracking properties
    public long ActualUploadedVolume { get; set; }      // Bytes uploaded to cloud storage
    
    public long ActualDownloadedVolume { get; set; }    // Bytes downloaded from cloud storage
    
    public long LocalCopyTransferredVolume { get; set; } // Bytes copied locally on same machine
    
    public long SynchronizedVolume { get; set; }        // Total size of synchronized files (final result)
    
    // Existing properties (kept for compatibility)
    public long ProcessedVolume { get; set; }            // DEPRECATED - Will be migrated to SynchronizedVolume
    
    public long ExchangedVolume { get; set; }            // DEPRECATED - Will be migrated to Actual*Volume
    
    public long TotalAtomicActionsCount { get; set; }
    
    public long FinishedAtomicActionsCount { get; set; }
    
    public int ErrorsCount { get; set; }
    
    public List<string> CompletedMembers { get; set; } 
    
    public List<string> Members { get; set; }
    
    public bool AllActionsDone => FinishedAtomicActionsCount >= TotalAtomicActionsCount;
    
    public bool AllMembersCompleted => CompletedMembers.Count == Members.Count;
}