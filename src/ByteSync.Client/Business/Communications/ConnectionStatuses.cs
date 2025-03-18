namespace ByteSync.Business.Communications;

public enum ConnectionStatuses
{
    NotConnected = 0,
    Connecting = 1,
    Connected = 2,
    RetryConnectingSoon = 3,
    ConnectionFailed = 4,
    UpdateNeeded = 5,
}