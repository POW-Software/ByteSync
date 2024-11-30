using System.IO;
using System.IO.Compression;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Controls.JSon;
using Newtonsoft.Json;

namespace ByteSync.Services.Actions;

public class SynchronizationDataSaver
{
    public SynchronizationDataSaver() 
    {
        
    }

    public void Save(string localPath, SharedSynchronizationStartData sharedSynchronizationStartData)
    {
        using ZipArchive zipArchive = ZipFile.Open(localPath, ZipArchiveMode.Create);
        
        JsonSerializerSettings settings = JsonSerializerSettingsHelper.BuildSettings(true, true, true);
        string json = JsonConvert.SerializeObject(sharedSynchronizationStartData, Formatting.Indented, settings);

        var inventoryFile = zipArchive.CreateEntry("synchronization_start_data.json");

        using var entryStream = inventoryFile.Open();
        using var streamWriter = new StreamWriter(entryStream);
        streamWriter.Write(json);
    }

    public SharedSynchronizationStartData Load(string localPath)
    {
        using ZipArchive zipArchive = ZipFile.OpenRead(localPath);
        
        var synchronizationActionsFile = zipArchive.GetEntry("synchronization_start_data.json");

        using var entryStream = synchronizationActionsFile!.Open();
        using var streamWriter = new StreamReader(entryStream);
        var json = streamWriter.ReadToEnd();

        JsonSerializerSettings settings = JsonSerializerSettingsHelper.BuildSettings(true, true, true);
        var sharedSynchronizationData = JsonConvert.DeserializeObject<SharedSynchronizationStartData>(json, settings);

        return sharedSynchronizationData;
    }
}