using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public abstract class BaseSourceCacheRepository<TObject, TKey> : IBaseSourceCacheRepository<TObject, TKey> 
    where TKey : notnull where TObject : notnull
{
    protected BaseSourceCacheRepository()
    {
        SourceCache = new SourceCache<TObject, TKey>(KeySelector);
        
        ObservableCache = SourceCache.AsObservableCache();
    }

    protected abstract TKey KeySelector(TObject element);

    protected SourceCache<TObject, TKey> SourceCache { get;}

    public IObservableCache<TObject, TKey> ObservableCache { get; set; }
    
    public IEnumerable<TObject> Elements => SourceCache.Items;

    public TObject? GetElement(TKey key)
    {
        var optional = SourceCache.Lookup(key);
        
        return optional.HasValue ? optional.Value : default;
    }

    public void AddOrUpdate(TObject element)
    {
        SourceCache.AddOrUpdate(element);
    }
    
    public void AddOrUpdate(IEnumerable<TObject> elements)
    {
        SourceCache.AddOrUpdate(elements);
    }
    
    public void Remove(TObject element)
    {
        SourceCache.Remove(element);
    }
    
    public void Remove(IEnumerable<TObject> elements)
    {
        SourceCache.Remove(elements);
    }
    
    public void Remove(TKey key)
    {
        SourceCache.Remove(key);
    }
    
    public void Clear()
    {
        SourceCache.Clear();
    }

    public IObservable<Change<TObject, TKey>> Watch(TObject element)
    {
        return SourceCache.Watch(KeySelector(element));
    }
}