using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using ByteSync;
using ByteSync.TestsCommon;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public abstract class HeadlessIntegrationTest : IntegrationTest
{
    private static bool _initialized;
    // No need for a running lifetime; we will pump UI jobs manually

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

    [OneTimeTearDown]
    public static void GlobalTeardown()
    {
        _initialized = false;
    }

    protected Task ExecuteOnUiThread(Func<Task> action)
    {
        var tcs = new TaskCompletionSource();
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        PumpUiJobsUntilComplete(tcs.Task);
        return tcs.Task;
    }

    protected Task<T> ExecuteOnUiThread<T>(Func<Task<T>> action)
    {
        var tcs = new TaskCompletionSource<T>();
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var result = await action();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        PumpUiJobsUntilComplete(tcs.Task);
        return tcs.Task;
    }

    private static void PumpUiJobsUntilComplete(Task task, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (!task.IsCompleted && sw.ElapsedMilliseconds < timeoutMs)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
    }

    protected void PumpUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
    }

    private class TestApp : Application
    {
    }
}
