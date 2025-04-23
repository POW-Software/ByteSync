using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface IPropertyIndexer<TObject, in TIndex> 
    where TObject : notnull 
    where TIndex : notnull
{
    void Initialize(SourceCache<TObject, string> sourceCache, Func<TObject, TIndex> indexSelector);

    List<TObject> GetByIndex(TIndex index);
}