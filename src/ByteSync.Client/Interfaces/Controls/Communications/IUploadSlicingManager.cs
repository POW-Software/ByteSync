namespace ByteSync.Interfaces.Controls.Communications;

public interface IUploadSlicingManager
{
    Task Enqueue(Func<Task> startSlicingAsync);
}


