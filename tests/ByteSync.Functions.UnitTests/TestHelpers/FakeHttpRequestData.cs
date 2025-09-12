using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace ByteSync.Functions.UnitTests.TestHelpers;

public class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext) : base(functionContext)
    {
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
        Url = new Uri("http://localhost");
    }

    public override HttpHeadersCollection Headers { get; }
    
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    
    public override Stream Body { get; }
    
    public override Uri Url { get; }
    
    public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();
    
    public override string Method { get; }

    public override HttpResponseData CreateResponse()
    {
        var responseMock = new Mock<HttpResponseData>(FunctionContext);
        responseMock.SetupProperty(r => r.StatusCode);
        responseMock.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        responseMock.SetupProperty(r => r.Body, new MemoryStream());
        responseMock.SetupGet(r => r.Cookies).Returns(new Mock<HttpCookies>().Object);

        return responseMock.Object;
    }
}
