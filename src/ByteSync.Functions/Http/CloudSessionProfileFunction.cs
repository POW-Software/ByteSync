using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.Profiles;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> CreateCloudSessionProfile([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile")] HttpRequestData req, FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var sessionId = await FunctionHelper.DeserializeRequestBody<string>(req);
            
            var result = await _cloudSessionProfileService.CreateCloudSessionProfile(sessionId, client);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating cloud session profile");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("GetCloudSessionProfileDataFunction")]
    public async Task<IActionResult> GetCloudSessionProfileData([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile/{cloudSessionProfileId}/get")] HttpRequestData req,
        FunctionContext executionContext, string cloudSessionProfileId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<GetCloudSessionProfileDataParameters>(req);
            
            var result = await _cloudSessionProfileService.GetCloudSessionProfileData(parameters, client);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting cloud session profile data for cloudSessionProfileId: {cloudSessionProfileId}", cloudSessionProfileId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("GetProfileDetailsPasswordFunction")]
    public async Task<IActionResult> GetProfileDetailsPassword([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile/{cloudSessionProfileId}/getProfileDetailsPassword")] HttpRequestData req,
        FunctionContext executionContext, string cloudSessionProfileId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<GetProfileDetailsPasswordParameters>(req);
            
            var result = await _cloudSessionProfileService.GetProfileDetailsPassword(parameters, client);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting profile details password for cloudSessionProfileId: {cloudSessionProfileId}", cloudSessionProfileId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("DeleteCloudSessionProfileFunction")]
    public async Task<IActionResult> DeleteCloudSessionProfile([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "cloudSessionProfile/{cloudSessionProfileId}/delete")] HttpRequestData req,
        FunctionContext executionContext, string cloudSessionProfileId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<DeleteCloudSessionProfileParameters>(req);
            
            var result = await _cloudSessionProfileService.DeleteCloudSessionProfile(parameters, client);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting cloud session profile with cloudSessionProfileId: {cloudSessionProfileId}", cloudSessionProfileId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}