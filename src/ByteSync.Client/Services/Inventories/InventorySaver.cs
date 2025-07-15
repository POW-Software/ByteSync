using System.IO;
using System.IO.Compression;
using ByteSync.Common.Controls.Json;
using Serilog;

namespace ByteSync.Services.Inventories;

class InventorySaver
{
    public InventorySaver(InventoryBuilder inventoryBuilder)
    {
        InventoryBuilder = inventoryBuilder;

        CountByDirectory = new Dictionary<string, int>();
    }

    public void Start(string inventoryFullName)
    {
        if (File.Exists(inventoryFullName))
        {
            Log.Information("Deleting {FileFullName}", inventoryFullName);
            File.Delete(inventoryFullName);
            // throw new Exception("file already exists");
        }

        Log.Information("[InventorySaver] Creating zip: {FileFullName}", inventoryFullName);
        ZipArchive = ZipFile.Open(inventoryFullName, ZipArchiveMode.Create);

        CountByDirectory.Clear();
    }

    private InventoryBuilder InventoryBuilder { get; }

    private ZipArchive? ZipArchive { get; set; }

    private Dictionary<string, int> CountByDirectory { get; }

    public void AddSignature(string guid, MemoryStream memoryStream)
    {
        var directoryName = GetDirectoryName(guid);

        if (ZipArchive != null)
        {
            Log.Information("[InventorySaver] Adding signature entry: {EntryName}", $"{directoryName}/{guid}.sign");
            var signatureFile = ZipArchive.CreateEntry($"{directoryName}/{guid}.sign");

            using (var entryStream = signatureFile.Open())
            {
                memoryStream.Position = 0;
                memoryStream.CopyTo(entryStream);

                //using (var streamWriter = new StreamWriter(entryStream))
                //{
                //    streamWriter.Write(json);
                //}
            }
        }
    }

    private string GetDirectoryName(string guid)
    {
        // On doit déterminer le chemin

        // on répartit par clé pour éviter qu'il n'y ait trop de fichiers signatures dans le même répertoire
        // ex : si guid = fa7dbd6f-eefc-474f-9a09-7fc2c5de2718
        //      => première clé : f (première lettre du guid)
        //      => dès qu'on atteint 1000 fichiers dans f\, on prend 2 lettres => fa
        //      => ainsi de suite, dès qu'on atteint 1000 fichiers, on ajoute une lettre => fa7, fa7d

        var len = 1;

        var candidate = guid.Substring(0, len);
        var isOK = false;
        while (!isOK)
        {
            if (!CountByDirectory.ContainsKey(candidate))
            {
                CountByDirectory.Add(candidate, 0);
                isOK = true;
            }
            else if (CountByDirectory[candidate] < 1000)
            {
                CountByDirectory[candidate] += 1;
                isOK = true;
            }
            else
            {
                len += 1;
                candidate = guid.Substring(0, len);
            }
        }

        return candidate;
    }

    public void WriteInventory()
    {
        var json = JsonHelper.Serialize(InventoryBuilder.Inventory);

        if (ZipArchive != null)
        {
            Log.Information("[InventorySaver] Writing inventory.json entry");
            var inventoryFile = ZipArchive.CreateEntry("inventory.json");

            using (var entryStream = inventoryFile.Open())
            {
                using (var streamWriter = new StreamWriter(entryStream))
                {
                    streamWriter.Write(json);
                }
            }
        }
    }


    public void Stop()
    {
        try
        {
            Log.Information("[InventorySaver] Closing zip");
            ZipArchive?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "InventorySaver.Stop");
        }
    }
}