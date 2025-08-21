namespace ByteSync.Client.IntegrationTests.TestHelpers.Http;

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