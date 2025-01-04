using System.IO;
using System.IO.Compression;
using System.Text.Json;
using ByteSync.Common.Controls.Json;
using ByteSync.Models.Inventories;

namespace ByteSync.Services.Inventories;

class InventoryLoader : IDisposable
{
    public InventoryLoader(string fullName)
    {
        FullName = fullName;
            
        ZipArchive = ZipFile.OpenRead(FullName);

        Inventory = Load();
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

        using var entryStream = inventoryFile!.Open();
        var options = JsonSerializerOptionsHelper.BuildOptions(true, true, true);
            
        var inventory = JsonSerializer.Deserialize<Inventory>(entryStream, options);

        if (inventory == null)
        {
            throw new InvalidOperationException("Failed to deserialize inventory.json.");
        }
        
        return inventory;

        // return inventory;
        //
        // using (var streamReader = new StreamReader(entryStream))
        // {
        //     using (var jsonTextReader = new JsonTextReader(streamReader))
        //     {
        //         var settings = JsonSerializerSettingsHelper.BuildSettings(true, true, true);
        //         var serializer = JsonSerializer.Create(settings);
        //
        //         var inventory = serializer.Deserialize<Inventory>(jsonTextReader);
        //         return inventory;
        //     }
        // }
    }

    public MemoryStream GetSignature(string guid)
    {
        var entryName = GetEntryName(guid);

        var entry = ZipArchive.GetEntry(entryName);

        //var signatureFile = ZipArchive.CreateEntry($"{directoryName}/{guid}.sign");

        var memoryStream = new MemoryStream();
        using (var entryStream = entry.Open())
        {
            entryStream.CopyTo(memoryStream);

            //using (var streamWriter = new StreamWriter(entryStream))
            //{
            //    streamWriter.Write(json);
            //}
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