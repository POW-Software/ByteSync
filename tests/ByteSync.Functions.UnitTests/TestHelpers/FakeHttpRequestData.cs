using System.Security.Claims;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ByteSync.Functions.UnitTests.TestHelpers;

public class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext) : base(functionContext)
    {
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
        Url = new Uri("http://localhost");
        Cookies = new List<IHttpCookie>();
        Method = "GET";
    }

    public override HttpHeadersCollection Headers { get; }
    
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    
    public override Stream Body { get; }
    
    public override Uri Url { get; }
    
    public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();
    
    public override string Method { get; }

    public override HttpResponseData CreateResponse()
    {
        var jsonSerializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ObjectSerializer>(jsonSerializer)
            .BuildServiceProvider();

        var existingContextMock = Mock.Get(FunctionContext);
        var contextMock = existingContextMock ?? new Mock<FunctionContext>();
        
        if (existingContextMock == null)
        {
            contextMock.SetupGet(c => c.Items).Returns(new Dictionary<object, object>());
        }
        
        contextMock.SetupGet(c => c.InstanceServices).Returns(serviceProvider);

        var responseMock = new Mock<HttpResponseData>(contextMock.Object);
        responseMock.SetupProperty(r => r.StatusCode);
        responseMock.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        responseMock.SetupProperty(r => r.Body, new MemoryStream());
        responseMock.SetupGet(r => r.Cookies).Returns(new Mock<HttpCookies>().Object);

        return responseMock.Object;
    }
}
