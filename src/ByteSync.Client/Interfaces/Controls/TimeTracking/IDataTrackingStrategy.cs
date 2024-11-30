namespace ByteSync.Interfaces.Controls.TimeTracking;

public interface IDataTrackingStrategy
{
    IObservable<(long IdentifiedSize, long ProcessedSize)> GetDataObservable();
}