using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;

namespace ByteSync.Services.Misc;

public class UIHelper : IUIHelper
{
    // https://stackoverflow.com/questions/2091988/how-do-i-update-an-observablecollection-via-a-worker-thread
    public bool IsCommandLineMode { get; set; }

    public Task AddOnUI<T>(ObservableCollection<T> collection, T item)
    {
        return RunAction(() =>
        {
            collection.Add(item);
        });
    }

    public Task ClearAndAddOnUI<T>(ObservableCollection<T> collection, ICollection<T> items) 
    {
        return RunAction(() =>
        {
            collection.Clear();
            collection.AddAll(items);
        });
    }

    public Task ClearOnUI<T>(ObservableCollection<T> collection)
    {
        return RunAction(() =>
        {
            collection.Clear();
        });
    }

    public Task RemoveOnUI<T>(ObservableCollection<T> collection, T item)
    {
        return RunAction(() =>
        {
            collection.Remove(item);
        });
    }
    
    public Task RemoveAllOnUI<T>(ObservableCollection<T> collection, Func<T, bool> condition)
    {
        return RunAction(() =>
        {
            collection.RemoveAll(condition);
        });
    }

    public Task ExecuteOnUi(Action action)
    {
        // return Dispatcher.UIThread.InvokeAsync(action.Invoke);
        return RunAction(action);
    }
    
    private Task RunAction(Action action)
    {
        if (IsCommandLineMode)
        {
            return Task.Run(action);
        }
        else
        {
            return Dispatcher.UIThread.InvokeAsync(action);
        }
    }
}