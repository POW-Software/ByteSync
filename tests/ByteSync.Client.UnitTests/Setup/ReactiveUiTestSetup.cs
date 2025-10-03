using System.Reactive.Concurrency;
using NUnit.Framework;
using ReactiveUI;

namespace ByteSync.Client.UnitTests.Setup;

[SetUpFixture]
public class ReactiveUiTestSetup
{
    private IScheduler? _previousMainScheduler;
    
    [OneTimeSetUp]
    public void SetUp()
    {
        _previousMainScheduler = RxApp.MainThreadScheduler;
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;
    }
    
    [OneTimeTearDown]
    public void TearDown()
    {
        if (_previousMainScheduler != null)
        {
            RxApp.MainThreadScheduler = _previousMainScheduler;
        }
    }
}