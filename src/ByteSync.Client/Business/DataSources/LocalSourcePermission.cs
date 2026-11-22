using ByteSync.Common.Business.Inventories;

namespace ByteSync.Business.DataSources;

public record LocalSourcePermission
{
    public LocalSourcePermission() { }
    public LocalSourcePermission(string identity, string path, FileSystemTypes type) { Identity = identity; Path = path; Type = type; }
    public string Identity { get; set; } = "";
    public string Path { get; set; } = "";
    public FileSystemTypes Type { get; set; }
}
