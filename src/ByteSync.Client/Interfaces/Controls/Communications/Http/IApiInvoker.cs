using System.Threading.Tasks;
using RestSharp;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IApiInvoker
{
    Task<T> GetAsync<T>(string resource);
    
    Task<T> PostAsync<T>(string resource, object? postObject = null);
    
    Task<T> DeleteAsync<T>(string resource, object objectToDelete);
    
    Task PostAsync(string resource, object? postObject = null);
    
    Task<T> InvokeRestAsync<T>(Method httpVerb, string resource, Dictionary<string, string>? additionalHeaders, object? requestObject);
}