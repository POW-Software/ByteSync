namespace ByteSync.ServerCommon.Business.Repositories;

public class UpdateEntityResult<T>
{
    public UpdateEntityResult(T? element, UpdateEntityStatus status)
    {
        Element = element;
        Status = status;
    }

    public T? Element { get; set; }
    
    public UpdateEntityStatus Status { get; set; }
    
    public bool IsSaved => Status == UpdateEntityStatus.Saved;
    
    public bool IsDeleted => Status == UpdateEntityStatus.Deleted;
    
    public bool IsWaitingForTransaction => Status == UpdateEntityStatus.WaitingForTransaction;
    
    public bool IsNoOperation => Status == UpdateEntityStatus.NoOperation;
    
    public bool IsNotFound => Status == UpdateEntityStatus.NotFound;
}