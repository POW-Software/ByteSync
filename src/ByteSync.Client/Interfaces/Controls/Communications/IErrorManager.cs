namespace ByteSync.Services.Communications.Transfers;

public interface IErrorManager
{
    void SetOnError();
    bool IsError { get; }
} 