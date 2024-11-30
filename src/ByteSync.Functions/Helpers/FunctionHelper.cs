﻿using ByteSync.ServerCommon.Business.Auth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace ByteSync.Functions.Helpers;

public static class FunctionHelper
{
    public static Client GetClientFromContext(FunctionContext context)
    {
        var client = context.Items[AuthConstants.FUNCTION_CONTEXT_CLIENT] as Client;
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client), "Client cannot be null");
        }
        return client;
    }

    public static async Task<T> DeserializeRequestBody<T>(HttpRequestData req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var deserializedObject = JsonConvert.DeserializeObject<T>(requestBody);
        if (deserializedObject == null)
        {
            throw new ArgumentNullException(nameof(deserializedObject), "Deserialized object cannot be null");
        }
        return deserializedObject;
    }
}