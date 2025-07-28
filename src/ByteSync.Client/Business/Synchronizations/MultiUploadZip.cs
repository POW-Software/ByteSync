using System.IO;
using System.IO.Compression;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Business.Synchronizations;

public class MultiUploadZip : IDisposable
{
    public MultiUploadZip(string key, SharedFileDefinition sharedFileDefinition)
    {
        Key = key;
            
        SharedFileDefinition = sharedFileDefinition;
        
        SharedFileDefinition.IsMultiFileZip = true;
        CreationDate = DateTime.Now;
        Size = 0;

        MemoryStream = new MemoryStream();

        ZipArchive = new ZipArchive(MemoryStream, ZipArchiveMode.Create, true);

        ActionGroupsIds = new List<string>();
        
        FilesFullNames = new List<string>();

        ActionsGroupIdsConcatenationLength = 0;
    }

    public string Key { get; }
        
    public SharedFileDefinition SharedFileDefinition { get; }
    
    public MemoryStream MemoryStream { get; }
    
    public ZipArchive ZipArchive { get; }

    public DateTime CreationDate { get; set; }

    public long Size { get; private set; }
    
    public int ActionsGroupIdsConcatenationLength { get; set; }
    
    public List<string> ActionGroupsIds { get; set; }
    
    public List<string> FilesFullNames { get; }

    public bool CanAdd(FileInfo fileInfo, string actionsGroupId)
    {
        return 
            ActionGroupsIds.Count < 100 &&
            ActionsGroupIdsConcatenationLength + actionsGroupId.Length + 5 < 25000 &&
            Size + fileInfo.Length < 8 * SizeConstants.ONE_MEGA_BYTES;
    }

    public void AddEntry(FileInfo fileInfo, string actionsGroupId)
    {
        // https://stackoverflow.com/questions/22339260/how-do-i-add-files-to-an-existing-zip-archive
        ZipArchive.CreateEntryFromFile(fileInfo.FullName, actionsGroupId, CompressionLevel.Fastest);

        ActionGroupsIds.Add(actionsGroupId);
        FilesFullNames.Add(fileInfo.FullName);

        Size += fileInfo.Length;

        ActionsGroupIdsConcatenationLength += actionsGroupId.Length + 5;
    }

    public void CloseZip()
    {
        ZipArchive.Dispose();
    }

    public void Dispose()
    {
        MemoryStream.Dispose();
    }
}