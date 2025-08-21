using System.Net;
using System.Net.Http;

namespace ByteSync.Client.IntegrationTests.TestHelpers.Http;

public class FlakyOnceHandler : DelegatingHandler
{
    private int _remainingPutFailures;
    private int _remainingGetFailures;

    public FlakyOnceHandler(int putFailures = 1, int getFailures = 0)
    {
        _remainingPutFailures = putFailures;
        _remainingGetFailures = getFailures;
        InnerHandler = new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Put)
        {
            if (Interlocked.Exchange(ref _remainingPutFailures, Math.Max(0, _remainingPutFailures - 1)) > 0)
            {
                var failure = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    RequestMessage = request
                };
                return Task.FromResult(failure);
            }
        }
        else if (request.Method == HttpMethod.Get)
        {
            if (Interlocked.Exchange(ref _remainingGetFailures, Math.Max(0, _remainingGetFailures - 1)) > 0)
            {
                var failure = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    RequestMessage = request
                };
                return Task.FromResult(failure);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}


