using System.Net.Http;

namespace ByteSync.Client.IntegrationTests.TestHelpers.Http;

public class FlakyHttpClientFactory : IHttpClientFactory
{
    private readonly IFlakyCounter _counter;
    private readonly TimeSpan _timeout;

    public FlakyHttpClientFactory(IFlakyCounter counter, TimeSpan? timeout = null)
    {
        _counter = counter;
        _timeout = timeout ?? TimeSpan.FromMinutes(10);
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new FlakySharedHandler(_counter)) { Timeout = _timeout };
    }
}


