using System;
using System.Threading;
using System.Threading.Tasks;

namespace ByteSync.Common.Interfaces;

public interface IRepository<T>
{
    public Task<T> GetDataAsync(string dataId);
    
    public Task<T?> GetDataAsync();
    
    public T? GetData();
    
    Task ClearAsync();
    
    public Task RunAsync(string dataId, Action<T> func);

    public void Run(string dataId, Action<T> func);

    Task<T2> GetAsync<T2>(string lobbyId, Func<T, T2> func);
    
    T2 Get<T2>(string lobbyId, Func<T, T2> func);

    public Task WaitOrThrowAsync(string dataId, Func<T, EventWaitHandle> func, TimeSpan? timeout, string exceptionMessage);

    public Task WaitOrThrowAsync(string dataId, Func<T, EventWaitHandle> func, Func<T, TimeSpan?> getTimeSpan, string exceptionMessage);
    
    public Task<bool> WaitAsync(string dataId, Func<T, EventWaitHandle> func, TimeSpan? timeout);

    public Task<bool> WaitAsync(string dataId, Func<T, EventWaitHandle> func, Func<T, TimeSpan?> getTimeSpan);

    public bool Wait(string dataId, Func<T, EventWaitHandle> func, Func<T, TimeSpan?> getTimeSpan);
}