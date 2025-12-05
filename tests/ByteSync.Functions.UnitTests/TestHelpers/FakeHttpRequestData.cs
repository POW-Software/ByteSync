using System.Net;
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
    private readonly IServiceProvider _serviceProvider;
    
    public FakeHttpRequestData(FunctionContext functionContext) : base(functionContext)
    {
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
        Url = new Uri("http://localhost");
        Cookies = new List<IHttpCookie>();
        Method = "GET";
        
        var jsonSerializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _serviceProvider = new ServiceCollection()
            .AddSingleton<ObjectSerializer>(jsonSerializer)
            .BuildServiceProvider();
        
        var contextMock = Mock.Get(functionContext);
        contextMock.SetupGet(c => c.InstanceServices).Returns(_serviceProvider);
    }

    public override HttpHeadersCollection Headers { get; }
    
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    
    public override Stream Body { get; }
    
    public override Uri Url { get; }
    
    public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();
    
    public override string Method { get; }

    public override HttpResponseData CreateResponse()
    {
        return new FakeHttpResponseData(FunctionContext);
    }
    
    private class FakeHttpResponseData : HttpResponseData
    {
        public FakeHttpResponseData(FunctionContext functionContext) : base(functionContext)
        {
            StatusCode = HttpStatusCode.OK;
            Headers = new HttpHeadersCollection();
            Body = new MemoryStream();
            Cookies = new Mock<HttpCookies>().Object;
        }

        public override HttpStatusCode StatusCode { get; set; }
        
        public override HttpHeadersCollection Headers { get; set; }
        
        public override Stream Body { get; set; }
        
        public override HttpCookies Cookies { get; }
    }
}
