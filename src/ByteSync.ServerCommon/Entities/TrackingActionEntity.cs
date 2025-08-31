using ByteSync.Common.Business.Actions;

namespace ByteSync.ServerCommon.Entities;

public class TrackingActionEntity
{
    public TrackingActionEntity()
    {
        TargetClientInstanceAndNodeIds = new HashSet<ClientInstanceIdAndNodeId>();
        SuccessTargetClientInstanceAndNodeIds = new HashSet<ClientInstanceIdAndNodeId>();
        ErrorTargetClientInstanceAndNodeIds = new HashSet<ClientInstanceIdAndNodeId>();
    }
    
    public string ActionsGroupId { get; set; } = null!;

    public string? SourceClientInstanceId { get; set; }
    
    public bool? IsSourceSuccess { get; set; }
        
    public HashSet<ClientInstanceIdAndNodeId> TargetClientInstanceAndNodeIds { get; set; }

    public HashSet<ClientInstanceIdAndNodeId> SuccessTargetClientInstanceAndNodeIds { get; set; }
    
    public HashSet<ClientInstanceIdAndNodeId> ErrorTargetClientInstanceAndNodeIds { get; set; }
    
    public long? Size { get; set; }

    public bool AddSuccessOnTarget(ClientInstanceIdAndNodeId clientInstanceAndNodeId)
    {
        if (TargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId) && !ErrorTargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId))
        {
            SuccessTargetClientInstanceAndNodeIds.Add(clientInstanceAndNodeId);
            return true;
        }

        return false;
    }
    
    public bool AddErrorOnTarget(ClientInstanceIdAndNodeId clientInstanceAndNodeId)
    {
        if (TargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId) && !SuccessTargetClientInstanceAndNodeIds.Contains(clientInstanceAndNodeId))
        {
            ErrorTargetClientInstanceAndNodeIds.Add(clientInstanceAndNodeId);
            return true;
        }

        return false;
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