// using System.Collections.ObjectModel;
// using System.Threading.Tasks;
//
// namespace ByteSync.Interfaces;
//
// public interface IUIHelper
// {
//     bool IsCommandLineMode { get; set; }
//     
//     Task AddOnUI<T>(ObservableCollection<T> collection, T item);
//
//     Task ClearAndAddOnUI<T>(ObservableCollection<T> collection, ICollection<T> items);
//     
//     Task ClearOnUI<T>(ObservableCollection<T> collection);
//
//     Task RemoveOnUI<T>(ObservableCollection<T> collection, T item);
//
//     Task RemoveAllOnUI<T>(ObservableCollection<T> collection, Func<T, bool> condition);
//     
//     Task ExecuteOnUi(Action action);
//     
// }