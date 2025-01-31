using System.Threading;
using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IApiInvoker
{
    Task<T> GetAsync<T>(string resource, CancellationToken cancellationToken = default);
    
    Task<T> PostAsync<T>(string resource, object? postObject = null, CancellationToken cancellationToken = default);
    
    Task<T> DeleteAsync<T>(string resource, object objectToDelete, CancellationToken cancellationToken = default);
    
    Task PostAsync(string resource, object? postObject = null, CancellationToken cancellationToken = default);
}