using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Business.Repositories;

public class CacheKey
{
    public required EntityType EntityType { get; init; }
    
    public required string EntityId { get; init; }
    
    public required string Value { get; init; }
}