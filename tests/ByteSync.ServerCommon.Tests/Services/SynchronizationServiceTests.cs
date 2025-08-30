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
    
}