using System.Net;
using System.Net.Http;

namespace ByteSync.Client.IntegrationTests.TestHelpers.Http;

public interface IFlakyCounter
{
    bool ShouldFail(HttpMethod method);
}

public class FlakyCounter : IFlakyCounter
{
    private int _putFailuresRemaining;
    private int _getFailuresRemaining;

    public FlakyCounter(int putFailures, int getFailures)
    {
        _putFailuresRemaining = putFailures;
        _getFailuresRemaining = getFailures;
    }

    public bool ShouldFail(HttpMethod method)
    {
        if (method == HttpMethod.Put)
        {
            var before = Interlocked.CompareExchange(ref _putFailuresRemaining, 0, int.MinValue + 1);
            if (before > 0)
            {
                Interlocked.Decrement(ref _putFailuresRemaining);
                return true;
            }
        }
        else if (method == HttpMethod.Get)
        {
            var before = Interlocked.CompareExchange(ref _getFailuresRemaining, 0, int.MinValue + 1);
            if (before > 0)
            {
                Interlocked.Decrement(ref _getFailuresRemaining);
                return true;
            }
        }
        return false;
    }
}

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


