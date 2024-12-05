// using System.Threading.Tasks;
//
// namespace ByteSync.Interfaces.Controls.Communications.SignalR;
//
// public interface IHubInvoker
// {
//     public Task Invoke(string methodName);
//
//     public Task Invoke(string methodName, object? arg1);
//     
//     public Task Invoke(string methodName, object? arg1, object? arg2);
//     
//     public Task Invoke(string methodName, object? arg1, object? arg2, object? arg3);
//     
//     public Task Invoke(string methodName, object? arg1, object? arg2, object? arg3, object? arg4);
//
//     public Task<T> Invoke<T>(string methodName);
//     
//     public Task<T> Invoke<T>(string methodName, object? arg1);
//     
//     public Task<T> Invoke<T>(string methodName, object? arg1, object? arg2);
//     
//     public Task<T> Invoke<T>(string methodName, object? arg1, object? arg2, object? arg3);
//     
//     public Task<T> Invoke<T>(string methodName, object? arg1, object? arg2, object? arg3, object? arg4);
// }