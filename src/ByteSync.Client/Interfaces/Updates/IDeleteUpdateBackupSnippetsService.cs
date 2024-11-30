using System.Threading.Tasks;

namespace ByteSync.Interfaces.Updates;

public interface IDeleteUpdateBackupSnippetsService
{
    Task DeleteBackupSnippetsAsync();
}