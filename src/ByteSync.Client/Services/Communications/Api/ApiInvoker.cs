using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ByteSync.Common.Helpers;
using ByteSync.Exceptions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Misc;

namespace ByteSync.Services.Communications.Api;

public class ApiInvoker : IApiInvoker
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationTokensRepository _authenticationTokensRepository;
    private readonly IConnectionConstantsService _connectionConstantsService;
    private readonly ILogger<ApiInvoker> _logger;
    
    public ApiInvoker(IHttpClientFactory httpClientFactory, IAuthenticationTokensRepository authenticationTokensRepository, 
        IConnectionConstantsService connectionConstantsService, ILogger<ApiInvoker> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _authenticationTokensRepository = authenticationTokensRepository;
        _connectionConstantsService = connectionConstantsService;
        _logger = logger;
    }

    public Task<T> GetAsync<T>(string resource)
    {
        return InvokeRestAsync<T>(HttpMethod.Get, resource, null, null);
    }

    public Task<T> PostAsync<T>(string resource, object? postObject)
    {
        return InvokeRestAsync<T>(HttpMethod.Post, resource, null, postObject);
    }

    public Task<T> DeleteAsync<T>(string resource, object objectToDelete)
    {
        return InvokeRestAsync<T>(HttpMethod.Delete, resource, null, objectToDelete);
    }

    public Task PostAsync(string resource, object? postObject)
    {
        return DoInvokeRestAsync<object>(HttpMethod.Post, resource, null, postObject, false);
    }

    private async Task<T> InvokeRestAsync<T>(HttpMethod httpVerb, string resource, Dictionary<string, string>? additionalHeaders, object? requestObject)
    {
        return await DoInvokeRestAsync<T>(httpVerb, resource, additionalHeaders, requestObject, true);
    }

    private async Task<T> DoInvokeRestAsync<T>(HttpMethod httpMethod, string resource, Dictionary<string, string>? additionalHeaders, 
        object? requestObject, bool handleResult)
    {
        using var request = await BuildRequest(httpMethod, resource, additionalHeaders, requestObject);

        // var policy = _policyFactory.BuildHttpPolicy(resource);
        var apiUrl = await _connectionConstantsService.GetApiUrl();
        // var restClient = new RestClient(apiUrl);
        
        
        try
        {
            // Envoyer la requête sans appliquer de politique supplémentaire
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(handleResult, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while invoking API: {Resource}", resource);
            throw new ApiException("An error occurred while invoking the API.", ex);
        }
        
        
        // var attempt = 0;
        // var restResponse = await policy.ExecuteAsync(async () =>
        // {
        //     _logger.LogDebug("{Uri}: Attempt {Attempt}", "/" + resource.TrimStart('/'), ++attempt);
        //     // return await restClient.ExecuteAsync(restRequest);
        //     return await _httpClient.SendAsync(request);
        // });
        //
        // return await HandleResponse<T>(handleResult, restResponse);
    }

    private async Task<HttpRequestMessage> BuildRequest(HttpMethod httpMethod, string resource, Dictionary<string, string>? additionalHeaders,
        object? requestObject)
    {
        var rootUrl = await _connectionConstantsService.GetApiUrl();
        var fullUrl = $"{rootUrl.TrimEnd('/')}/{resource}";
        
        var request = new HttpRequestMessage(httpMethod, fullUrl);
        
        var jwtToken = await _authenticationTokensRepository.GetTokens();
        if (jwtToken != null && !jwtToken.JwtToken.IsNullOrEmpty()) 
        {
            request.Headers.Add("authorization", jwtToken.JwtToken);   
        }
        if (additionalHeaders is { Count: > 0 })
        {
            foreach (var header in additionalHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        
        if (httpMethod != HttpMethod.Get && requestObject != null)
        {
            // restRequest.RequestFormat = DataFormat.Json;
            // restRequest.AddJsonBody(requestObject);
            //
            
            
            var json = JsonHelper.Serialize(requestObject);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

//     private static T HandleResult<T>(bool handleResult, HttpResponseMessage restResponse)
//     {
//         if (!restResponse.IsSuccessful)
//         {
//             throw new ApiException($"An error occurred while invoking the API: {restResponse.StatusCode}");  
//         }
//         
//         if (restResponse.Content != null)
//         {
//             if (handleResult)
//             {
//                 var result = JsonHelper.Deserialize<T>(restResponse.Content);
//                 return result;
//             }
//             else
//             {
//                 return default!;
//             }
//
//             /*
//             var jsonObject = JObject.Parse(restResponse.Content);
//
//             var statusCode = jsonObject["StatusCode"]?.ToObject<int>();
//             if (statusCode == 200)
//             {
//                 if (handleResult)
//                 {
//                     var valueContent = jsonObject["Value"]!.ToString();
//
//                     if (typeof(T) == typeof(string))
//                     {
//                         var result = (T)(object)valueContent;
//
//                         return result;
//                     }
//                     else
//                     {
//                         if (typeof(T) == typeof(bool))
//                         {
//                             valueContent = valueContent.ToLower();
//                         }
//                         var result = JsonConvert.DeserializeObject<T>(valueContent);
//
//                         return result!;
//                     }
//                 }
//                 else
//                 {
//                     return default!;
//                 }
//             }
//             else
//             {
//                 throw new ApiException(jsonObject["Value"]!.ToString());
//             }
//              */
//             
//             return default!;
//         }
//         else
//         {
//             if (handleResult)
//             {
//                 throw new ApiException("response content is null"); 
//             }
//             else
//             {
//                 return default!;
//             }
//         }
//     }

    private async Task<T> HandleResponse<T>(bool handleResult, HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            if (handleResult)
            {
                return DeserializeContent<T>(content);
            }
            else
            {
                return default!;
            }
        }
        else
        {
            string errorMessage = "An error occurred while invoking the API.";

            // Tenter de désérialiser le message d'erreur si présent
            if (!string.IsNullOrWhiteSpace(content) && handleResult)
            {
                return DeserializeContent<T>(content);
                
                // try
                // {
                //     var errorObj = JsonHelper.Deserialize<Dictionary<string, string>>(content);
                //     if (errorObj != null)
                //     {
                //         if (errorObj.ContainsKey("error"))
                //         {
                //             errorMessage = errorObj["error"];
                //         }
                //         else if (errorObj.ContainsKey("message"))
                //         {
                //             errorMessage = errorObj["message"];
                //         }
                //     }
                // }
                // catch (JsonException)
                // {
                //     // Si la désérialisation échoue, conserver le message d'erreur générique
                // }
            }

            _logger.LogError("API call failed with status code {StatusCode}: {ErrorMessage}", response.StatusCode, errorMessage);
            throw new ApiException($"API call failed with status code {response.StatusCode}: {errorMessage}");
        }
    }

    private T DeserializeContent<T>(string content)
    {
        try
        {
            var result = JsonHelper.Deserialize<T>(content);
            if (result == null)
            {
                throw new ApiException("Failed to deserialize the response content.");
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error.");
            throw new ApiException("Failed to deserialize the response content.", ex);
        }
    }
}