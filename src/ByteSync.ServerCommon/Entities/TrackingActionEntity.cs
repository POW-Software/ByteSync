namespace ByteSync.ServerCommon.Entities;

public class TrackingActionEntity
{
    public TrackingActionEntity()
    {
        TargetClientInstanceAndNodeIds = new HashSet<string>();
        SuccessTargetClientInstanceAndNodeIds = new HashSet<string>();
        ErrorTargetClientInstanceAndNodeIds = new HashSet<string>();
    }
    
    public string ActionsGroupId { get; set; } = null!;

    public string? SourceClientInstanceId { get; set; }
    
    public bool? IsSourceSuccess { get; set; }
        
    public HashSet<string> TargetClientInstanceAndNodeIds { get; set; }

    public HashSet<string> SuccessTargetClientInstanceAndNodeIds { get; set; }
    
    public HashSet<string> ErrorTargetClientInstanceAndNodeIds { get; set; }
    
    public long? Size { get; set; }

    public void AddSuccessOnTarget(string clientInstanceAndNodeId)
    {
        if (TargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId) && !ErrorTargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId))
        {
            SuccessTargetClientInstanceAndNodeIds.Add(clientInstanceAndNodeId);
        }
    }
    
    public void AddErrorOnTarget(string clientInstanceAndNodeId)
    {
        if (TargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId) && !SuccessTargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId))
        {
            ErrorTargetClientInstanceAndNodeIds.Add(clientInstanceAndNodeId);
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

            bool isFinished = ErrorTargetClientInstanceAndNodeIds.Count + SuccessTargetClientInstanceAndNodeIds.Count 
                              >= TargetClientInstanceAndNodeIds.Count;

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
            return ErrorTargetClientInstanceAndNodeIds.Count > 0;
        }
    }
}