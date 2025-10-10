namespace ByteSync.Interfaces.Controls.TimeTracking;

public interface IDataTrackingStrategy
{
    IObservable<(long IdentifiedVolume, long ProcessedVolume)> GetDataObservable();
}