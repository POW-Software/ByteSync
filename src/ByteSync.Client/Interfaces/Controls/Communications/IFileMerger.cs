namespace ByteSync.Services.Communications.Transfers;

public interface IFileMerger
{
    Task MergeAsync(int partToMerge);
} 