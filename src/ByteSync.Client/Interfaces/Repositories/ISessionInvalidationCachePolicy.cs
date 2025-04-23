using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface ISessionInvalidationCachePolicy<TObject, TKey> : IDisposable 
    where TKey : notnull 
    where TObject : notnull
{
    void Initialize(SourceCache<TObject, TKey> sourceCache, bool b, bool b1);
}