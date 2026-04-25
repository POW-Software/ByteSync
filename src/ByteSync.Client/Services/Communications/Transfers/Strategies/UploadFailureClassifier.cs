using System;
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

        return UploadFileResponse.Failure(500, exception);
    }
}
