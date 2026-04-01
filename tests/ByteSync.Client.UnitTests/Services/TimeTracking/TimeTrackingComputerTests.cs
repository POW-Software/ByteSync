using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Services.TimeTracking;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.TimeTracking;

[TestFixture]
public class TimeTrackingComputerTests
{
    private Mock<IDataTrackingStrategy> _dataTrackingStrategyMock = null!;
    private BehaviorSubject<(long IdentifiedSize, long ProcessedSize)> _dataSubject = null!;
    
    [SetUp]
    public void SetUp()
    {
        _dataTrackingStrategyMock = new Mock<IDataTrackingStrategy>();
        _dataSubject = new BehaviorSubject<(long IdentifiedSize, long ProcessedSize)>((0, 0));
        _dataTrackingStrategyMock.Setup(x => x.GetDataObservable()).Returns(_dataSubject);
    }
    
    [TearDown]
    public void TearDown()
    {
        _dataSubject.Dispose();
    }
    
    private TimeTrackingComputer CreateSut()
    {
        return new TimeTrackingComputer(_dataTrackingStrategyMock.Object);
    }

    private static void WaitFor(TimeSpan delay)
    {
        using var gate = new ManualResetEventSlim(false);
        gate.Wait(delay);
    }
    
    [Test]
    public void Constructor_ShouldInitialize_WithDefaultValues()
    {
        var sut = CreateSut();
        
        sut.Should().NotBeNull();
        sut.LastDataHandledDateTime.Should().BeNull();
    }
    
    [Test]
    public void Start_ShouldInitialize_TimeTrack()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        TimeTrack? capturedTimeTrack = null;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.StartDateTime.Should().BeCloseTo(startDateTime.LocalDateTime, TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public void Start_ShouldReset_PreviousData()
    {
        var sut = CreateSut();
        var firstStart = DateTimeOffset.Now.AddMinutes(-10);
        var secondStart = DateTimeOffset.Now;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(firstStart);
        _dataSubject.OnNext((1000, 500));
        WaitFor(TimeSpan.FromMilliseconds(50));
        
        sut.Start(secondStart);
        
        TimeTrack? capturedTimeTrack = null;
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.StartDateTime.Should().BeCloseTo(secondStart.LocalDateTime, TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public void Stop_ShouldStop_TimeTracking()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        var emissions = new List<TimeTrack>();
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Subscribe(tt => emissions.Add(tt));
        
        WaitFor(TimeSpan.FromMilliseconds(1500));
        
        sut.Stop();
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        emissions.Should().NotBeEmpty();
        var finalTimeTrack = emissions.Last();
        finalTimeTrack.RemainingTime.Should().Be(TimeSpan.Zero);
    }
    
    [Test]
    public void Stop_WhenStartDateTimeIsSet_ShouldCalculate_EstimatedEndDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        TimeTrack? capturedTimeTrack = null;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        sut.Stop();
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.EstimatedEndDateTime.Should().NotBeNull();
        capturedTimeTrack.RemainingTime.Should().Be(TimeSpan.Zero);
    }
    
    [Test]
    public void DataUpdate_WhenNotStarted_ShouldNotUpdate_TimeTrack()
    {
        var sut = CreateSut();
        var emissions = new List<TimeTrack>();
        
        using var subscription = sut.RemainingTime.Take(2).Subscribe(emissions.Add);
        
        _dataSubject.OnNext((1000, 100));
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        emissions.Should().BeEmpty();
    }
    
    [Test]
    public void DataUpdate_WhenStarted_ShouldUpdate_TimeTrack()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        var emissions = new List<TimeTrack>();
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Take(2).Subscribe(emissions.Add);
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        _dataSubject.OnNext((1000, 100));
        
        WaitFor(TimeSpan.FromMilliseconds(2100));
        
        sut.LastDataHandledDateTime.Should().NotBeNull();
        emissions.Should().NotBeEmpty();
    }
    
    [Test]
    public void DataUpdate_WithValidProgress_ShouldCalculate_EstimatedEndDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now.AddSeconds(-10);
        TimeTrack? capturedTimeTrack = null;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        _dataSubject.OnNext((1000, 100));
        
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.EstimatedEndDateTime.Should().NotBeNull();
        capturedTimeTrack.EstimatedEndDateTime.Should().BeAfter(DateTime.Now);
    }
    
    [Test]
    public void DataUpdate_WithZeroTotalData_ShouldNotCalculate_EstimatedEndDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        TimeTrack? capturedTimeTrack = null;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        _dataSubject.OnNext((0, 0));
        
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.EstimatedEndDateTime.Should().BeNull();
    }
    
    [Test]
    public void DataUpdate_WithZeroHandledData_ShouldNotCalculate_EstimatedEndDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        TimeTrack? capturedTimeTrack = null;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        _dataSubject.OnNext((1000, 0));
        
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.EstimatedEndDateTime.Should().BeNull();
    }
    
    [Test]
    public void DataUpdate_WithHandledDataGreaterThanTotal_ShouldNotCalculate_EstimatedEndDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        TimeTrack? capturedTimeTrack = null;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        _dataSubject.OnNext((1000, 1500));
        
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.EstimatedEndDateTime.Should().BeNull();
    }
    
    [Test]
    public void RemainingTime_WhenNotStarted_ShouldNotEmit()
    {
        var sut = CreateSut();
        var emissions = new List<TimeTrack>();
        
        using var subscription = sut.RemainingTime
            .Timeout(TimeSpan.FromMilliseconds(500))
            .Subscribe(
                tt => emissions.Add(tt),
                _ => { });
        
        WaitFor(TimeSpan.FromMilliseconds(600));
        
        emissions.Should().BeEmpty();
    }
    
    [Test]
    public void RemainingTime_WhenStarted_ShouldEmit_Periodically()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        var emissions = new ConcurrentQueue<TimeTrack>();
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime
            .Take(3)
            .Subscribe(tt => emissions.Enqueue(tt));
        
        var receivedTwoEmissions = SpinWait.SpinUntil(
            () => emissions.Count >= 2,
            TimeSpan.FromSeconds(5));
        
        receivedTwoEmissions.Should().BeTrue("remaining time should emit periodically while tracking is started");
        emissions.Should().HaveCountGreaterThanOrEqualTo(2);
        emissions.All(tt => tt.StartDateTime.HasValue).Should().BeTrue();
    }
    
    [Test]
    public void RemainingTime_AfterStop_ShouldStop_Emitting()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        var emissionCount = 0;
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Subscribe(_ => emissionCount++);
        
        WaitFor(TimeSpan.FromMilliseconds(2100));
        
        sut.Stop();
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        var countAfterStop = emissionCount;
        
        WaitFor(TimeSpan.FromMilliseconds(1500));
        
        var countAfterWait = emissionCount;
        
        countAfterWait.Should().Be(countAfterStop);
    }
    
    [Test]
    public void ProgressCalculation_WithHalfCompletion_ShouldEstimate_DoubleElapsedTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now.AddSeconds(-10);
        TimeTrack? capturedTimeTrack = null;
        var emissionReceived = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        _dataSubject.OnNext((1000, 500));
        
        using var subscription = sut.RemainingTime.Subscribe(tt =>
        {
            capturedTimeTrack = tt;
            emissionReceived.Set();
        });
        
        emissionReceived.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("observable should emit at least once");
        
        capturedTimeTrack.Should().NotBeNull();
        capturedTimeTrack!.EstimatedEndDateTime.Should().NotBeNull();
        
        var elapsedSeconds = (capturedTimeTrack.EstimatedEndDateTime!.Value - startDateTime.LocalDateTime).TotalSeconds;
        elapsedSeconds.Should().BeGreaterThan(15);
    }
    
    [Test]
    public void MultipleDataUpdates_ShouldUpdate_EstimatedEndDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now.AddSeconds(-5);
        var emissions = new List<TimeTrack>();
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Subscribe(tt => emissions.Add(tt));
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        _dataSubject.OnNext((1000, 100));
        WaitFor(TimeSpan.FromMilliseconds(1100));
        
        _dataSubject.OnNext((1000, 200));
        WaitFor(TimeSpan.FromMilliseconds(1100));
        
        _dataSubject.OnNext((1000, 300));
        WaitFor(TimeSpan.FromMilliseconds(1100));
        
        emissions.Should().HaveCountGreaterThanOrEqualTo(3);
        
        var emissionsWithEstimatedEnd = emissions.Where(e => e.EstimatedEndDateTime.HasValue).ToList();
        emissionsWithEstimatedEnd.Should().NotBeEmpty();
    }
    
    [Test]
    public void Start_ThenDataUpdate_ShouldSet_LastDataHandledDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        var beforeUpdate = DateTime.Now;
        
        sut.Start(startDateTime);
        
        WaitFor(TimeSpan.FromMilliseconds(50));
        
        _dataSubject.OnNext((1000, 100));
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        var afterUpdate = DateTime.Now;
        
        sut.LastDataHandledDateTime.Should().NotBeNull();
        sut.LastDataHandledDateTime!.Value.Should().BeOnOrAfter(beforeUpdate);
        sut.LastDataHandledDateTime!.Value.Should().BeOnOrBefore(afterUpdate);
    }
    
    [Test]
    public void RemainingTime_Observable_ShouldCombine_IntervalAndTimeTrack()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        var emissions = new List<TimeTrack>();
        
        sut.Start(startDateTime);
        
        _dataSubject.OnNext((1000, 250));
        
        using var subscription = sut.RemainingTime
            .Take(3)
            .Subscribe(tt => emissions.Add(tt));
        
        WaitFor(TimeSpan.FromMilliseconds(3500));
        
        emissions.Should().HaveCountGreaterThanOrEqualTo(2);
        emissions.Should().OnlyContain(tt => tt.StartDateTime.HasValue);
    }
    
    [Test]
    public void Stop_WithoutStart_ShouldNotThrow()
    {
        var sut = CreateSut();
        
        var act = () => sut.Stop();
        
        act.Should().NotThrow();
    }
    
    [Test]
    public void Constructor_ShouldSubscribe_ToDataTrackingStrategy()
    {
        CreateSut();
        
        _dataTrackingStrategyMock.Verify(x => x.GetDataObservable(), Times.Once);
    }
    
    [Test]
    public void Start_WithoutDataUpdate_ShouldNotCalculate_EstimatedEndDateTime()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now;
        var emissions = new List<TimeTrack>();
        var expectedEmissions = new ManualResetEvent(false);
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Take(2).Subscribe(
            onNext: tt => emissions.Add(tt),
            onCompleted: () => expectedEmissions.Set());
        
        expectedEmissions.WaitOne(TimeSpan.FromSeconds(3)).Should().BeTrue("observable should complete after 2 emissions");
        
        emissions.Should().HaveCount(2);
        emissions.All(tt => tt.EstimatedEndDateTime == null).Should().BeTrue();
    }
    
    [Test]
    public void CompleteProcess_FromStartToStop_ShouldUpdate_AllFields()
    {
        var sut = CreateSut();
        var startDateTime = DateTimeOffset.Now.AddSeconds(-2);
        var emissions = new List<TimeTrack>();
        
        sut.Start(startDateTime);
        
        using var subscription = sut.RemainingTime.Subscribe(tt => emissions.Add(tt));
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        _dataSubject.OnNext((1000, 250));
        WaitFor(TimeSpan.FromMilliseconds(1100));
        
        _dataSubject.OnNext((1000, 500));
        WaitFor(TimeSpan.FromMilliseconds(1100));
        
        _dataSubject.OnNext((1000, 750));
        WaitFor(TimeSpan.FromMilliseconds(1100));
        
        sut.Stop();
        
        WaitFor(TimeSpan.FromMilliseconds(100));
        
        emissions.Should().NotBeEmpty();
        var finalEmission = emissions.Last();
        finalEmission.StartDateTime.Should().NotBeNull();
        finalEmission.EstimatedEndDateTime.Should().NotBeNull();
        finalEmission.RemainingTime.Should().Be(TimeSpan.Zero);
        sut.LastDataHandledDateTime.Should().NotBeNull();
    }
}
