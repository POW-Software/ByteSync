using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ByteSync.Helpers;

public static class ReactiveUtils
{
    public static async Task WaitUntilTrue(this IObservable<bool> subject, TimeSpan? timeout = null)
    {
        var observable = subject.FirstAsync(x => x);
        
        if (timeout.HasValue)
        {
            observable = observable.Timeout(timeout.Value);
        }

        await observable;
    }
}