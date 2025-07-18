namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileMerger
{
    Task MergeAsync(int partToMerge);
} 