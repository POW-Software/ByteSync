using System;
using System.Collections.Generic;
using System.Linq;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class IndexedCache<TObject, TIndex> : IIndexedCache<TObject, TIndex>
{
    private Func<TObject, TIndex> _indexSelector;
    private readonly Dictionary<TIndex, List<TObject>> _cache = new();

    public IndexedCache()
    {
        
    }
    
    public void Initialize(SourceCache<TObject, string> sourceCache, Func<TObject, TIndex> indexSelector)
    {
        _indexSelector = indexSelector;
        
        // Initialisation du cache avec les objets existants
        foreach (var obj in sourceCache.Items)
        {
            Update(obj);
        }

        // Synchronisation avec le SourceCache
        sourceCache.Connect()
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    switch (change.Reason)
                    {
                        case ChangeReason.Add:
                        case ChangeReason.Update:
                            Update(change.Current);
                            break;
                        case ChangeReason.Remove:
                            Remove(change.Current);
                            break;
                    }
                }
            });
    }

    public void Update(TObject obj)
    {
        var index = _indexSelector(obj);

        if (!_cache.TryGetValue(index, out var objects))
        {
            objects = new List<TObject>();
            _cache[index] = objects;
        }

        // Mise à jour ou ajout de l'objet
        var existingObject = objects.FirstOrDefault(o => o.Equals(obj));
        if (existingObject != null)
        {
            objects.Remove(existingObject);
        }

        objects.Add(obj);
    }

    public void Remove(TObject obj)
    {
        var index = _indexSelector(obj);

        if (_cache.TryGetValue(index, out var objects))
        {
            objects.RemoveAll(o => o.Equals(obj));
            if (objects.Count == 0)
            {
                _cache.Remove(index);
            }
        }
    }

    public List<TObject> GetByIndex(TIndex index)
    {
        return _cache.TryGetValue(index, out var objects) ? objects : new List<TObject>();
    }
}
