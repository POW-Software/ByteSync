using System.IO.Compression;
using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Controls.Json;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Inventories;

public class InventoryStatisticsServiceTests
{
    private static string CreateInventoryZip(Inventory inventory)
    {
        var temp = Path.GetTempFileName();
        var zipPath = Path.ChangeExtension(temp, ".zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
        
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("inventory.json");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream);
        var json = JsonHelper.Serialize(inventory);
        writer.Write(json);
        
        return zipPath;
    }
    
    private static Inventory BuildInventoryWithFiles(int successCount, int errorCount, int neutralCount)
    {
        var inv = new Inventory
        {
            InventoryId = "IID_TEST",
            Endpoint = new ByteSyncEndpoint
            {
                ClientId = "C",
                ClientInstanceId = "CI",
                Version = "1",
                OSPlatform = OSPlatforms.Windows,
                IpAddress = "127.0.0.1"
            },
            Code = "A",
            NodeId = "N",
            MachineName = "M",
            StartDateTime = DateTimeOffset.Now,
            EndDateTime = DateTimeOffset.Now
        };
        
        var part = new InventoryPart(inv, "/root", FileSystemTypes.Directory)
        {
            Code = "P"
        };
        
        inv.InventoryParts.Add(part);
        
        for (var i = 0; i < successCount; i++)
        {
            var fd = new FileDescription
            {
                InventoryPart = part,
                RelativePath = $"/file_success_{i}",
                Sha256 = "hash"
            };
            part.FileDescriptions.Add(fd);
        }
        
        for (var i = 0; i < errorCount; i++)
        {
            var fd = new FileDescription
            {
                InventoryPart = part,
                RelativePath = $"/file_error_{i}",
                AnalysisErrorType = "ex",
                AnalysisErrorDescription = "error"
            };
            part.FileDescriptions.Add(fd);
        }
        
        for (var i = 0; i < neutralCount; i++)
        {
            var fd = new FileDescription
            {
                InventoryPart = part,
                RelativePath = $"/file_neutral_{i}"
            };
            part.FileDescriptions.Add(fd);
        }
        
        return inv;
    }
    
    [Test]
    public async Task Compute_WithSingleInventory_ComputesExpectedTotals()
    {
        var inv = BuildInventoryWithFiles(successCount: 3, errorCount: 2, neutralCount: 1);
        var zip = CreateInventoryZip(inv);
        
        try
        {
            var sfd = new SharedFileDefinition
            {
                SessionId = "S",
                ClientInstanceId = inv.Endpoint.ClientInstanceId,
                SharedFileType = SharedFileTypes.FullInventory,
                AdditionalName = inv.CodeAndId,
                IV = new byte[16]
            };
            var inventoryFile = new InventoryFile(sfd, zip);
            
            var repo = new Mock<IInventoryFileRepository>();
            repo.Setup(r => r.GetAllInventoriesFiles(LocalInventoryModes.Full))
                .Returns([inventoryFile]);
            
            var ipd = new InventoryProcessData();
            var invService = new Mock<IInventoryService>();
            invService.SetupGet(s => s.InventoryProcessData).Returns(ipd);
            
            var logger = new Mock<ILogger<InventoryStatisticsService>>();
            
            var service = new InventoryStatisticsService(invService.Object, repo.Object, logger.Object);
            
            var tcs = new TaskCompletionSource<InventoryStatistics>();
            using var sub = service.Statistics.Where(s => s != null).Take(1).Select(s => s!).Subscribe(s => tcs.TrySetResult(s));
            
            ipd.AreFullInventoriesComplete.OnNext(true);
            
            var stats = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            
            stats.TotalAnalyzed.Should().Be(5);
            stats.Success.Should().Be(3);
            stats.Errors.Should().Be(2);
        }
        finally
        {
            if (File.Exists(zip))
            {
                File.Delete(zip);
            }
        }
    }
    
    [Test]
    public async Task Compute_WithMultipleInventories_AggregatesAll()
    {
        var inv1 = BuildInventoryWithFiles(1, 1, 0);
        var inv2 = BuildInventoryWithFiles(2, 0, 2);
        var zip1 = CreateInventoryZip(inv1);
        var zip2 = CreateInventoryZip(inv2);
        
        try
        {
            var sfd1 = new SharedFileDefinition
            {
                SessionId = "S",
                ClientInstanceId = inv1.Endpoint.ClientInstanceId,
                SharedFileType = SharedFileTypes.FullInventory,
                AdditionalName = inv1.CodeAndId,
                IV = new byte[16]
            };
            var sfd2 = new SharedFileDefinition
            {
                SessionId = "S",
                ClientInstanceId = inv2.Endpoint.ClientInstanceId,
                SharedFileType = SharedFileTypes.FullInventory,
                AdditionalName = inv2.CodeAndId,
                IV = new byte[16]
            };
            var inventoryFile1 = new InventoryFile(sfd1, zip1);
            var inventoryFile2 = new InventoryFile(sfd2, zip2);
            
            var repo = new Mock<IInventoryFileRepository>();
            repo.Setup(r => r.GetAllInventoriesFiles(LocalInventoryModes.Full))
                .Returns([inventoryFile1, inventoryFile2]);
            
            var ipd = new InventoryProcessData();
            var invService = new Mock<IInventoryService>();
            invService.SetupGet(s => s.InventoryProcessData).Returns(ipd);
            
            var logger = new Mock<ILogger<InventoryStatisticsService>>();
            
            var service = new InventoryStatisticsService(invService.Object, repo.Object, logger.Object);
            
            var tcs = new TaskCompletionSource<InventoryStatistics>();
            using var sub = service.Statistics.Where(s => s != null).Take(1).Select(s => s!).Subscribe(s => tcs.TrySetResult(s));
            
            ipd.AreFullInventoriesComplete.OnNext(true);
            
            var stats = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            
            stats.TotalAnalyzed.Should().Be(1 + 1 + 2 + 0);
            stats.Success.Should().Be(1 + 2);
            stats.Errors.Should().Be(1 + 0);
        }
        finally
        {
            if (File.Exists(zip1))
            {
                File.Delete(zip1);
            }
            
            if (File.Exists(zip2))
            {
                File.Delete(zip2);
            }
        }
    }
}