using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class SynchronizationServiceTests
{
    private ICloudSessionsRepository _cloudSessionsRepository;
    private ISynchronizationRepository _synchronizationRepository;
    private ITrackingActionRepository _trackingActionRepository;
    private ISynchronizationProgressService _synchronizationProgressService;
    private ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private ILogger<SynchronizationService> _logger;
    
    private SynchronizationService _synchronizationService;

    [SetUp]
    public void Setup()
    {
        _cloudSessionsRepository = A.Fake<ICloudSessionsRepository>(x => x.Strict());
        _synchronizationRepository = A.Fake<ISynchronizationRepository>(x => x.Strict());
        _trackingActionRepository = A.Fake<ITrackingActionRepository>(x => x.Strict());
        _synchronizationProgressService = A.Fake<ISynchronizationProgressService>(x => x.Strict());
        _synchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>(x => x.Strict());
        _logger = A.Fake<ILogger<SynchronizationService>>();
        
        _synchronizationService = new SynchronizationService(_cloudSessionsRepository, _synchronizationRepository, _trackingActionRepository, 
            _synchronizationProgressService, _synchronizationStatusCheckerService, _logger); 
    }

    [Test]
    public async Task GetSynchronization_WhenSynchronizationNotExists_ReturnsNull()
    {
        // Arrange
        var client = new Client();
        var sessionId = "sessionId";

        A.CallTo(() => _synchronizationRepository.Get(A<string>.Ignored))
            .Returns((SynchronizationEntity?)null);
        
        // Act
        var result = await _synchronizationService.GetSynchronization(sessionId, client);
        
        // Assert
        result.Should().BeNull();
        A.CallTo(() => _synchronizationRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task GetSynchronization_WhenSynchronizationExistsAndClientMatches_ReturnsSynchronization()
    {
        // Arrange
        var client = new Client();
        client.ClientInstanceId = "clientInstanceId";
        var sessionId = "sessionId";

        var synchronizationEntity = new SynchronizationEntity();
        synchronizationEntity.Progress.Members.Add(client.ClientInstanceId);
        var synchronization = new Synchronization();

        A.CallTo(() => _synchronizationRepository.Get(A<string>.Ignored))
            .Returns(synchronizationEntity);
        A.CallTo(() => _synchronizationProgressService.MapToSynchronization(synchronizationEntity))
            .Returns(synchronization);
        
        // Act
        var result = await _synchronizationService.GetSynchronization(sessionId, client);
        
        // Assert
        result.Should().BeSameAs(synchronization);
        A.CallTo(() => _synchronizationRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationProgressService.MapToSynchronization(synchronizationEntity)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task StartSynchronization()
    {
        // Arrange
        var client = new Client();
        client.ClientInstanceId = "clientInstanceId";
        var sessionId = "sessionId";

        var synchronizationEntity = new SynchronizationEntity();
        synchronizationEntity.Progress.Members.Add(client.ClientInstanceId);
        var synchronization = new Synchronization();

        A.CallTo(() => _synchronizationRepository.Get(A<string>.Ignored))
            .Returns(synchronizationEntity);
        A.CallTo(() => _synchronizationProgressService.MapToSynchronization(synchronizationEntity))
            .Returns(synchronization);
        
        // Act
        var result = await _synchronizationService.GetSynchronization(sessionId, client);
        
        // Assert
        result.Should().BeSameAs(synchronization);
        A.CallTo(() => _synchronizationRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationProgressService.MapToSynchronization(synchronizationEntity)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task StartSynchronization_WhenNoExistingSynchronization_CreatesNewSynchronization()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupDefinitions = new List<ActionsGroupDefinition> { new ActionsGroupDefinition() };
        var session = new CloudSessionData();
        var synchronization = new Synchronization();

        A.CallTo(() => _synchronizationRepository.Get(sessionId)).Returns((SynchronizationEntity?)null);
        A.CallTo(() => _cloudSessionsRepository.Get(sessionId)).Returns(session);
        A.CallTo(() => _synchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, actionsGroupDefinitions))
            .Returns(Task.CompletedTask);
        A.CallTo(() => _synchronizationProgressService.InformSynchronizationStarted(A<SynchronizationEntity>._, client))
            .Returns(synchronization);
        
        // Act
        var result = await _synchronizationService.StartSynchronization(sessionId, client, actionsGroupDefinitions);

        // Assert
        result.Should().BeSameAs(synchronization);
        A.CallTo(() => _synchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, actionsGroupDefinitions)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationProgressService.InformSynchronizationStarted(A<SynchronizationEntity>._, client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task StartSynchronization_WhenSynchronizationExists_ReturnsExistingSynchronization()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var actionsGroupDefinitions = new List<ActionsGroupDefinition> { new ActionsGroupDefinition() };
        var synchronizationEntity = new SynchronizationEntity();
        var synchronization = new Synchronization();

        A.CallTo(() => _synchronizationRepository.Get(sessionId)).Returns(synchronizationEntity);
        A.CallTo(() => _synchronizationProgressService.MapToSynchronization(synchronizationEntity))
            .Returns(synchronization);

        // Act
        var result = await _synchronizationService.StartSynchronization(sessionId, client, actionsGroupDefinitions);

        // Assert
        result.Should().BeSameAs(synchronization);
        A.CallTo(() => _synchronizationRepository.AddSynchronization(A<SynchronizationEntity>._, actionsGroupDefinitions)).MustNotHaveHappened();
        A.CallTo(() => _synchronizationProgressService.MapToSynchronization(synchronizationEntity)).MustHaveHappened();
    }
    
    [Test]
    public async Task OnUploadIsFinishedAsync_WhenCheckSynchronizationSuccess_RunsNormally()
    {
        // Arrange
        var sessionId = "sessionId";
        var client = new Client();
        var sharedFileDefinition = new SharedFileDefinition { SessionId = sessionId };

        TrackingActionEntity trackingActionEntity = new TrackingActionEntity();
        trackingActionEntity.TargetClientInstanceIds.Add("targetClientInstanceId");
        SynchronizationEntity synchronizationEntity = new SynchronizationEntity();

        A.CallTo(() => _trackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .Invokes((string _, List<string> _, Func<TrackingActionEntity, SynchronizationEntity, bool> func) => func(trackingActionEntity, synchronizationEntity))
            .Returns(new TrackingActionResult(true, new List<TrackingActionEntity>(), synchronizationEntity));
            
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .Returns(true);

        A.CallTo(() => _synchronizationProgressService.UploadIsFinished(sharedFileDefinition, 1, A<HashSet<string>>.That.Contains("targetClientInstanceId")))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationService.OnUploadIsFinishedAsync(sharedFileDefinition, 1, client);

        // Assert
        A.CallTo(() => _trackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationProgressService.UploadIsFinished(sharedFileDefinition, 1, A<HashSet<string>>.That.Contains("targetClientInstanceId")))
            .MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task OnUploadIsFinishedAsync_WhenCheckSynchronizationFails_Aborts()
    {
        // Arrange
        var sessionId = "sessionId";
        var client = new Client();
        var sharedFileDefinition = new SharedFileDefinition { SessionId = sessionId };

        TrackingActionEntity trackingActionEntity = new TrackingActionEntity();
        trackingActionEntity.TargetClientInstanceIds.Add("targetClientInstanceId");
        SynchronizationEntity synchronizationEntity = new SynchronizationEntity();

        A.CallTo(() => _trackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .Invokes((string _, List<string> _, Func<TrackingActionEntity, SynchronizationEntity, bool> func) => func(trackingActionEntity, synchronizationEntity))
            .Returns(new TrackingActionResult(false, new List<TrackingActionEntity>(), synchronizationEntity));
            
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .Returns(false);

        // Act
        await _synchronizationService.OnUploadIsFinishedAsync(sharedFileDefinition, 1, client);

        // Assert
        A.CallTo(() => _trackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationProgressService.UploadIsFinished(sharedFileDefinition, 1, A<HashSet<string>>.That.Contains("targetClientInstanceId")))
            .MustNotHaveHappened();
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
        trackingActionEntity.TargetClientInstanceIds.Add("targetClientInstanceId");
        SynchronizationEntity synchronizationEntity = new SynchronizationEntity();

        A.CallTo(() => _trackingActionRepository.GetOrBuild(sessionId, "ActionGroupId"))
            .Returns(trackingActionEntity);
            
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .Returns(true);

        A.CallTo(() => _synchronizationProgressService.FilePartIsUploaded(sharedFileDefinition, 1, A<HashSet<string>>.That.Contains("targetClientInstanceId")))
            .Returns(Task.CompletedTask);
        
        A.CallTo(() => _synchronizationRepository.Get(sessionId))
            .Returns(synchronizationEntity);

        // Act
        await _synchronizationService.OnFilePartIsUploadedAsync(sharedFileDefinition, 1);
        
        A.CallTo(() => _trackingActionRepository.GetOrBuild(sessionId, "ActionGroupId"))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationProgressService.FilePartIsUploaded(sharedFileDefinition, 1, A<HashSet<string>>.That.Contains("targetClientInstanceId")))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationRepository.Get(sessionId))
            .MustHaveHappenedOnceExactly();
    }
}