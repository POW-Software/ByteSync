namespace ByteSync.Business.Updates;

public enum UpdateProgressStatus
{
    Downloading, 
    Extracting,
    UpdatingFiles,
    BackingUpExistingFiles,
    MovingNewFiles,
    RestartingApplication
}