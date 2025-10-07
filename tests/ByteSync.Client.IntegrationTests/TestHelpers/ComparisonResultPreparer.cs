using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Inventories;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public class ComparisonResultPreparer
{
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    
    public ComparisonResultPreparer(ICloudSessionLocalDataManager cloudSessionLocalDataManager)
    {
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        
        InventoryDatas = new List<InventoryData>();
        BaseInventoryFiles = new List<string>();
        FullInventoryFiles = new List<string>();
    }
    
    public SessionSettings SessionSettings { get; private set; }
    
    public List<InventoryData> InventoryDatas { get; }
    
    public List<string> BaseInventoryFiles { get; }
    
    public List<string> FullInventoryFiles { get; }
    
    public async Task<ComparisonResult> BuildAndCompare(SessionSettings sessionSettings, params InventoryData[] inventoryDatas)
    {
        SessionSettings = sessionSettings;
        
        InventoryDatas.Clear();
        foreach (var inventoryData in inventoryDatas)
        {
            inventoryData.InventoryBuilder = null;
            
            Add(inventoryData);
        }
        
        BaseInventoryFiles.Clear();
        FullInventoryFiles.Clear();
        
        return await DoBuildAndCompare();
    }
    
    private void Add(InventoryData inventoryData)
    {
        InventoryDatas.Add(inventoryData);
        
        var cLetter = (char)('A' + InventoryDatas.IndexOf(inventoryData));
        
        var letter = cLetter.ToString();
        
        inventoryData.SetLetter(letter);
    }
    
    private async Task<ComparisonResult> DoBuildAndCompare()
    {
        foreach (var inventoryData in InventoryDatas)
        {
            var endpoint = new ByteSyncEndpoint
            {
                ClientId = $"CI_{inventoryData.Letter}",
                ClientInstanceId = $"CII_{inventoryData.Letter}",
                IpAddress = "localhost",
                OSPlatform = OSPlatforms.Windows
            };
            
            var sessionMemberInfo = new SessionMember
            {
                Endpoint = endpoint,
                PositionInList = inventoryData.Letter[0] - 'A',
                PrivateData = new()
                {
                    MachineName = "Machine" + inventoryData.Letter
                }
            };
            
            var dataNode = new DataNode
            {
                Id = Guid.NewGuid().ToString(),
                ClientInstanceId = sessionMemberInfo.ClientInstanceId,
                Code = inventoryData.Letter + "1"
            };
            
            var loggerMock = new Mock<ILogger<InventoryBuilder>>();
            
            var inventoryBuilder = new InventoryBuilder(sessionMemberInfo, dataNode, SessionSettings, new InventoryProcessData(),
                OSPlatforms.Windows, FingerprintModes.Rsync, loggerMock.Object,
                new InventoryFileAnalyzerFactory(),
                new InventorySaverFactory(),
                new InventoryIndexerFactory());
            
            foreach (var dataSource in inventoryData.DataSources)
            {
                inventoryBuilder.AddInventoryPart(dataSource);
            }
            
            inventoryData.InventoryBuilder = inventoryBuilder;
            
            var inventoryFile =
                _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventoryData.Inventory, LocalInventoryModes.Base);
            BaseInventoryFiles.Add(inventoryFile);
            
            await inventoryBuilder.BuildBaseInventoryAsync(inventoryFile);
        }
        
        foreach (var inventoryData in InventoryDatas)
        {
            var initialStatusBuilder = new InitialStatusBuilder();
            using var inventoryComparer = new InventoryComparer(SessionSettings, initialStatusBuilder);
            inventoryComparer.Indexer = inventoryData.InventoryBuilder.Indexer;
            
            foreach (var inventoryBaseFile in BaseInventoryFiles)
            {
                inventoryComparer.AddInventory(inventoryBaseFile);
            }
            
            var comparisonResult = inventoryComparer.Compare();
            
            var filesIdentifier = new FilesIdentifier(inventoryData.Inventory, SessionSettings, inventoryComparer.Indexer);
            var items = filesIdentifier.Identify(comparisonResult);
            
            var inventoryFull =
                _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventoryData.Inventory, LocalInventoryModes.Full);
            FullInventoryFiles.Add(inventoryFull);
            await inventoryData.InventoryBuilder.RunAnalysisAsync(inventoryFull, items, CancellationToken.None);
        }
        
        var initialStatusBuilderFull = new InitialStatusBuilder();
        using var inventoryComparerFull = new InventoryComparer(SessionSettings, initialStatusBuilderFull);
        foreach (var fullInventoryFile in FullInventoryFiles)
        {
            inventoryComparerFull.AddInventory(fullInventoryFile);
        }
        
        var finalComparisonResult = inventoryComparerFull.Compare();
        
        return finalComparisonResult;
    }
}