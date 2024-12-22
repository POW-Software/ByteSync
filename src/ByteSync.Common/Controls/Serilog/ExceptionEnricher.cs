using System;
using System.Net.Sockets;
using Serilog.Core;
using Serilog.Events;

namespace ByteSync.Common.Controls.Serilog;

public class ExceptionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception != null)
        {
            var socketException = FindException<SocketException>(logEvent.Exception);
            if (socketException != null)
            {
                EnrichWithSocketException(logEvent, propertyFactory, socketException);
            }
        }
    }

    private T? FindException<T>(Exception? exception) where T : Exception
    {
        if (exception is null)
        {
            return null;
        }
        
        if (exception is T)
        {
            return exception as T;
        }
        else
        {
            return FindException<T>(exception.InnerException);
        }
    }

    private static void EnrichWithSocketException(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, SocketException se)
    {
        // Détail des erreurs : https://docs.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2?redirectedfrom=MSDN
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SocketException_ErrorCode", se.ErrorCode));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SocketException_SocketErrorCode", se.SocketErrorCode));
    }
}