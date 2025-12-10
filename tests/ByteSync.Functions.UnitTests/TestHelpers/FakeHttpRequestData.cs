using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace ByteSync.Functions.UnitTests.TestHelpers;

public class FakeHttpRequestData : HttpRequestData
{
    private static readonly IServiceProvider SharedServiceProvider = BuildServiceProvider();
    
    private static IServiceProvider BuildServiceProvider()
    {
        var jsonSerializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var workerOptions = Options.Create(new WorkerOptions { Serializer = jsonSerializer });
        
        return new ServiceCollection()
            .AddSingleton<ObjectSerializer>(jsonSerializer)
            .AddSingleton<IOptions<WorkerOptions>>(workerOptions)
            .BuildServiceProvider();
    }
    
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
        return new FakeHttpResponseData(new FakeFunctionContext(SharedServiceProvider));
    }
    
    private class FakeFunctionContext : FunctionContext
    {
        public FakeFunctionContext(IServiceProvider serviceProvider)
        {
            InstanceServices = serviceProvider;
        }

        public override string InvocationId => Guid.NewGuid().ToString();
        public override string FunctionId => "test-function";
        public override TraceContext TraceContext => null!;
        public override BindingContext BindingContext => null!;
        public override RetryContext RetryContext => null!;
        public override IServiceProvider InstanceServices { get; set; }
        public override FunctionDefinition FunctionDefinition => null!;
        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
        public override IInvocationFeatures Features => null!;
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
