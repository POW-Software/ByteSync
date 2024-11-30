using ByteSync.ServerCommon.Business.Repositories;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IRepository<T>
{
    string ComputeCacheKey(params string[] keyParts);
    
    string ElementName { get; }
    
    Task<T?> Get(string key);
    
    Task<T?> Get(string key, ITransaction? transaction);
    
    Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler);
    
    Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler, ITransaction? transaction);
    
    Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler);
    
    Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler, ITransaction transaction);
    
    Task<UpdateEntityResult<T>> UpdateIfExists(string key, Func<T, bool> updateHandler, ITransaction? transaction = null);
    
    Task<UpdateEntityResult<T>> Save(string key, T element, ITransaction? transaction = null);

    Task<UpdateEntityResult<T>> SetElement(string cacheKey, T createdOrUpdatedElement, IDatabaseAsync database);

    Task Delete(string key);
    
    Task Delete(string key, ITransaction? transaction);
}