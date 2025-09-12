using System.Linq.Expressions;
using System.Reactive.Linq;
using FluentAssertions;
using ReactiveUI;

namespace ByteSync.Tests.Helpers;

public static class ReactiveViewModelTestHelpers
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(1);

    public static bool WaitForProperty<TViewModel, TProp>(
        TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        Func<TProp, bool> predicate,
        TimeSpan timeout)
        where TViewModel : class
    {
        var evt = new ManualResetEventSlim();
        using var sub = viewModel
            .WhenAnyValue(selector)
            .Where(predicate)
            .Subscribe(_ => evt.Set());

        return evt.Wait(timeout);
    }

    public static void ShouldEventuallyBe<TViewModel, TProp>(
        TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        TProp expected,
        TimeSpan timeout)
        where TViewModel : class
    {
        var ok = WaitForProperty(viewModel, selector, v => Equals(v, expected), timeout);
        ok.Should().BeTrue();
    }

    public static void ShouldEventuallyBe<TViewModel, TProp>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        TProp expected)
        where TViewModel : class
    {
        ShouldEventuallyBe(viewModel, selector, expected, _defaultTimeout);
    }
}