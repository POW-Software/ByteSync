using System.Text;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls.Json;
using ByteSync.ServerCommon.Business.Auth;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.TestHelpers;

public static class HttpFunctionTestHelper
{
    public static FunctionContext BuildFunctionContextWithClient()
    {
        var mockContext = new Mock<FunctionContext>();
        var items = new Dictionary<object, object>();
        mockContext.SetupGet(c => c.Items).Returns(items);
        
        var client = new Client("cli", "cliInst", "1.0.0", OSPlatforms.Windows, "127.0.0.1");
        items[AuthConstants.FUNCTION_CONTEXT_CLIENT] = client;
        
        return mockContext.Object;
    }
    
    public static async Task WriteBodyAsync<T>(FakeHttpRequestData request, T body)
    {
        var json = JsonHelper.Serialize(body);
        var bytes = Encoding.UTF8.GetBytes(json);
        request.Body.SetLength(0);
        await request.Body.WriteAsync(bytes, 0, bytes.Length);
        request.Body.Position = 0;
    }
}
