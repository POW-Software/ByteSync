using System;

namespace ByteSync.Common.Business.Communications.Transfers;

public sealed record UploadResult(
    TimeSpan Elapsed,
    bool IsSuccess,
    int PartNumber,
    int? StatusCode = null,
    Exception? Exception = null,
    string? FileId = null,
    long ActualBytes = -1,
    UploadFailureKind FailureKind = UploadFailureKind.None);
