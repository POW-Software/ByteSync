using System.IO;
using System.IO.Compression;
using ByteSync.Common.Controls.Json;
using ByteSync.Models.Inventories;

namespace ByteSync.Services.Inventories;

public class InventoryLoader : IDisposable
{
    private readonly string? _expectedOwner;

    public InventoryLoader(string fullName, string? expectedOwner = null)
    {
        FullName = fullName;
        _expectedOwner = expectedOwner;
        
        var stream = ByteSync.Services.Synchronizations.LocalPathSafety.OpenRead(FullName);
        try
        {
            ZipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
            Inventory = Load();
        }
        catch
        {
            ZipArchive?.Dispose();
            stream.Dispose();
            throw;
        }
    }
    
    public string FullName { get; }
    
    private ZipArchive ZipArchive { get; set; }
    
    public Inventory Inventory { get; private set; }
    
    private Inventory Load()
    {
        var inventoryFile = ZipArchive.GetEntry("inventory.json");
        
        if (inventoryFile == null)
        {
            throw new FileNotFoundException("inventory.json not found in the archive.");
        }
        
        using var entryStream = inventoryFile.Open();
        
        var inventory = JsonHelper.Deserialize<Inventory>(entryStream);
        
        if (inventory == null)
        {
            throw new InvalidOperationException("Failed to deserialize inventory.json.");
        }
        
        if (_expectedOwner != null && inventory.Endpoint.ClientInstanceId != _expectedOwner) throw new UnauthorizedAccessException("Inventory sender mismatch");
        foreach (var part in inventory.InventoryParts)
        {
            part.Inventory = inventory;
            if (_expectedOwner != null && string.IsNullOrEmpty(part.SourceId)) throw new UnauthorizedAccessException("Missing inventory source id");
            foreach (var item in part.FileDescriptions.Cast<ByteSync.Models.FileSystems.FileSystemDescription>().Concat(part.DirectoryDescriptions))
            {
                item.InventoryPart = part;
                if (_expectedOwner != null)
                {
                    if (item.RelativePath == null || !item.RelativePath.StartsWith('/')) throw new UnauthorizedAccessException("Invalid inventory path");
                    ByteSync.Services.Synchronizations.LocalPathSafety.ValidateRelativePath(item.RelativePath[1..]);
                }
            }
        }

        foreach (var inventoryPart in inventory.InventoryParts
                     .Where(ip => !ip.IsIncompleteDueToAccess &&
                                  (ip.DirectoryDescriptions.Any(d => !d.IsAccessible)
                                   || ip.FileDescriptions.Any(f => !f.IsAccessible))))
        {
            inventoryPart.IsIncompleteDueToAccess = true;
        }
        
        return inventory;
    }
    
    public MemoryStream GetSignature(string guid)
    {
        var entryName = GetEntryName(guid);
        var entry = ZipArchive.GetEntry(entryName);
        
        var memoryStream = new MemoryStream();
        using (var entryStream = entry!.Open())
        {
            entryStream.CopyTo(memoryStream);
        }
        
        memoryStream.Position = 0;
        
        return memoryStream;
    }
    
    private string GetEntryName(string guid)
    {
        var charCount = 1;
        
        var directoryName = guid.Substring(0, charCount);
        var candidate = $"{directoryName}/{guid}.sign";
        
        while (ZipArchive.GetEntry(candidate) == null)
        {
            charCount += 1;
            
            directoryName = guid.Substring(0, charCount);
            candidate = $"{directoryName}/{guid}.sign";
        }
        
        return candidate;
    }
    
    public void Dispose()
    {
        ZipArchive.Dispose();
        
        ZipArchive = null!;
    }
}