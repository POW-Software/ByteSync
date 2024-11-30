using System.IO;

namespace ByteSync.Business.Communications.Transfers;

public class FileUploaderSlice
{
    public FileUploaderSlice(int partNumber, MemoryStream memoryStream)
    {
        PartNumber = partNumber;
        MemoryStream = memoryStream;
        
        MemoryStream.Position = 0;
    }
    
    public int PartNumber { get; }
    
    public MemoryStream MemoryStream { get; }
}