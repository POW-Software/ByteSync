namespace ByteSync.ServerCommon.Entities;

public class TrackingActionEntity
{
    public TrackingActionEntity()
    {
        TargetClientInstanceIds = new HashSet<string>();
        SuccessTargetClientInstanceIds = new HashSet<string>();
        ErrorTargetClientInstanceIds = new HashSet<string>();
    }
    
    public string ActionsGroupId { get; set; } = null!;

    public string? SourceClientInstanceId { get; set; }
    
    public bool? IsSourceSuccess { get; set; }
        
    public HashSet<string> TargetClientInstanceIds { get; set; }

    public HashSet<string> SuccessTargetClientInstanceIds { get; set; }
    
    public HashSet<string> ErrorTargetClientInstanceIds { get; set; }
    
    public long? Size { get; set; }

    public void AddSuccessOnTarget(string clientInstanceId)
    {
        if (TargetClientInstanceIds.Contains(clientInstanceId) && !ErrorTargetClientInstanceIds.Contains(clientInstanceId))
        {
            SuccessTargetClientInstanceIds.Add(clientInstanceId);
        }
    }
    
    public void AddErrorOnTarget(string clientInstanceId)
    {
        if (TargetClientInstanceIds.Contains(clientInstanceId) && !SuccessTargetClientInstanceIds.Contains(clientInstanceId))
        {
            ErrorTargetClientInstanceIds.Add(clientInstanceId);
        }
    }
    
    public bool IsFinished
    {
        get
        {
            if (SourceClientInstanceId != null && IsSourceSuccess == false)
            {
                return true;
            }

            if (SourceClientInstanceId != null && IsSourceSuccess == null)
            {
                return false;
            }

            bool isFinished = ErrorTargetClientInstanceIds.Count + SuccessTargetClientInstanceIds.Count >= TargetClientInstanceIds.Count;

            return isFinished;
        }
    }

    public bool IsSuccess
    {
        get
        {
            return IsFinished && !IsError;
        }
    }

    public bool IsError
    {
        get
        {
            return (IsSourceSuccess != null && !IsSourceSuccess.Value) || IsErrorOnTarget;
        }
    }
    
    public bool IsErrorOnTarget
    {
        get
        {
            return ErrorTargetClientInstanceIds.Count > 0;
        }
    }
}