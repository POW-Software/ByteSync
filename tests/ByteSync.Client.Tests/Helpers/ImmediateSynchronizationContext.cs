namespace ByteSync.Tests.Helpers;

public sealed class ImmediateSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state)
    {
        d(state);
    }
    
    public override void Send(SendOrPostCallback d, object? state)
    {
        d(state);
    }
    
    public override SynchronizationContext CreateCopy()
    {
        return this;
    }
}