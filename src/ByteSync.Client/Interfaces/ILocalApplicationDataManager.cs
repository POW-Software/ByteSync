namespace ByteSync.Interfaces;

public interface ILocalApplicationDataManager
{
    // string ClientInstanceId { get; }
    //
    // ByteSyncEndpoint? CurrentEndPoint { get; set; }

    // Task ExploreApplicationDataPath();
        
    string ApplicationDataPath { get; }
        
    string? LogFilePath { get; }

    string? DebugLogFilePath { get; }
}