using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
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

        char cLetter = (char) ('A' + InventoryDatas.IndexOf(inventoryData));

        string letter = cLetter.ToString();

        inventoryData.SetLetter(letter);
    }

    private async Task<ComparisonResult> DoBuildAndCompare()
    {
        foreach (var inventoryData in InventoryDatas)
        {
            string inventoryFile = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventoryData.Letter, LocalInventoryModes.Base);
            BaseInventoryFiles.Add(inventoryFile);
            
            var endpoint = new ByteSyncEndpoint
            {
                ClientId = $"CI_{inventoryData.Letter}",
                ClientInstanceId = $"CII_{inventoryData.Letter}",
                IpAddress = "localhost",
                OSPlatform = OSPlatforms.Windows
            };
            
            var sessionMemberInfo = new SessionMemberInfo
            {
                Endpoint = endpoint,
                PositionInList = inventoryData.Letter[0] - 'A',
                PrivateData = new()
                {
                    MachineName = "Machine" + inventoryData.Letter
                }
            };
            
            Mock<ILogger<InventoryBuilder>> loggerMock = new Mock<ILogger<InventoryBuilder>>();
            
            InventoryBuilder inventoryBuilder = new InventoryBuilder(sessionMemberInfo, SessionSettings, new InventoryProcessData(), 
                OSPlatforms.Windows, FingerprintModes.Rsync, loggerMock.Object);
            
            foreach (var pathItem in inventoryData.PathItems)
            {
                inventoryBuilder.AddInventoryPart(pathItem);
            }
            
            inventoryData.InventoryBuilder = inventoryBuilder;
            
            await inventoryBuilder.BuildBaseInventoryAsync(inventoryFile);
        }

        foreach (var inventoryData in InventoryDatas)
        {
            using InventoryComparer inventoryComparer = new InventoryComparer(SessionSettings);
            inventoryComparer.Indexer = inventoryData.InventoryBuilder.Indexer;

            foreach (var inventoryBaseFile in BaseInventoryFiles)
            {
                inventoryComparer.AddInventory(inventoryBaseFile);
            }

            var comparisonResult = inventoryComparer.Compare();
                
            FilesIdentifier filesIdentifier = new FilesIdentifier(inventoryData.Inventory, SessionSettings, inventoryComparer.Indexer);
            var items = filesIdentifier.Identify(comparisonResult);
        
            string inventoryFull = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventoryData.Letter, LocalInventoryModes.Full);
            FullInventoryFiles.Add(inventoryFull);
            await inventoryData.InventoryBuilder.RunAnalysisAsync(inventoryFull, items, new CancellationToken());
        }

        using InventoryComparer inventoryComparerFull = new InventoryComparer(SessionSettings);
        foreach (var fullInventoryFile in FullInventoryFiles)
        {
            inventoryComparerFull.AddInventory(fullInventoryFile);
        }

        var finalComparisonResult = inventoryComparerFull.Compare();

        return finalComparisonResult;
    }
}