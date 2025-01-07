using System.Net;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.Profiles;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class CloudSessionProfileFunction
{
    private readonly ICloudSessionProfileService _cloudSessionProfileService;
    private readonly ILogger<CloudSessionProfileFunction> _logger;

    public CloudSessionProfileFunction(ICloudSessionProfileService cloudSessionProfileService, ILoggerFactory loggerFactory)
    {
        _cloudSessionProfileService = cloudSessionProfileService;
        _logger = loggerFactory.CreateLogger<CloudSessionProfileFunction>();
    }
    
    [Function("CreateCloudSessionProfileFunction")]
    public async Task<HttpResponseData> CreateCloudSessionProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile")] 
        HttpRequestData req,
        FunctionContext executionContext)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var sessionId = await FunctionHelper.DeserializeRequestBody<string>(req);

            var result = await _cloudSessionProfileService.CreateCloudSessionProfile(sessionId, client);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating cloud session profile");
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("GetCloudSessionProfileDataFunction")]
    public async Task<HttpResponseData> GetCloudSessionProfileData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile/{cloudSessionProfileId}/get")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string cloudSessionProfileId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<GetCloudSessionProfileDataParameters>(req);

            var result = await _cloudSessionProfileService.GetCloudSessionProfileData(parameters, client);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error while getting cloud session profile data for cloudSessionProfileId: {cloudSessionProfileId}",
                cloudSessionProfileId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("GetProfileDetailsPasswordFunction")]
    public async Task<HttpResponseData> GetProfileDetailsPassword([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile/{cloudSessionProfileId}/getProfileDetailsPassword")] HttpRequestData req,
        FunctionContext executionContext, string cloudSessionProfileId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<GetProfileDetailsPasswordParameters>(req);

            var result = await _cloudSessionProfileService.GetProfileDetailsPassword(parameters, client);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error while getting profile details password for cloudSessionProfileId: {cloudSessionProfileId}",
                cloudSessionProfileId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("DeleteCloudSessionProfileFunction")]
    public async Task<HttpResponseData> DeleteCloudSessionProfile([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile/{cloudSessionProfileId}/delete")] HttpRequestData req,
        FunctionContext executionContext, string cloudSessionProfileId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<DeleteCloudSessionProfileParameters>(req);

            var result = await _cloudSessionProfileService.DeleteCloudSessionProfile(parameters, client);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error while deleting cloud session profile with cloudSessionProfileId: {cloudSessionProfileId}",
                cloudSessionProfileId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
}