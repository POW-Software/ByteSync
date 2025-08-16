using System.Reactive.Linq;
using System.Threading;

namespace ByteSync.Helpers;

public static class ReactiveUtils
{
    public static async Task WaitUntilTrue(this IObservable<bool> subject, CancellationToken cancellationToken = default)
    {
        await subject.WaitUntilTrue(TimeSpan.FromMilliseconds(-1), cancellationToken);
    }
    
    public static async Task WaitUntilTrue(this IObservable<bool> subject, TimeSpan? timeout = null)
    {
        var observable = subject.FirstAsync(x => x);
        
        // Only apply timeout if it's a positive value (null or negative means infinite timeout)
        if (timeout.HasValue && timeout.Value.TotalMilliseconds > 0)
        {
            observable = observable.Timeout(timeout.Value);
        }

        await observable;
    }
    
    public static async Task WaitUntilTrue(this IObservable<bool> subject, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var observable = subject.FirstAsync(x => x);
        
        // Only apply timeout if it's a positive value (negative means infinite timeout)
        if (timeout.TotalMilliseconds > 0)
        {
            observable = observable.Timeout(timeout);
        }
        
        // Use a TaskCompletionSource to handle cancellation properly
        var tcs = new TaskCompletionSource<bool>();
        
        using var subscription = observable.Subscribe(
            result => tcs.TrySetResult(result),
            error => tcs.TrySetException(error)
        );
        
        using var cancellationRegistration = cancellationToken.Register(() => tcs.TrySetCanceled());
        
        await tcs.Task;
    }
}