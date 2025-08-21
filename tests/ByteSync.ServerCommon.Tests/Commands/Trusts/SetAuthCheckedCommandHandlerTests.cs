using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.ServerCommon.Tests.Commands.Trusts;

[TestFixture]
public class SetAuthCheckedCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenMemberExists_AddsCheckedClientInstanceId()
    {
        // Arrange
        var sessionId = "session-1";
        var callerInstanceId = "caller-instance";
        var checkedInstanceId = "checked-instance";

        var cloudSession = new CloudSessionData
        {
            SessionId = sessionId,
            SessionMembers =
            [
                new SessionMemberData
                {
                    ClientInstanceId = callerInstanceId,
                    ClientId = "caller-client",
                    CloudSessionData = new CloudSessionData { SessionId = sessionId }
                }
            ]
        };

        var repo = new Mock<ICloudSessionsRepository>();
        repo.Setup(r => r.Update(sessionId, It.IsAny<Func<CloudSessionData, bool>>(), null, null))
            .Returns<string, Func<CloudSessionData, bool>, object, object>((id, updater, _, _) =>
            {
                var result = updater(cloudSession);
                return Task.FromResult(new ServerCommon.Business.Repositories.UpdateEntityResult<CloudSessionData>(cloudSession,
                    result ? ServerCommon.Business.Repositories.UpdateEntityStatus.Saved : ServerCommon.Business.Repositories.UpdateEntityStatus.NoOperation));
            });

        var logger = new Mock<ILogger<SetAuthCheckedCommandHandler>>();
        var handler = new SetAuthCheckedCommandHandler(repo.Object, logger.Object);

        var parameters = new SetAuthCheckedParameters { SessionId = sessionId, CheckedClientInstanceId = checkedInstanceId };
        var client = new Client { ClientInstanceId = callerInstanceId, ClientId = "caller-client" };
        var request = new SetAuthCheckedRequest(parameters, client);

        // Act
        await handler.Handle(request, default);

        // Assert
        var member = cloudSession.FindMember(callerInstanceId);
        member.Should().NotBeNull();
        member!.AuthCheckClientInstanceIds.Should().Contain(checkedInstanceId);
    }

    [Test]
    public async Task Handle_WhenMemberNotFound_DoesNotUpdate()
    {
        // Arrange
        var sessionId = "session-1";
        var callerInstanceId = "unknown-instance";
        var checkedInstanceId = "checked-instance";

        var cloudSession = new CloudSessionData { SessionId = sessionId };

        var repo = new Mock<ICloudSessionsRepository>();
        var wasUpdated = false;
        repo.Setup(r => r.Update(sessionId, It.IsAny<Func<CloudSessionData, bool>>(), null, null))
            .Returns<string, Func<CloudSessionData, bool>, object, object>((id, updater, _, _) =>
            {
                wasUpdated = updater(cloudSession);
                return Task.FromResult(new ServerCommon.Business.Repositories.UpdateEntityResult<CloudSessionData>(cloudSession,
                    wasUpdated ? ServerCommon.Business.Repositories.UpdateEntityStatus.Saved : ServerCommon.Business.Repositories.UpdateEntityStatus.NoOperation));
            });

        var logger = new Mock<ILogger<SetAuthCheckedCommandHandler>>();
        var handler = new SetAuthCheckedCommandHandler(repo.Object, logger.Object);

        var parameters = new SetAuthCheckedParameters { SessionId = sessionId, CheckedClientInstanceId = checkedInstanceId };
        var client = new Client { ClientInstanceId = callerInstanceId, ClientId = "caller-client" };
        var request = new SetAuthCheckedRequest(parameters, client);

        // Act
        await handler.Handle(request, default);

        // Assert
        wasUpdated.Should().BeFalse();
        cloudSession.SessionMembers.Should().BeEmpty();
    }
}