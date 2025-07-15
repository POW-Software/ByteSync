﻿using System.IO;
using System.IO.Compression;
using ByteSync.Common.Controls.Json;
using ByteSync.Models.Inventories;

namespace ByteSync.Services.Inventories;

public class InventoryLoader : IDisposable
{
    public InventoryLoader(string fullName)
    {
        if (!File.Exists(fullName))
        {
            Console.WriteLine($"[InventoryLoader] File does not exist: {fullName}");
        }
        else
        {
            var fileInfo = new FileInfo(fullName);
            Console.WriteLine($"[InventoryLoader] File exists: {fullName}, Size: {fileInfo.Length} bytes");
        }
        try
        {
        FullName = fullName;
            
        ZipArchive = ZipFile.OpenRead(FullName);

        Inventory = Load();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryLoader] Exception opening zip: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    public string FullName { get; }

    private ZipArchive ZipArchive { get; set; }

    internal Inventory Inventory { get; private set; }

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
        
        return inventory;
    }

    public MemoryStream GetSignature(string guid)
    {
        var entryName = GetEntryName(guid);
        var entry = ZipArchive.GetEntry(entryName);
        
        var memoryStream = new MemoryStream();
        using (var entryStream = entry.Open())
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

        ZipArchive = null;
    }
}