using ByteSync.ServerCommon.Business.Repositories;

namespace ByteSync.ServerCommon.Exceptions;

public class ElementNotFoundException : Exception
{
    public ElementNotFoundException(string message) : base(message)
    {
    }
    
    public ElementNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ElementNotFoundException(CacheKey cacheKey) :
        base("Element not found in cache, key: " + cacheKey.Value + ", type: " + cacheKey.EntityType)
    {

    }
}