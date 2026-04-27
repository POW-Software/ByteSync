namespace ByteSync.Common.Business.Communications.Transfers;

public enum UploadFailureKind
{
    None = 0,
    ServerError = 1,
    ClientCancellation = 2,
    ClientTimeout = 3,
    ClientNetworkError = 4,
}
