using System.Linq.Expressions;
using System.Reactive.Linq;
using FluentAssertions;
using ReactiveUI;

namespace ByteSync.Tests.Helpers;

public static class ReactiveViewModelTestHelpers
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

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

    // Extension-style helpers for concise call-sites: viewModel.ShouldEventuallyBe(vm => vm.Prop, expected)
    public static void ShouldEventuallyBe<TViewModel, TProp>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TProp>> selector,
        TProp expected)
        where TViewModel : class
        => ShouldEventuallyBe(viewModel, selector, expected, DefaultTimeout);

    // If a custom timeout is needed, call the non-extension overload explicitly.
}