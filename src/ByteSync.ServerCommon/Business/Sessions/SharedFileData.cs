using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;

namespace ByteSync.ServerCommon.Business.Sessions;

public class SharedFileData
{
    public SharedFileData()
    {
        
    }
    
    public SharedFileData(SharedFileDefinition sharedFileDefinition, ICollection<string> recipients)
    {
        SharedFileDefinition = sharedFileDefinition;

        UploadedPartsNumbers = new HashSet<int>();

        Recipients = new List<string>();
        foreach (var recipient in recipients)
        {
            var cleanedData = recipient;
            if (recipient.StartsWith("CIID_"))
            {
                cleanedData = recipient.Substring("CIID_".Length);
            }
                
            Recipients.Add(cleanedData);
        }
            
        DownloadedBy = new Dictionary<int, HashSet<string>>();
    }

    public SharedFileDefinition SharedFileDefinition { get; set; } = null!;

    public HashSet<int> UploadedPartsNumbers { get; set; } = null!;

    public bool IsFullyUploaded
    {
        get
        {
            return TotalParts != null && UploadedPartsNumbers.Count == TotalParts;
        }
    }

    public int? TotalParts { get; set; }
        
    public List<string> Recipients { get; set; } = null!;

    public Dictionary<int, HashSet<string>> DownloadedBy { get; set; } = null!;

    public bool IsFullyDownloaded 
    {
        get
        {
            return IsFullyUploaded && Recipients.Count > 0 
                                   && DownloadedBy.Keys.Count == TotalParts
                                   && DownloadedBy.Values.All(v => Recipients.HaveSameElements(v));
        }
    }

    public void SetDownloadedBy(string clientInstanceId, int partNumber)
    {
        if (!DownloadedBy.ContainsKey(partNumber))
        {
            DownloadedBy.Add(partNumber, new HashSet<string>());
        }
            
        DownloadedBy[partNumber].Add(clientInstanceId);
    }

    public bool IsPartFullyDownloaded(int partNumber)
    {
        if (DownloadedBy.TryGetValue(partNumber, out var value))
        {
            return Recipients.HaveSameElements(value);
        }

        return false;
    }
}