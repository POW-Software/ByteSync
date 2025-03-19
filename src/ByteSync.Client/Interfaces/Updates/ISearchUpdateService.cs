namespace ByteSync.Interfaces.Updates;

public interface ISearchUpdateService
{
    Task SearchNextAvailableVersionsAsync();
}