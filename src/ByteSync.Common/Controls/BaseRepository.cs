﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Helpers;
using ByteSync.Common.Interfaces;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeProtected.Global
namespace ByteSync.Common.Controls;

public abstract class BaseRepository<T> : IRepository<T>
{
    protected readonly ILogger<BaseRepository<T>> _logger;

    protected BaseRepository(ILogger<BaseRepository<T>> logger)
    {
        _logger = logger;
        
        SyncRoot = new object();

        IsDataSetEvent = new ManualResetEvent(false); 
    }

    protected object SyncRoot { get; }
    
    private ManualResetEvent IsDataSetEvent { get; }
    
    private T? Data { get; set; }

    public async Task<T> GetDataAsync(string dataId)
    {
        return await Task.Run(() =>
        {
            return CheckAndRun(dataId, data => data);
        });
    }

    public async Task<T?> GetDataAsync()
    {
        return await Task.Run(GetData);
    }

    public T? GetData()
    {
        lock (SyncRoot)
        {
            return Data;
        }
    }

    protected async Task ResetDataAsync(string newDataId)
    {
        await ResetDataAsync(newDataId, null);
    }

    protected async Task ResetDataAsync(string newDataId, Action<T>? func)
    {
        await Task.Run(() =>
        {
            lock (SyncRoot)
            {
                HandlePreviousData();

                Data = (T) Activator.CreateInstance(typeof(T), newDataId);
                IsDataSetEvent.Set();
                
                _logger.LogDebug("{HolderType}.ResetDataAsync, DataId:{DataId}", GetType().Name, GetDataId(Data));

                if (func != null)
                {
                    CheckAndRun(newDataId, func);
                }
            }
        });
    }

    protected void InitializeData(T data)
    {
        lock (SyncRoot)
        {
            Data = data;
            IsDataSetEvent.Set();
        }
    }

    protected bool IsDataSet()
    {
        return IsDataSetEvent.WaitOne(0);
    }

    public async Task ClearAsync()
    {
        await Task.Run(() =>
        {
            lock (SyncRoot)
            {
                HandlePreviousData();

                Data = default;
                IsDataSetEvent.Reset();
            }
        });
    }

    private void HandlePreviousData()
    {
        if (Data != null)
        {
            // On set le WaitHandle de fin 
            var endEvent = GetEndEvent(Data);
            endEvent?.Set();
        }
    }
    
    public async Task RunAsync(string? dataId, Action<T> func)
    {
        await Task.Run(() =>
        {
            CheckAndRun(dataId, func);
        });
    }

    public void Run(string? dataId, Action<T> func)
    {
        CheckAndRun(dataId, func);
    }
    
    public async Task<T2> GetAsync<T2>(string? dataId, Func<T, T2> func)
    {
        return await Task.Run(() =>
        {
            return CheckAndRun(dataId, func);
        });
    }
    
    public T2 Get<T2>(string? dataId, Func<T, T2> func)
    {
        return CheckAndRun(dataId, func);
    }
    
    public async Task WaitOrThrowAsync(string dataId, Func<T, EventWaitHandle> func, TimeSpan? timeout, string exceptionMessage, 
        CancellationToken cancellationToken = default)
    {
        await WaitOrThrowAsync(dataId, func, _ => timeout, exceptionMessage, cancellationToken);
    }
    
    public async Task WaitOrThrowAsync(string dataId, Func<T, EventWaitHandle> func, Func<T, TimeSpan?> getTimeSpan, string exceptionMessage,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            bool isWaitOK = Wait(dataId, func, getTimeSpan, cancellationToken);
            
            if (!isWaitOK)
            {
                throw new TimeoutException(exceptionMessage);
            }
        });
    }
    
    public async Task<bool> WaitAsync(string dataId, Func<T, EventWaitHandle> func, TimeSpan? timeout)
    {
        return await WaitAsync(dataId, func, _ => timeout);
    }
    
    public async Task<bool> WaitAsync(string dataId, Func<T, EventWaitHandle> func, Func<T, TimeSpan?> getTimeSpan)
    {
        return await Task.Run(() =>
        {
            return Wait(dataId, func, getTimeSpan);
        });
    }

    public bool Wait(string dataId, Func<T, EventWaitHandle> func, Func<T, TimeSpan?> getTimeSpan, CancellationToken cancellationToken = default)
    {
        EventWaitHandle? waitHandle = null;
        TimeSpan? timeout = null;
        EventWaitHandle? endEvent = null;

        var myFunc = (T data) =>
        {
            waitHandle = func(data);
            timeout = getTimeSpan(data);
            endEvent = GetEndEvent(data);
        };
        
        CheckAndRun(dataId, myFunc);
        
        timeout ??= new TimeSpan(-1);
        List<WaitHandle> waitHandles = [waitHandle!];
        
        if (endEvent != null)
        {
            waitHandles.Add(endEvent);
        }
        if (cancellationToken.CanBeCanceled)
        {
            waitHandles.Add(cancellationToken.WaitHandle);
        }
        
        int index = WaitHandle.WaitAny(waitHandles.ToArray(), timeout.Value);

        if (index == 0)
        {
            return true;
        }
        if (index < waitHandles.Count && waitHandles[index] == endEvent)
        {
            _logger.LogError("Wait: Process has ended");
            return false;
        }
        if (index < waitHandles.Count && waitHandles[index] == cancellationToken.WaitHandle)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return true;
            }
        }
        _logger.LogError("Wait: timeout has occured");
        return false;
    }

    private void CheckAndRun(string? dataId, Action<T> func)
    {
        CheckAndRun(dataId, arg =>
        {
            func.Invoke(arg);

            return true;
        });
    }

    private T2 CheckAndRun<T2>(string? dataId, Func<T, T2> func)
    {
        IsDataSetEvent.WaitOne();
        
        lock (SyncRoot)
        {
            if (Data != null)
            {
                if (dataId != null && !CheckDataId(dataId, Data))
                {
                    _logger.LogDebug("dataId is not expected. Current DataId:{CurrentDataId}, Incoming dataId:{IncomingDataId}", GetDataId(Data), dataId);
                    throw new ArgumentOutOfRangeException(nameof(dataId), "dataId is not expected");
                }
                
                return func.Invoke(Data);
            }
            else
            {
                throw new NullReferenceException("Data is null");
            }
        }
    }

    private bool CheckDataId(string dataId, T data)
    {
        return dataId.IsNotEmpty() && Equals(dataId, GetDataId(data));
    }

    protected abstract string GetDataId(T data);

    protected abstract ManualResetEvent? GetEndEvent(T data);
}