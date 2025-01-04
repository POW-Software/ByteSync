using System.Threading.Tasks;
using ByteSync.Common.Helpers;
using ByteSync.Exceptions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Services.Communications;
using RestSharp;
using Serilog;

namespace ByteSync.Services.Communications.Api;

// Issu de https://gist.github.com/pedrovasconcellos/cf2b8dcde14313e19a891408c3404337
// Mais adapté
public class ApiInvoker : IApiInvoker
{
    private readonly IAuthenticationTokensRepository _authenticationTokensRepository;
    private readonly IConnectionConstantsService _connectionConstantsService;
    private readonly IPolicyFactory _policyFactory;

    public ApiInvoker(IAuthenticationTokensRepository authenticationTokensRepository, IConnectionConstantsService connectionConstantsService,
        IPolicyFactory policyFactory)
    {
        _authenticationTokensRepository = authenticationTokensRepository;
        _connectionConstantsService = connectionConstantsService;
        _policyFactory = policyFactory;
    }

    public Task<T> GetAsync<T>(string resource)
    {
        return InvokeRestAsync<T>(Method.Get, resource, null, null);
    }

    public Task<T> PostAsync<T>(string resource, object? postObject)
    {
        return InvokeRestAsync<T>(Method.Post, resource, null, postObject);
    }

    public Task<T> DeleteAsync<T>(string resource, object objectToDelete)
    {
        return InvokeRestAsync<T>(Method.Delete, resource, null, objectToDelete);
    }

    public Task PostAsync(string resource, object? postObject)
    {
        return DoInvokeRestAsync<object>(Method.Post, resource, null, postObject, false);
    }

    public async Task<T> InvokeRestAsync<T>(Method httpVerb, string resource, Dictionary<string, string>? additionalHeaders, object? requestObject)
    {
        return await DoInvokeRestAsync<T>(httpVerb, resource, additionalHeaders, requestObject, true);
    }

    private async Task<T> DoInvokeRestAsync<T>(Method httpVerb, string resource, Dictionary<string, string>? additionalHeaders, 
        object? requestObject, bool handleResult)
    {
        var restRequest = await BuildRequest(httpVerb, resource, additionalHeaders, requestObject);

        var policy = _policyFactory.BuildRestPolicy(resource);
        var apiUrl = await _connectionConstantsService.GetApiUrl();
        var restClient = new RestClient(apiUrl);
        
        var attempt = 0;
        var restResponse = await policy.ExecuteAsync(async () =>
        {
            Log.Debug("{Uri}: Attempt {Attempt}", "/" + resource.TrimStart('/'), ++attempt);
            return await restClient.ExecuteAsync(restRequest);
        });
        
        return HandleResult<T>(handleResult, restResponse);
    }

    private async Task<RestRequest> BuildRequest(Method httpVerb, string resource, Dictionary<string, string>? additionalHeaders,
        object? requestObject)
    {
        var restRequest = new RestRequest(resource, httpVerb);
        
        var jwtToken = await _authenticationTokensRepository.GetTokens();
        if (jwtToken != null && !jwtToken.JwtToken.IsNullOrEmpty()) 
        {
            restRequest.AddHeader("authorization", jwtToken.JwtToken);   
        }
        if (additionalHeaders is { Count: > 0 })
        {
            foreach (var header in additionalHeaders)
            {
                restRequest.AddHeader(header.Key, header.Value);
            }
        }
        
        if (httpVerb != Method.Get && requestObject != null)
        {
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddJsonBody(requestObject);
        }

        return restRequest;
    }

    private static T HandleResult<T>(bool handleResult, RestResponse restResponse)
    {
        if (!restResponse.IsSuccessful)
        {
            throw new ApiException($"An error occurred while invoking the API: {restResponse.StatusCode}");  
        }
        
        if (restResponse.Content != null)
        {
            /*
            var jsonObject = JObject.Parse(restResponse.Content);
            
            var statusCode = jsonObject["StatusCode"]?.ToObject<int>();
            if (statusCode == 200)
            {
                if (handleResult)
                {
                    var valueContent = jsonObject["Value"]!.ToString();

                    if (typeof(T) == typeof(string))
                    {   
                        var result = (T)(object)valueContent;

                        return result;
                    }
                    else
                    {
                        if (typeof(T) == typeof(bool))
                        {
                            valueContent = valueContent.ToLower();
                        }
                        var result = JsonConvert.DeserializeObject<T>(valueContent);

                        return result!;
                    }
                }
                else
                {
                    return default!;
                }
            }
            else
            {
                throw new ApiException(jsonObject["Value"]!.ToString());
            }
             */
            
            return default!;
        }
        else
        {
            if (handleResult)
            {
                throw new ApiException("response content is null"); 
            }
            else
            {
                return default!;
            }
        }
    }
}