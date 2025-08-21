namespace ByteSync.Client.IntegrationTests.TestHelpers.Http;

public interface IFlakyCounter
{
    bool ShouldFail(HttpMethod method);
}