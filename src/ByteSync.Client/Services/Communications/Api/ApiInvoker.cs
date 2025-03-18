using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Controls.Json;
using ByteSync.Common.Helpers;
using ByteSync.Exceptions;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Services.Communications;

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

    public Task<T> GetAsync<T>(string resource, CancellationToken cancellationToken = default)
    {
        return InvokeRestAsync<T>(HttpMethod.Get, resource, null, null, cancellationToken);
    }

    public Task<T> PostAsync<T>(string resource, object? postObject, CancellationToken cancellationToken = default)
    {
        return InvokeRestAsync<T>(HttpMethod.Post, resource, null, postObject, cancellationToken);
    }

    public Task<T> DeleteAsync<T>(string resource, object objectToDelete, CancellationToken cancellationToken = default)
    {
        return InvokeRestAsync<T>(HttpMethod.Delete, resource, null, objectToDelete, cancellationToken);
    }

    public Task PostAsync(string resource, object? postObject, CancellationToken cancellationToken = default)
    {
        return DoInvokeRestAsync<object>(HttpMethod.Post, resource, null, postObject, false, cancellationToken);
    }

    private async Task<T> InvokeRestAsync<T>(HttpMethod httpVerb, string resource, Dictionary<string, string>? additionalHeaders, object? requestObject,
        CancellationToken cancellationToken)
    {
        return await DoInvokeRestAsync<T>(httpVerb, resource, additionalHeaders, requestObject, true, cancellationToken);
    }

    private async Task<T> DoInvokeRestAsync<T>(HttpMethod httpMethod, string resource, Dictionary<string, string>? additionalHeaders, 
        object? requestObject, bool handleResult, CancellationToken cancellationToken)
    {
        using var request = await BuildRequest(httpMethod, resource, additionalHeaders, requestObject);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponse<T>(handleResult, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while invoking API: {Resource}", resource);
            throw new ApiException($"An error occurred while invoking the API on resource {resource}", ex);
        }
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
            var json = JsonHelper.Serialize(requestObject);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

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
            if (response.ReasonPhrase != null)
            {
                errorMessage += $" Reason: {response.ReasonPhrase}";
            }
            
            if (!IsEmptyContent(content) && handleResult)
            {
                return DeserializeContent<T>(content);
            }

            _logger.LogError("API call failed with status code {StatusCode}: {ErrorMessage}", response.StatusCode, errorMessage);
            throw new ApiException($"API call failed with status code {response.StatusCode}: {errorMessage}", response.StatusCode);
        }
    }
    
    private bool IsEmptyContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "null")
        {
            return true;
        }
        else
        {
            var trimmed = content.Trim();
            if (trimmed == "{}")
            {
                return true;
            }

            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                var trimmedNoSpaces = trimmed
                    .Replace(" ", "")
                    .Replace("\t", "")
                    .Replace("\n", "")
                    .Replace("\r", "");

                if (trimmedNoSpaces == """{"$id":"1"}""")
                {
                    return true;
                }
            }

            return false;
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
            _logger.LogError(ex, "JSON deserialization error");
            throw new ApiException("Failed to deserialize the response content.", ex);
        }
    }
}