using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FakeItEasy;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class SynchronizationServiceTests
{
    private ISynchronizationRepository _synchronizationRepository;
    private ITrackingActionRepository _trackingActionRepository;
    private ISynchronizationProgressService _synchronizationProgressService;
    private ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    
    private SynchronizationService _synchronizationService;

    [SetUp]
    public void Setup()
    {
        _synchronizationRepository = A.Fake<ISynchronizationRepository>(x => x.Strict());
        _trackingActionRepository = A.Fake<ITrackingActionRepository>(x => x.Strict());
        _synchronizationProgressService = A.Fake<ISynchronizationProgressService>(x => x.Strict());
        _synchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>(x => x.Strict());
        
        _synchronizationService = new SynchronizationService(_synchronizationRepository, _trackingActionRepository, 
            _synchronizationProgressService, _synchronizationStatusCheckerService); 
    }
    
    [Test]
    public async Task OnFilePartIsUploadedAsync_WhenCheckSynchronizationSuccess_RunsNormally()
    {
        // Arrange
        var sessionId = "sessionId";
        var sharedFileDefinition = new SharedFileDefinition
        {
            SessionId = sessionId, 
            ActionsGroupIds = new List<string> { "ActionGroupId" }
        };

        TrackingActionEntity trackingActionEntity = new TrackingActionEntity();
        trackingActionEntity.TargetClientInstanceAndNodeIds.Add("targetClientInstanceId_nodeId");
        SynchronizationEntity synchronizationEntity = new SynchronizationEntity();

        A.CallTo(() => _trackingActionRepository.GetOrThrow(sessionId, "ActionGroupId"))
            .Returns(trackingActionEntity);
            
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .Returns(true);

        A.CallTo(() => _synchronizationProgressService.FilePartIsUploaded(sharedFileDefinition, 1, A<HashSet<string>>.That.Contains("targetClientInstanceId_nodeId")))
            .Returns(Task.CompletedTask);
        
        A.CallTo(() => _synchronizationRepository.Get(sessionId))
            .Returns(synchronizationEntity);

        // Act
        await _synchronizationService.OnFilePartIsUploadedAsync(sharedFileDefinition, 1);
        
        A.CallTo(() => _trackingActionRepository.GetOrThrow(sessionId, "ActionGroupId"))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationProgressService.FilePartIsUploaded(sharedFileDefinition, 1, A<HashSet<string>>.That.Contains("targetClientInstanceId_nodeId")))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationRepository.Get(sessionId))
            .MustHaveHappenedOnceExactly();
    }
}