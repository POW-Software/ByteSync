namespace ByteSync.ServerCommon.Business.Repositories;

public enum UpdateEntityStatus
{
    Saved,
    Deleted,
    WaitingForTransaction,
    NotFound,
    NoOperation
}