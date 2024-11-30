using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface IBaseSourceCacheRepository<TObject, TKey>
{
    public IObservableCache<TObject, TKey> ObservableCache { get; }
    
    public IEnumerable<TObject> Elements { get; }
    
    public TObject? GetElement(TKey key);
    
    void AddOrUpdate(TObject element);
    
    void AddOrUpdate(IEnumerable<TObject> element);
    
    void Remove(TObject element);
    
    void Remove(IEnumerable<TObject> elements);

    void Remove(TKey key);
    
    void Clear();
    
    public IObservable<Change<TObject, TKey>> Watch(TObject element);
}