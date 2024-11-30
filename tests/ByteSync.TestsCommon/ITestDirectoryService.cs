namespace ByteSync.TestsCommon;

public interface ITestDirectoryService
{
    DirectoryInfo TestDirectory { get; set; }

    DirectoryInfo CreateTestDirectory();

    FileInfo CreateFileInDirectory(DirectoryInfo directoryInfo, string fileName, string contents);

    FileInfo CreateFileInDirectory(string directoryFullName, string fileName, string contents);

    FileInfo CreateSubTestFile(string relativeFilePath, string contents, DateTime? lastWriteTime = null, DateTime? lastWriteTimeUtc = null);

    DirectoryInfo CreateSubTestDirectory(string relativeDirectoryPath);
    
    void Clear();
}