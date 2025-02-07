using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Repositories;
using ByteSync.Tests.TestUtilities.Helpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

// ReSharper disable PossibleMultipleEnumeration

namespace ByteSync.Tests.Controls.Repositories;

[TestFixture]
public class SessionMembersRepositoryTests
{
    private Mock<IConnectionService> _mockConnectionService;
    private Mock<ISessionInvalidationSourceCachePolicy<SessionMemberInfo, string>> _mockSessionInvalidationCachePolicy;
    
    private SessionMemberRepository _sessionMemberRepository;


    private const double WAIT_TIME = 2.5f;

    [SetUp]
    public void SetUp()
    {
        _mockConnectionService = new Mock<IConnectionService>();
        _mockSessionInvalidationCachePolicy = new Mock<ISessionInvalidationSourceCachePolicy<SessionMemberInfo, string>>();

        _sessionMemberRepository = new SessionMemberRepository(
            _mockConnectionService.Object,
            _mockSessionInvalidationCachePolicy.Object
        );
    }

    [Test]
    public void GetSessionMember_ReturnsCorrectSessionMemberInfo_WhenInstanceExists()
    {
        // Arrange
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);

        // Act
        var result = _sessionMemberRepository.GetElement("CID1");
        
        // Assert
        result.Should().NotBeNull();
        result!.ClientInstanceId.Should().Be("CID1");
    }

    [Test]
    public void GetSessionMember_ReturnsNull_WhenInstanceDoesNotExist()
    {
        // Act
        var result = _sessionMemberRepository.GetElement("null");

        // Assert
        result.Should().BeNull();
    }
    
    [Test]
    public void SortedSessionMembers_ReturnsCorrectSessionMemberInfo_WhenInstanceExists()
    {
        // Arrange
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);

        // Act
        var result = _sessionMemberRepository.SortedSessionMembers;

        // Assert
        result.Should().NotBeNull();
        var list = result.ToList();
        list.Count.Should().Be(1);
        list.First().ClientInstanceId.Should().Be("CID1");
    }
    
    [Test]
    public void SortedOtherSessionMembers_ReturnsEmpty_WhenSessionMemberIsCurrentUser()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        var result = _sessionMemberRepository.SortedOtherSessionMembers;

        // Assert
        result.Should().NotBeNull();
        var list = result.ToList();
        list.Count.Should().Be(0);
    }
    
    [Test]
    public void SortedOtherSessionMembers_ReturnsNonEmpty_WhenSessionMemberIsCurrentUser()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        var result = _sessionMemberRepository.SortedOtherSessionMembers;

        // Assert
        result.Should().NotBeNull();
        var list = result.ToList();
        list.Count.Should().Be(1);
        list.First().ClientInstanceId.Should().Be("CID2");
    }
    
    [Test]
    public async Task IsCurrentUserFirstSessionMember_ReturnsTrue_WhenCurrentUserIsFirstSessionMember()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        var resultObservable = _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable;
        var result = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

        // Assert
        resultObservable.Should().NotBeNull();
        
        var value = await resultObservable
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(WAIT_TIME));
        value.Should().BeTrue();
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsCurrentUserFirstSessionMember_ReturnsFalse_WhenEmpty()
    {
        // Arrange
        
        // Act
        var resultObservable = _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable;
        var result = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

        // Assert
        resultObservable.Should().NotBeNull();
        
        async Task Act() => await resultObservable
            .Timeout(TimeSpan.FromSeconds(2.5))
            .FirstAsync();
        Assert.ThrowsAsync<TimeoutException>(Act);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public async Task IsCurrentUserFirstSessionMember_ReturnsTrue_WhenCurrentUserIsFirstSessionMember_2A()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"), 
            JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10)
        };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2"),
            JoinedSessionOn = DateTimeOffset.Now
        };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        var resultObservable = _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable;
        var result = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

        // Assert
        resultObservable.Should().NotBeNull();
        var value = await resultObservable
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(WAIT_TIME));
        value.Should().BeTrue();
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task IsCurrentUserFirstSessionMember_ReturnsTrue_WhenCurrentUserIsFirstSessionMember_3()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"),
            JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10)
        };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2"),
            JoinedSessionOn = DateTimeOffset.Now
        };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        var resultObservable = _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable;
        var result = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

        // Assert
        resultObservable.Should().NotBeNull();
        var value = await resultObservable
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(WAIT_TIME));
        value.Should().BeTrue();
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task IsCurrentUserFirstSessionMember_ReturnsTrue_WhenCurrentUserIsFirstSessionMember_4()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        _sessionMemberRepository.Remove("CID2");
        
        // Act
        var resultObservable = _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable;
        var result = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

        // Assert
        resultObservable.Should().NotBeNull();
        var value = await resultObservable
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(WAIT_TIME));
        value.Should().BeTrue();
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task IsCurrentUserFirstSessionMember_ReturnsFalse_WhenCurrentUserIsNotFirstSessionMember()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2") };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        var resultObservable = _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable;
        var result = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

        // Assert
        resultObservable.Should().NotBeNull();
        var value = await resultObservable
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(WAIT_TIME));
        value.Should().BeFalse();
        result.Should().BeFalse();
    }
    
    [Test]
    public async Task IsCurrentUserFirstSessionMember_ReturnsFalse_WhenCurrentUserIsNotFirstSessionMember_2()
    {
        // Arrange
        _mockConnectionService.SetupGet(x => x.ClientInstanceId).Returns("CID1");
        var sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID2"), 
            JoinedSessionOn = DateTimeOffset.Now.AddSeconds(-10)
        };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID1"),
            JoinedSessionOn = DateTimeOffset.Now
        };
        _sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        var resultObservable = _sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable;
        var result = _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

        // Assert
        resultObservable.Should().NotBeNull();
        var value = await resultObservable
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(WAIT_TIME));
        value.Should().BeFalse();
        result.Should().BeFalse();
    }
}