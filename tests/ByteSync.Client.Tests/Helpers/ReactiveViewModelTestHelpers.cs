using System.Linq.Expressions;
using FluentAssertions;
using ReactiveUI;

namespace ByteSync.Tests.Helpers;

public static class ReactiveViewModelTestHelpers
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);

    public static bool WaitForProperty<TViewModel, TProp>(
        TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        Func<TProp, bool> predicate,
        TimeSpan timeout)
        where TViewModel : class
    {
        return WaitForProperty(viewModel, selector, predicate, timeout, out _);
    }

    public static bool WaitForProperty<TViewModel, TProp>(
        TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        Func<TProp, bool> predicate,
        TimeSpan timeout,
        out TProp? lastValue)
        where TViewModel : class
    {
        TProp latest = default!;
        var evt = new ManualResetEventSlim();
        using var sub = viewModel
            .WhenAnyValue(selector)
            .Subscribe(v =>
            {
                latest = v;
                if (predicate(v))
                {
                    evt.Set();
                }
            });

        var signaled = evt.Wait(timeout);
        lastValue = latest;

        return signaled;
    }

    public static void ShouldEventuallyBe<TViewModel, TProp>(
        TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        TProp expected,
        TimeSpan timeout)
        where TViewModel : class
    {
        var ok = WaitForProperty(viewModel, selector, v => Equals(v, expected), timeout, out var lastValue);
        var member = GetMemberName(selector);
        ok.Should().BeTrue(
            $"Expected {member} to become '{expected}' within {timeout}, but last value was '{lastValue}'");
    }

    public static void ShouldEventuallyBe<TViewModel, TProp>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        TProp expected)
        where TViewModel : class
    {
        ShouldEventuallyBe(viewModel, selector, expected, _defaultTimeout);
    }

    private static string GetMemberName<TViewModel, TProp>(Expression<Func<TViewModel, TProp>> selector)
    {
        return selector.Body is MemberExpression m ? m.Member.Name : selector.ToString();
    }
}