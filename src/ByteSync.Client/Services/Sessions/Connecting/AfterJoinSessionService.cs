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
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly IQuitSessionService _quitSessionService;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly ILogger<AfterJoinSessionService> _logger;

    public AfterJoinSessionService(
        ICloudSessionApiClient cloudSessionApiClient,
        ISessionService sessionService,
        ISessionMemberService sessionMemberService,
        IDataSourceService dataSourceService,
        IDataEncrypter dataEncrypter,
        ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        IInventoryApiClient inventoryApiClient,
        IDigitalSignaturesRepository digitalSignaturesRepository,
        IEnvironmentService environmentService,
        ICloudSessionConnectionService cloudSessionConnectionService,
        IQuitSessionService quitSessionService,
        IDataSourceRepository dataSourceRepository,
        ILogger<AfterJoinSessionService> logger)
    {
        _cloudSessionApiClient = cloudSessionApiClient;
        _sessionService = sessionService;
        _sessionMemberService = sessionMemberService;
        _dataSourceService = dataSourceService;
        _dataEncrypter = dataEncrypter;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _inventoryApiClient = inventoryApiClient;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _environmentService = environmentService;
        _cloudSessionConnectionService = cloudSessionConnectionService;
        _quitSessionService = quitSessionService;
        _dataSourceRepository = dataSourceRepository;
        _logger = logger;
    }
    
    public async Task Process(AfterJoinSessionRequest request)
    {
        var sessionMemberInfoDtos = await _cloudSessionApiClient.GetMembers(request.CloudSessionResult.SessionId);
        
        await CheckOtherMembersAreTrustedAndChecked(request, sessionMemberInfoDtos);

        var sessionSettings = _dataEncrypter.DecryptSessionSettings(request.CloudSessionResult.SessionSettings);

        var password = await GetPassword(request);
        await _sessionService.SetCloudSession(request.CloudSessionResult.CloudSession, request.RunCloudSessionProfileInfo, sessionSettings, password);
        
        _sessionMemberService.AddOrUpdate(sessionMemberInfoDtos);
        
        await FillPathItems(request, sessionMemberInfoDtos);
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
    
    private async Task FillPathItems(AfterJoinSessionRequest request, List<SessionMemberInfoDTO> sessionMemberInfoDtos)
    {
        if (request.RunCloudSessionProfileInfo != null)
        {
            var myPathItems = request.RunCloudSessionProfileInfo.GetMyPathItems();
            
            foreach (var pathItem in myPathItems)
            {
                await _dataSourceService.CreateAndTryAddDataSource(pathItem.Path, pathItem.Type);
            }
        }

        foreach (var sessionMemberInfo in sessionMemberInfoDtos)
        {
            if (!sessionMemberInfo.HasClientInstanceId(_environmentService.ClientInstanceId))
            {
                var encryptedPathItems = await _inventoryApiClient.GetPathItems(request.CloudSessionResult.SessionId, sessionMemberInfo.ClientInstanceId);

                if (encryptedPathItems != null)
                {
                    foreach (var encryptedPathItem in encryptedPathItems)
                    {
                        var pathItem = _dataEncrypter.DecryptPathItem(encryptedPathItem);
                        await _dataSourceService.TryAddDataSource(pathItem);
                    }
                }
            }
        }

        AddDebugPathItems();
    }

    private void AddDebugPathItems()
    {
        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTA))
        {
            DebugAddDesktopPathItem("testA");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTA1))
        {
            DebugAddDesktopPathItem("testA1");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTB))
        {
            DebugAddDesktopPathItem("testB");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTB1))
        {
            DebugAddDesktopPathItem("testB1");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTC))
        {
            DebugAddDesktopPathItem("testC");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTC1))
        {
            DebugAddDesktopPathItem("testC1");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTD))
        {
            DebugAddDesktopPathItem("testD");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTD1))
        {
            DebugAddDesktopPathItem("testD1");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTE))
        {
            DebugAddDesktopPathItem("testE");
        }

        if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTE1))
        {
            DebugAddDesktopPathItem("testE1");
        }
    }
    
    private void DebugAddDesktopPathItem(string folderName)
    {
        var myPathItems = _dataSourceRepository.Elements.Where(pi => pi.ClientInstanceId == _environmentService.ClientInstanceId).ToList();
                
        if (myPathItems.Any(pi => pi.Path.Equals(IOUtils.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), folderName), 
                StringComparison.InvariantCultureIgnoreCase)))
        {
            return;
        }

        _dataSourceService.CreateAndTryAddDataSource(
            IOUtils.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), folderName), 
            FileSystemTypes.Directory);
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