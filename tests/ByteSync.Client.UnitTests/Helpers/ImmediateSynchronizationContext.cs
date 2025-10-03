namespace ByteSync.Client.UnitTests.Helpers;

public sealed class ImmediateSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback callback, object? state)
    {
        callback(state);
    }
    
    public override void Send(SendOrPostCallback callback, object? state)
    {
        callback(state);
    }
    
    public override SynchronizationContext CreateCopy()
    {
        return this;
    }
}