using ByteSync.Common.Helpers;

namespace ByteSync.TestsCommon;

public static class TestFileSystemUtils
{
    private static Random _random = new Random();

    public static FileInfo CreateSubTestFile(DirectoryInfo rootDirectory, string relativeFilePath, string contents, DateTime? lastWriteTime = null, DateTime? lastWriteTimeUtc = null)
    {
        relativeFilePath = relativeFilePath.Replace("/", Path.DirectorySeparatorChar.ToString());

        string fileFullName = IOUtils.Combine(rootDirectory.FullName, relativeFilePath);

        FileInfo fileInfo = new FileInfo(fileFullName);
        fileInfo.Directory.Create();

        File.WriteAllText(fileFullName, contents);

        fileInfo.Refresh();
        if (lastWriteTime != null)
        {
            fileInfo.LastWriteTime = lastWriteTime.Value;
        }
        if (lastWriteTimeUtc != null)
        {
            fileInfo.LastWriteTimeUtc = lastWriteTimeUtc.Value;
        }

        return fileInfo;
    }

    public static DirectoryInfo CreateSubTestDirectory(DirectoryInfo rootDirectory, string relativeDirectoryPath)
    {
        relativeDirectoryPath = relativeDirectoryPath.Replace("/", Path.DirectorySeparatorChar.ToString());

        string directoryFullName = IOUtils.Combine(rootDirectory.FullName, relativeDirectoryPath);

        var result = Directory.CreateDirectory(directoryFullName);

        return result;
    }

    public static string GenerateRandomTextContent(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}