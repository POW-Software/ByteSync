using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Business.Profiles;

public class SessionProfilePathItem
{
    public SessionProfilePathItem()
    {
        
    }
    
    public SessionProfilePathItem(DataSource dataSource)
    {
        Type = dataSource.Type;
        Path = dataSource.Path;
        Code = dataSource.Code;
    }

    public FileSystemTypes Type { get; set; }

    public string Path { get; set; } = null!;

    public string Code { get; set; } = null!;
}