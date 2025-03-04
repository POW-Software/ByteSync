using RedLockNet;

namespace ByteSync.ServerCommon.Exceptions;

public class AcquireRedisLockException : Exception
{
    public AcquireRedisLockException(string message) : base(message)
    {
    }
    
    public AcquireRedisLockException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AcquireRedisLockException(string key, IRedLock redisLock) :
        base("Could not acquire redis lock, key: " + key + ", status: " + redisLock.Status)
    {

    }
}