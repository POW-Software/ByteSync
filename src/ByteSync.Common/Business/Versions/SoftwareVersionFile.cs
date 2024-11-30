namespace PowSoftware.Common.Business.Versions;

public class SoftwareVersionFile
{
    public string FileName { get; set; } = null!;
        
    public Platform Platform { get; set; }
        
    public string PortableZipSha256 { get; set; } = null!;

    public long PortableZipSize { get; set; }
        
    public string ExecutableFileName { get; set; } = null!;
}