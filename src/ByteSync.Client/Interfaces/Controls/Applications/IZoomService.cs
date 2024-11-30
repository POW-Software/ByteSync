namespace ByteSync.Interfaces.Controls.Applications;

public interface IZoomService
{
    public IObservable<int> ZoomLevel { get; }
    
    public void Initialize();

    void ApplicationZoomIn();

    void ApplicationZoomOut();
}