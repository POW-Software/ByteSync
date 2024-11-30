using ByteSync.Common.Helpers;

namespace ByteSync.TestsCommon;

public class TestDirectoryService : ITestDirectoryService
{
    protected static object _staticSyncRoot = new();
    
    private const string TEST_DIRECTORY_NAME = "POW_Unit_Tests";
    
    public DirectoryInfo? TestDirectory { get; set; }

    public DirectoryInfo CreateTestDirectory()
    {
        lock (_staticSyncRoot)
        {
            string testDirectoryFullName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                           Path.DirectorySeparatorChar + TEST_DIRECTORY_NAME;
            
            testDirectoryFullName = IOUtils.Combine(testDirectoryFullName, MiscUtils.CreateGUID());

            TryDeleteDirectory(testDirectoryFullName);

            TestDirectory = Directory.CreateDirectory(testDirectoryFullName);
            
            return TestDirectory;
        }
    }

    private static void TryDeleteDirectory(string directoryFullName)
    {
        lock (_staticSyncRoot)
        {
            if (Directory.Exists(directoryFullName))
            {
                bool exists = true;

                int cpt = 0;
                while (exists && cpt < 5)
                {
                    try
                    {
                        cpt += 1;

                        Directory.Delete(directoryFullName, true);

                        Thread.Sleep(200);

                        exists = Directory.Exists(directoryFullName);
                    }
                    catch (Exception)
                    {
                        if (cpt == 3)
                        {
                            throw;
                        }
                        else
                        {
                            Thread.Sleep(500);
                            exists = Directory.Exists(directoryFullName);
                        }
                    }
                }
            }
        }
    }
    
    public FileInfo CreateFileInDirectory(DirectoryInfo directoryInfo, string fileName, string contents)
    {
        string fileFullName = IOUtils.Combine(directoryInfo.FullName, fileName);

        FileInfo fileInfo = new FileInfo(fileFullName);
        if (!fileInfo.Directory!.Exists)
        {
            fileInfo.Directory.Create();
        }

        File.WriteAllText(IOUtils.Combine(directoryInfo.FullName, fileName), contents);

        return new FileInfo(fileFullName);
    }

    public FileInfo CreateFileInDirectory(string directoryFullName, string fileName, string contents)
    {
        string fileFullName = IOUtils.Combine(directoryFullName, fileName);

        File.WriteAllText(fileFullName, contents);

        return new FileInfo(fileFullName);
    }

    public FileInfo CreateSubTestFile(string relativeFilePath, string contents, DateTime? lastWriteTime = null, DateTime? lastWriteTimeUtc = null)
    {
        return TestFileSystemUtils.CreateSubTestFile(TestDirectory, relativeFilePath, contents, 
            lastWriteTime, lastWriteTimeUtc);
    }

    public DirectoryInfo CreateSubTestDirectory(string relativeDirectoryPath)
    {
        return TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, relativeDirectoryPath);
    }

    public void Clear()
    {
        if (TestDirectory != null)
        {
            TestDirectory.Delete(true);
        }
    }
}