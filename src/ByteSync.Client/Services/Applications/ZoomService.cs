using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business.Configurations;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;

namespace ByteSync.Services.Applications;

public class ZoomService : IZoomService
{
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    
    private readonly BehaviorSubject<int> _zoomLevel;
    
    public ZoomService(IApplicationSettingsRepository applicationSettingsManager)
    {
        _applicationSettingsRepository = applicationSettingsManager;
        
        _zoomLevel = new BehaviorSubject<int>(100);
    }
    
    public IObservable<int> ZoomLevel => _zoomLevel.AsObservable();

    public void Initialize()
    {
        var settingsZoomLevel = _applicationSettingsRepository.GetCurrentApplicationSettings().ZoomLevel;
        
        if (IsZoomLevelValid(settingsZoomLevel))
        {
            _zoomLevel.OnNext(settingsZoomLevel);
        }
        else
        {
            _zoomLevel.OnNext(100);
            UpdateSettings();
        }
    }

    public void ApplicationZoomIn()
    {
        TryUpdateApplicationZoomIn(5);
    }
        
    public void ApplicationZoomOut()
    {
        TryUpdateApplicationZoomIn(-5);
    }

    private void TryUpdateApplicationZoomIn(int zoomIncrement)
    {
        var zoomLevelCandidate = _zoomLevel.Value + zoomIncrement;
        
        if (IsZoomLevelValid(zoomLevelCandidate))
        {
            _zoomLevel.OnNext(zoomLevelCandidate);

            UpdateSettings();
        }
    }

    private void UpdateSettings()
    {
        _applicationSettingsRepository.UpdateCurrentApplicationSettings(
            settings => settings.ZoomLevel = _zoomLevel.Value);
    }

    private static bool IsZoomLevelValid(int zoomLevelCandidate)
    {
        return zoomLevelCandidate >= ZoomConstants.MIN_ZOOM_LEVEL &&
               zoomLevelCandidate <= ZoomConstants.MAX_ZOOM_LEVEL &&
               zoomLevelCandidate % 5 == 0;
    }
}