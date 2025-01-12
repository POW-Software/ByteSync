using ByteSync.Common.Business.Versions;

namespace ByteSync.Business.Updates;

public class UpdateData
{
    public SoftwareVersionFile SoftwareVersionFile { get; init; } = null!;
    
    public string ApplicationBaseDirectory { get; set; } = null!;
    
    public string DownloadLocation { get; set; } = null!;
    
    public string UnzipLocation { get; set; } = null!;
    
    public string FileToDownload => SoftwareVersionFile.FileUri;
}