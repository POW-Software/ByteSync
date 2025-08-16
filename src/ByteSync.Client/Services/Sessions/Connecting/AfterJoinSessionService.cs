using System.Text;
using ByteSync.Business.Arguments;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

public class AfterJoinSessionService : IAfterJoinSessionService
{
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly IDataNodeService _dataNodeService;
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly IQuitSessionService _quitSessionService;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly ISessionMemberApiClient _sessionMemberApiClient;
    private readonly ILogger<AfterJoinSessionService> _logger;

    public AfterJoinSessionService(
        ISessionService sessionService,
        ISessionMemberService sessionMemberService,
        IDataNodeService dataNodeService,  
        IDataSourceService dataSourceService,
        IDataEncrypter dataEncrypter,
        ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        IInventoryApiClient inventoryApiClient,
        IDigitalSignaturesRepository digitalSignaturesRepository,
        IEnvironmentService environmentService,
        ICloudSessionConnectionService cloudSessionConnectionService,
        IQuitSessionService quitSessionService,
        IDataSourceRepository dataSourceRepository,
        IDataNodeRepository dataNodeRepository,
        ISessionMemberApiClient sessionMemberApiClient,
        ILogger<AfterJoinSessionService> logger)
    {
        _sessionService = sessionService;
        _sessionMemberService = sessionMemberService;
        _dataNodeService = dataNodeService;
        _dataSourceService = dataSourceService;
        _dataEncrypter = dataEncrypter;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _inventoryApiClient = inventoryApiClient;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _environmentService = environmentService;
        _cloudSessionConnectionService = cloudSessionConnectionService;
        _quitSessionService = quitSessionService;
        _dataSourceRepository = dataSourceRepository;
        _dataNodeRepository = dataNodeRepository;
        _sessionMemberApiClient = sessionMemberApiClient;
        _logger = logger;
    }
    
    public async Task Process(AfterJoinSessionRequest request)
    {
        var sessionMemberInfoDtos = await _sessionMemberApiClient.GetMembers(request.CloudSessionResult.SessionId);
        
        await CheckOtherMembersAreTrustedAndChecked(request, sessionMemberInfoDtos);

        var sessionSettings = _dataEncrypter.DecryptSessionSettings(request.CloudSessionResult.SessionSettings);

        var password = await GetPassword(request);
        await _sessionService.SetCloudSession(request.CloudSessionResult.CloudSession, request.RunCloudSessionProfileInfo, sessionSettings, password);
        
        _sessionMemberService.AddOrUpdate(sessionMemberInfoDtos);

        await FillDataNodes(request, sessionMemberInfoDtos);
        
        await FillDataSources(request, sessionMemberInfoDtos);
    }

    private async Task CheckOtherMembersAreTrustedAndChecked(AfterJoinSessionRequest request, List<SessionMemberInfoDTO> sessionMemberInfoDtos)
    {
        var areAllMemberAuthOK = true;
        foreach (var sessionMemberInfo in sessionMemberInfoDtos)
        {
            if (!sessionMemberInfo.HasClientInstanceId(_environmentService.ClientInstanceId))
            {
                if (! await _digitalSignaturesRepository.IsAuthChecked(request.CloudSessionResult.SessionId, sessionMemberInfo))
                {
                    _logger.LogWarning("Digital Signature not checked for Client {ClientInstanceId}", sessionMemberInfo.ClientInstanceId);
                    areAllMemberAuthOK = false;
                }
            }
        }

        if (!areAllMemberAuthOK)
        {
            _logger.LogWarning("Auth check failed, quitting session");
            
            await _cloudSessionConnectionService.ClearConnectionData();
            
            await _quitSessionService.Process();
            
            throw new Exception("Auth check failed, quitting session");
        }
    }

    private async Task<string> GetPassword(AfterJoinSessionRequest request)
    {
        string password;
        if (request.IsCreator)
        {
            password = GeneratePassword();
        }
        else
        {
            password = (await _cloudSessionConnectionRepository.GetTempSessionPassword(request.CloudSessionResult.SessionId))!;
        }

        return password;
    }
    
    private async Task FillDataNodes(AfterJoinSessionRequest request, List<SessionMemberInfoDTO> sessionMemberInfoDtos)
    {
        await _dataNodeService.CreateAndTryAddDataNode();
        
        foreach (var sessionMemberInfo in sessionMemberInfoDtos)
        {
            if (!sessionMemberInfo.HasClientInstanceId(_environmentService.ClientInstanceId))
            {
                var encryptedDataNodes = await _inventoryApiClient.GetDataNodes(request.CloudSessionResult.SessionId, sessionMemberInfo.ClientInstanceId);

                if (encryptedDataNodes != null)
                {
                    foreach (var encryptedDataNode in encryptedDataNodes)
                    {
                        var dataNode = _dataEncrypter.DecryptDataNode(encryptedDataNode);
                        await _dataNodeService.TryAddDataNode(dataNode);
                    }
                }
            }
        }
    }
    
    private async Task FillDataSources(AfterJoinSessionRequest request, List<SessionMemberInfoDTO> sessionMemberInfoDtos)
    {
        if (request.RunCloudSessionProfileInfo != null)
        {
            var myDataSources = request.RunCloudSessionProfileInfo.GetMyDataSources();
            
            foreach (var dataSource in myDataSources)
            {
                // await _dataSourceService.CreateAndTryAddDataSource(dataSource.Path, dataSource.Type);
            }
        }

        foreach (var sessionMemberInfo in sessionMemberInfoDtos)
        {
            if (!sessionMemberInfo.HasClientInstanceId(_environmentService.ClientInstanceId))
            {
                // Get all data nodes for this session member to retrieve their data sources
                var dataNodes = _dataNodeRepository.GetDataNodesByClientInstanceId(sessionMemberInfo.ClientInstanceId);
                
                foreach (var dataNode in dataNodes)
                {
                    var encryptedDataSources = await _inventoryApiClient.GetDataSources(request.CloudSessionResult.SessionId, sessionMemberInfo.ClientInstanceId, dataNode.Id);

                    if (encryptedDataSources != null)
                    {
                        foreach (var encryptedDataSource in encryptedDataSources)
                        {
                            var dataSource = _dataEncrypter.DecryptDataSource(encryptedDataSource);
                            await _dataSourceService.TryAddDataSource(dataSource);
                        }
                    }
                }
            }
        }

        AddDebugDataSources();
    }

    private void AddDebugDataSources()
    {
        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTA))
        {
            DebugAddDesktopDataSource("testA");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTA1))
        {
            DebugAddDesktopDataSource("testA1");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTB))
        {
            DebugAddDesktopDataSource("testB");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTB1))
        {
            DebugAddDesktopDataSource("testB1");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTC))
        {
            DebugAddDesktopDataSource("testC");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTC1))
        {
            DebugAddDesktopDataSource("testC1");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTD))
        {
            DebugAddDesktopDataSource("testD");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTD1))
        {
            DebugAddDesktopDataSource("testD1");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTE))
        {
            DebugAddDesktopDataSource("testE");
        }

        if (_environmentService.Arguments.Contains(DebugArguments.ADD_DATASOURCE_TESTE1))
        {
            DebugAddDesktopDataSource("testE1");
        }
    }
    
    private void DebugAddDesktopDataSource(string folderName)
    {
        var myDataSources = _dataSourceRepository.Elements.Where(ds => ds.ClientInstanceId == _environmentService.ClientInstanceId).ToList();

        string baseFolderPath = IOUtils.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ByteSync_Tests");
        
        if (myDataSources.Any(ds => ds.Path.Equals(IOUtils.Combine(baseFolderPath, folderName), 
                StringComparison.InvariantCultureIgnoreCase)))
        {
            return;
        }

        var dataNode = _dataNodeRepository.CurrentMemberDataNodes.Items.FirstOrDefault();
        if (dataNode == null)
        {
            return;
        }

        _dataSourceService.CreateAndTryAddDataSource(
            IOUtils.Combine(baseFolderPath, folderName), 
            FileSystemTypes.Directory, dataNode);
    }

    private string GeneratePassword()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 5; i++)
        {
            sb.Append(RandomUtils.GetRandomLetter(true));
        }
        
        return sb.ToString();
    }
}