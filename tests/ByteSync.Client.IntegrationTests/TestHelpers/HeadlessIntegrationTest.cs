using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using ByteSync;
using ByteSync.TestsCommon;
using NUnit.Framework;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public abstract class HeadlessIntegrationTest : IntegrationTest
{
    private static bool _initialized;

    [OneTimeSetUp]
    public static void GlobalSetup()
    {
        if (_initialized)
        {
            return;
        }

        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();

        _initialized = true;
    }

    protected Task ExecuteOnUiThread(Func<Task> action)
    {
        var tcs = new TaskCompletionSource();
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await action();
            tcs.SetResult();
        });
        return tcs.Task;
    }

    protected Task<T> ExecuteOnUiThread<T>(Func<Task<T>> action)
    {
        var tcs = new TaskCompletionSource<T>();
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var result = await action();
            tcs.SetResult(result);
        });
        return tcs.Task;
    }

    private class TestApp : Application
    {
    }
}
