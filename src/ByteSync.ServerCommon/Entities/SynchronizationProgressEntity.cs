using System.Text.Json.Serialization;
using System.Threading;

namespace ByteSync.ServerCommon.Entities;

public class SynchronizationProgressEntity
{
    private long _processedVolume;
    private long _exchangedVolume;
    private long _versionNumber;
    private long _totalActionsCount;
    private long _finishedActionsCount;
    private int _errorsCount;
    [NonSerialized] 
    private readonly object _lockObject = new object();

    public SynchronizationProgressEntity()
    {
        Members = new List<string>();
        CompletedMembers = new List<string>();
    }
    
    [JsonPropertyName("processedVolume")]
    public long ProcessedVolume 
    { 
        get => _processedVolume;
        set => Interlocked.Exchange(ref _processedVolume, value);
    }

    [JsonPropertyName("exchangedVolume")]
    public long ExchangedVolume 
    { 
        get => _exchangedVolume;
        set => Interlocked.Exchange(ref _exchangedVolume, value);
    }

    [JsonPropertyName("versionNumber")]
    public long VersionNumber 
    { 
        get => _versionNumber;
        set => Interlocked.Exchange(ref _versionNumber, value);
    }

    [JsonPropertyName("totalActionsCount")]
    public long TotalActionsCount
    {
        get => _totalActionsCount;
        set => Interlocked.Exchange(ref _totalActionsCount, value);
    }

    [JsonPropertyName("finishedActionsCount")]
    public long FinishedActionsCount 
    { 
        get => _finishedActionsCount;
        set => Interlocked.Exchange(ref _finishedActionsCount, value);
    }

    [JsonPropertyName("errorsCount")]
    public int ErrorsCount 
    { 
        get => _errorsCount;
        set => Interlocked.Exchange(ref _errorsCount, value);
    }

    [JsonPropertyName("completedMembers")]
    public List<string> CompletedMembers { get; set; } = new List<string>();

    [JsonPropertyName("members")]
    public List<string> Members { get; set; } = new List<string>();

    [JsonIgnore]
    public bool AllActionsDone => FinishedActionsCount >= TotalActionsCount;

    [JsonIgnore]
    public bool AllMembersCompleted => CompletedMembers.Count == Members.Count;

    public long AddProcessedVolume(long value)
    {
        return Interlocked.Add(ref _processedVolume, value);
    }

    public long AddExchangedVolume(long value)
    {
        return Interlocked.Add(ref _exchangedVolume, value);
    }

    public long IncrementVersionNumber()
    {
        return Interlocked.Increment(ref _versionNumber);
    }

    public long IncrementFinishedActionsCount()
    {
        return Interlocked.Increment(ref _finishedActionsCount);
    }

    public int IncrementErrorsCount()
    {
        return Interlocked.Increment(ref _errorsCount);
    }
    
    public void IncrementProcessedVolume(long volume)
    {
        if (volume > 0)
        {
            Interlocked.Add(ref _processedVolume, volume);
        }
    }
    
    public void IncrementExchangedVolume(long volume)
    {
        if (volume > 0)
        {
            Interlocked.Add(ref _exchangedVolume, volume);
        }
    }

    public void AddCompletedMember(string memberId)
    {
        if (memberId == null) return;
        
        lock (_lockObject)
        {
            if (!CompletedMembers.Contains(memberId))
            {
                CompletedMembers.Add(memberId);
            }
        }
    }

    public void AddMember(string memberId)
    {
        if (memberId == null) return;
        
        lock (_lockObject)
        {
            if (!Members.Contains(memberId))
            {
                Members.Add(memberId);
            }
        }
    }

    public bool ContainsMember(string memberId)
    {
        if (memberId == null) return false;
        
        lock (_lockObject)
        {
            return Members.Contains(memberId);
        }
    }

    public bool ContainsCompletedMember(string memberId)
    {
        if (memberId == null) return false;
        
        lock (_lockObject)
        {
            return CompletedMembers.Contains(memberId);
        }
    }
}