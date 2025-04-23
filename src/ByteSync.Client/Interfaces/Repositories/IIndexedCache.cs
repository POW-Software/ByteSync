using ByteSync.Business.Actions.Local;
using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface IIndexedCache<TObject, in TIndex>
{
    void Initialize(SourceCache<TObject, string> sourceCache, Func<TObject, TIndex> indexSelector);
    
    // void Update(TObject obj);
    //
    // void Remove(TObject obj);

    List<TObject> GetByIndex(TIndex index);
}