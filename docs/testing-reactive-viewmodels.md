# Testing ReactiveUI ViewModels (Client)

This document explains how Client ViewModel tests handle ReactiveUI pipelines (`WhenAnyValue`, `ObserveOn`, etc.) to avoid flakiness and
keep tests fast and deterministic.

## Background

Several ViewModels schedule updates with `ObserveOn(RxApp.MainThreadScheduler)`. In test environments there is no real UI dispatcher, so
scheduled work may not run or may be timing‑sensitive.

## Global Test Setup

We configure the main ReactiveUI scheduler at the Client.Tests assembly level:

- File: `tests/ByteSync.Client.Tests/Setup/ReactiveUiTestSetup.cs`
- Purpose: set `RxApp.MainThreadScheduler = ImmediateScheduler.Instance` in `[OneTimeSetUp]`, and restore the previous value in
  `[OneTimeTearDown]`.

Effect: any `ObserveOn(RxApp.MainThreadScheduler)` executes immediately and synchronously, without relying on a UI dispatcher.

## Helpers for Reactive Property Assertions

To wait for a property fed by a reactive stream (e.g., `WhenAnyValue`, `CombineLatest`) to reach a state/value, use the helper:

- File: `tests/ByteSync.Client.Tests/Helpers/ReactiveViewModelTestHelpers.cs`
- API:
    - `WaitForProperty(viewModel, selector, predicate, timeout)`
    - `ShouldEventuallyBe(viewModel, selector, expected, timeout)`

These helpers avoid `Thread.Sleep`/`SpinWait` and make tests more reliable and explicit.

## Example

```csharp
using ByteSync.Tests.Helpers;

// Arrange
using var _ = viewModel.Activator.Activate();
// Emit an event that triggers a reactive update
processData.SynchronizationProgress.OnNext(progress);

// Assert (wait up to 1s for the property to reach the expected value)
ReactiveViewModelTestHelpers.ShouldEventuallyBe(viewModel, vm => vm.HandledActions, 5L, TimeSpan.FromSeconds(1));
```

## Best Practices

- Prefer `ShouldEventuallyBe`/`WaitForProperty` for properties driven by observables.
- Avoid `Thread.Sleep` and `SpinWait` — they cause slow and flaky tests.
- Activate the ViewModel with `using var _ = viewModel.Activator.Activate();` when subscriptions live inside `WhenActivated`.
- Provide required correlation fields the ViewModel filters on (e.g., `SessionId`) so notifications aren’t ignored in tests.

## Troubleshooting

- If an assertion never triggers:
    - Ensure the ViewModel is activated (`WhenActivated`).
    - Ensure filtering conditions (e.g., `SessionId`) are satisfied in the test data.
    - Increase the helper `timeout` reasonably (e.g., 2s) for more complex pipelines.

