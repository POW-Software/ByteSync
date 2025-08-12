namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IInventoryService
{
    Task ResetSession(string sessionId);
}