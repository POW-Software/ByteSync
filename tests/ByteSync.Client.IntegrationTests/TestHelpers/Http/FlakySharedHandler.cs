using System.Net;

namespace ByteSync.Client.IntegrationTests.TestHelpers.Http;

public class FlakySharedHandler : DelegatingHandler
{
    private readonly IFlakyCounter _counter;

    public FlakySharedHandler(IFlakyCounter counter)
    {
        _counter = counter;
        InnerHandler = new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_counter.ShouldFail(request.Method))
        {
            var failure = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                RequestMessage = request
            };
            return Task.FromResult(failure);
        }

        return base.SendAsync(request, cancellationToken);
    }
}


