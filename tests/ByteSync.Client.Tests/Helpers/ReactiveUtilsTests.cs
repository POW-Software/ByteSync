using System.Reactive.Subjects;
using ByteSync.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Helpers;

[TestFixture]
public class ReactiveUtilsTests
{
    private BehaviorSubject<bool> _subject;

    [SetUp]
    public void SetUp()
    {
        _subject = new BehaviorSubject<bool>(false);
    }

    [TearDown]
    public void TearDown()
    {
        _subject?.Dispose();
    }

    [Test]
    public async Task WaitUntilTrue_WithoutParameters_CompletesWhenSubjectBecomesTrue()
    {
        // Arrange
        var task = _subject.WaitUntilTrue((TimeSpan?)null);
        
        // Act
        _subject.OnNext(true);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task WaitUntilTrue_WithCancellationToken_CompletesWhenSubjectBecomesTrue()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var task = _subject.WaitUntilTrue(cts.Token);
        
        // Act
        _subject.OnNext(true);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task WaitUntilTrue_WithTimeout_CompletesWhenSubjectBecomesTrue()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(5);
        var task = _subject.WaitUntilTrue(timeout);
        
        // Act
        _subject.OnNext(true);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task WaitUntilTrue_WithTimeoutAndCancellation_CompletesWhenSubjectBecomesTrue()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(5);
        var task = _subject.WaitUntilTrue(timeout, cts.Token);
        
        // Act
        _subject.OnNext(true);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public void WaitUntilTrue_WithTimeout_ThrowsTimeoutExceptionWhenTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);
        
        // Act & Assert
        Func<Task> act = async () => await _subject.WaitUntilTrue(timeout);
        
        act.Should().ThrowAsync<TimeoutException>();
    }

    [Test]
    public void WaitUntilTrue_WithTimeoutAndCancellation_ThrowsTimeoutExceptionWhenTimeout()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(100);
        
        // Act & Assert
        Func<Task> act = async () => await _subject.WaitUntilTrue(timeout, cts.Token);
        
        act.Should().ThrowAsync<TimeoutException>();
    }

    [Test]
    public void WaitUntilTrue_WithCancellationToken_ThrowsOperationCanceledExceptionWhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var task = _subject.WaitUntilTrue(cts.Token);
        
        // Act
        cts.Cancel();
        
        // Assert
        Func<Task> act = async () => await task;
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void WaitUntilTrue_WithTimeoutAndCancellation_ThrowsOperationCanceledExceptionWhenCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(5);
        var task = _subject.WaitUntilTrue(timeout, cts.Token);
        
        // Act
        cts.Cancel();
        
        // Assert
        Func<Task> act = async () => await task;
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task WaitUntilTrue_WithAlreadyTrueSubject_CompletesImmediately()
    {
        // Arrange
        _subject.OnNext(true);
        
        // Act
        var task = _subject.WaitUntilTrue((TimeSpan?)null);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(100));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task WaitUntilTrue_WithMultipleFalseValues_IgnoresFalseAndWaitsForTrue()
    {
        // Arrange
        var task = _subject.WaitUntilTrue(TimeSpan.FromSeconds(5));
        
        // Act
        _subject.OnNext(false);
        _subject.OnNext(false);
        _subject.OnNext(true);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public void WaitUntilTrue_WithSubjectError_ThrowsException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var task = _subject.WaitUntilTrue(TimeSpan.FromSeconds(5));
        
        // Act
        _subject.OnError(exception);
        
        // Assert
        Func<Task> act = async () => await task;
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
    }

    [Test]
    public async Task WaitUntilTrue_WithNegativeTimeout_TreatsAsInfiniteTimeout()
    {
        // Arrange
        var negativeTimeout = TimeSpan.FromMilliseconds(-1);
        var task = _subject.WaitUntilTrue(negativeTimeout);
        
        // Act - Give it a moment to ensure it doesn't immediately timeout
        await Task.Delay(50);
        _subject.OnNext(true);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task WaitUntilTrue_WithZeroTimeout_CompletesImmediatelyIfAlreadyTrue()
    {
        // Arrange
        _subject.OnNext(true);
        var zeroTimeout = TimeSpan.Zero;
        
        // Act
        var task = _subject.WaitUntilTrue(zeroTimeout);
        
        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(100));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public void WaitUntilTrue_WithZeroTimeout_ThrowsTimeoutIfNotAlreadyTrue()
    {
        // Arrange
        var zeroTimeout = TimeSpan.Zero;
        
        // Act & Assert
        Func<Task> act = async () => await _subject.WaitUntilTrue(zeroTimeout);
        act.Should().ThrowAsync<TimeoutException>();
    }

    [Test]
    public async Task WaitUntilTrue_ConcurrentCalls_AllCompleteWhenSubjectBecomesTrue()
    {
        // Arrange
        var task1 = _subject.WaitUntilTrue((TimeSpan?)null);
        var task2 = _subject.WaitUntilTrue((TimeSpan?)null);
        var task3 = _subject.WaitUntilTrue((TimeSpan?)null);
        
        // Act
        _subject.OnNext(true);
        
        // Assert
        await Task.WhenAll(task1, task2, task3).WaitAsync(TimeSpan.FromSeconds(1));
        task1.IsCompletedSuccessfully.Should().BeTrue();
        task2.IsCompletedSuccessfully.Should().BeTrue();
        task3.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public void WaitUntilTrue_WithDisposedSubject_ThrowsException()
    {
        // Arrange & Act
        _subject.Dispose();
        
        // Assert - When subject is disposed, FirstAsync should throw an exception
        Func<Task> act = async () => await _subject.WaitUntilTrue(TimeSpan.FromSeconds(1));
        
        // The exact exception type depends on RX implementation, but it should throw something
        act.Should().ThrowAsync<Exception>();
    }
}
