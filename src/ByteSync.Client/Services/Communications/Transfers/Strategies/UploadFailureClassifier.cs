using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using ByteSync.Common.Business.Communications.Transfers;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public static class UploadFailureClassifier
{
    public static UploadFileResponse Classify(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return UploadFileResponse.ClientCancellation(exception);
        }

        if (exception is OperationCanceledException)
        {
            return UploadFileResponse.ClientTimeout(exception);
        }

        if (IsClientNetworkError(exception))
        {
            return UploadFileResponse.ClientNetworkError(exception);
        }

        return UploadFileResponse.Failure(500, exception);
    }

    private static bool IsClientNetworkError(Exception exception)
    {
        if (exception is not HttpRequestException and not IOException and not SocketException)
        {
            return false;
        }

        var current = exception;
        while (current != null)
        {
            if (current is SocketException socketException)
            {
                return socketException.SocketErrorCode is SocketError.ConnectionReset
                    or SocketError.ConnectionAborted
                    or SocketError.TimedOut
                    or SocketError.NetworkDown
                    or SocketError.NetworkUnreachable
                    or SocketError.HostDown
                    or SocketError.HostUnreachable;
            }

            current = current.InnerException;
        }

        return false;
    }
}
