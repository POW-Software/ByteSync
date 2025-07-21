namespace ByteSync.Interfaces.Controls.Communications;

public interface IErrorManager
{
    Task SetOnErrorAsync();
    Task<bool> IsErrorAsync();
} 