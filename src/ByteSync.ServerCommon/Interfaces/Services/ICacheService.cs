using RedLockNet;
using RedLockNet.SERedis;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ICacheService
{
    public RedLockFactory RedLockFactory { get; }
    
    public string Prefix { get; }
    
    ITransaction OpenTransaction();
    
    IDatabaseAsync GetDatabase();
    
    IDatabaseAsync GetDatabase(ITransaction? transaction);
    
    Task<IRedLock> AcquireLockAsync(string key);
}