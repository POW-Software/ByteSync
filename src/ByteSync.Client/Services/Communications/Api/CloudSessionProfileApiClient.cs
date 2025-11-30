using ByteSync.Common.Business.Profiles;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class CloudSessionProfileApiClient : ICloudSessionProfileApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<CloudSessionProfileApiClient> _logger;
    
    public CloudSessionProfileApiClient(IApiInvoker apiInvoker, ILogger<CloudSessionProfileApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }
    
    public async Task<CreateCloudSessionProfileResult> CreateCloudSessionProfile(string sessionId)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<CreateCloudSessionProfileResult>($"cloudSessionProfile", sessionId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
            
            throw;
        }
    }
    
    public async Task<CloudSessionProfileData> GetCloudSessionProfileData(string sessionId, string additionalName)
    {
        try
        {
            var parameters = new GetCloudSessionProfileDataParameters
            {
                SessionId = sessionId,
                CloudSessionProfileId = additionalName
            };
            
            var result = await _apiInvoker.PostAsync<CloudSessionProfileData>($"cloudSessionProfile/{additionalName}/get", parameters);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
            
            throw;
        }
    }
    
    public async Task<bool> DeleteCloudSessionProfile(DeleteCloudSessionProfileParameters parameters)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<bool>($"cloudSessionProfile/{parameters.CloudSessionProfileId}/delete", parameters);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
            
            throw;
        }
    }
}