namespace ByteSync.Business.Inventories;

public enum LocalInventoryPartStatus
{
    Pending = 0,
    Running = 1,
    Cancelled = 2,
    NotLaunched = 3,
    Error = 4,
    Success = 5,
}