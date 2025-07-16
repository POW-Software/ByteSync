using System.IO;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Business.Communications.Downloading;

public class DownloadTarget
{
    public DownloadTarget(SharedFileDefinition sharedFileDefinition, LocalSharedFile? localSharedFile, HashSet<string> finalDestinations)
    {
        SharedFileDefinition = sharedFileDefinition;
        LocalSharedFile = localSharedFile;
        DownloadDestinations = finalDestinations;

        SyncRoot = new object();
        MemoryStreams = new Dictionary<int, MemoryStream>();
    }

    public SharedFileDefinition SharedFileDefinition { get; set; }
    
    public LocalSharedFile? LocalSharedFile { get; }

    public HashSet<string> DownloadDestinations { get; set; }

    private object SyncRoot { get; }
    
    public Dictionary<int, MemoryStream> MemoryStreams { get; }

    public string SessionId
    {
        get
        {
            return SharedFileDefinition.SessionId;
        }
    }

    public bool IsMultiFileZip { get; set; }
    
    public Dictionary<string, HashSet<string>>? FinalDestinationsPerActionsGroupId { get; set; }
    
    public Dictionary<string, DownloadTargetDates>? LastWriteTimeUtcPerActionsGroupId { get; set; }
    
    public List<ITemporaryFileManager>? TemporaryFileManagers { get; set; }

    public HashSet<string> AllFinalDestinations
    {
        get
        {
            HashSet<string> allFinalDestinations = new HashSet<string>();
            foreach (var value in FinalDestinationsPerActionsGroupId!.Values)
            {
                allFinalDestinations.AddAll(value);
            }

            return allFinalDestinations;
        }
    }

    public MemoryStream GetMemoryStream(int partNumber)
    {
        lock (SyncRoot)
        {
            return MemoryStreams[partNumber];
        }
    }
    
    public void RemoveMemoryStream(int partNumber)
    {
        lock (SyncRoot)
        {
            if (MemoryStreams.ContainsKey(partNumber))
            {
                Serilog.Log.Warning("[DownloadTarget] Removing MemoryStream for part {PartNumber}. HashCode={HashCode}, SessionId={SessionId}, FileId={FileId}, AllKeysBefore={AllKeysBefore}, stack: {Stack}", partNumber, this.GetHashCode(), SharedFileDefinition.SessionId, SharedFileDefinition.Id, string.Join(",", MemoryStreams.Keys), Environment.StackTrace);
                MemoryStreams.Remove(partNumber);
                Serilog.Log.Warning("[DownloadTarget] Removed MemoryStream for part {PartNumber}. AllKeysAfter={AllKeysAfter}", partNumber, string.Join(",", MemoryStreams.Keys));
            }
        }
    }

    public void AddOrReplaceMemoryStream(int partNumber, MemoryStream stream)
    {
        lock (SyncRoot)
        {
            MemoryStreams[partNumber] = stream;
        }
    }

    public DownloadTargetDates? GetTargetDates(string entryName)
    {
        if (LastWriteTimeUtcPerActionsGroupId != null)
        {
            DownloadTargetDates? downloadTargetDates;
            if (LastWriteTimeUtcPerActionsGroupId.TryGetValue(entryName, out downloadTargetDates))
            {
                return downloadTargetDates;
            }
        }

        return null;
    }

    public void ClearMemoryStream()
    {
        lock (SyncRoot)
        {
            Serilog.Log.Warning("[DownloadTarget] Clearing all MemoryStreams. HashCode={HashCode}, SessionId={SessionId}, FileId={FileId}, AllKeysBefore={AllKeysBefore}, stack: {Stack}", this.GetHashCode(), SharedFileDefinition.SessionId, SharedFileDefinition.Id, string.Join(",", MemoryStreams.Keys), Environment.StackTrace);
            MemoryStreams.Clear();
            Serilog.Log.Warning("[DownloadTarget] Cleared all MemoryStreams. AllKeysAfter={AllKeysAfter}", string.Join(",", MemoryStreams.Keys));
        }
    }
}