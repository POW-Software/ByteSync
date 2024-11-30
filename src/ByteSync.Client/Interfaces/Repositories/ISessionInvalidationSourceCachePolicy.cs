using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface ISessionInvalidationSourceCachePolicy<TObject, TKey> : IDisposable where TKey : notnull
{
    void Initialize(SourceCache<TObject, TKey> sourceCache, bool b, bool b1);
}