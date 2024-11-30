using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Business.Profiles;

public class SessionProfilePathItem
{
    public SessionProfilePathItem()
    {
        
    }
    
    public SessionProfilePathItem(PathItem pathItem)
    {
        Type = pathItem.Type;
        Path = pathItem.Path;
        Code = pathItem.Code;
    }

    public FileSystemTypes Type { get; set; }

    public string Path { get; set; } = null!;

    public string Code { get; set; } = null!;
}