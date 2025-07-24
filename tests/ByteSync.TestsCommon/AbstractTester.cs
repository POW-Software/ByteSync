using ByteSync.Common.Helpers;
using NUnit.Framework;

namespace ByteSync.TestsCommon;

public abstract class AbstractTester
{
    private const string TEST_DIRECTORY_NAME = "ByteSync_Automated_Tests";

    protected static object _staticSyncRoot = new object();

    public DirectoryInfo TestDirectory { get; set; }

    protected object SyncRoot { get; }

    protected AbstractTester()
    {
        SyncRoot = new object();
        
        // Enables Console.WriteLine to be published in real time
        // https://youtrack.jetbrains.com/issue/RIDER-40359
        Console.SetOut(TestContext.Progress);
    }
    
    protected virtual void CreateTestDirectory()
    {
        lock (_staticSyncRoot)
        {
            string testDirectoryFullName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                           Path.DirectorySeparatorChar + TEST_DIRECTORY_NAME;
            
            testDirectoryFullName = IOUtils.Combine(testDirectoryFullName, MiscUtils.CreateGUID());

            TryDeleteDirectory(testDirectoryFullName);

            TestDirectory = Directory.CreateDirectory(testDirectoryFullName);
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

    protected FileInfo CreateFileInDirectory(DirectoryInfo directoryInfo, string fileName, string contents)
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

    protected FileInfo CreateFileInDirectory(string directoryFullName, string fileName, string contents)
    {
        string fileFullName = IOUtils.Combine(directoryFullName, fileName);

        File.WriteAllText(fileFullName, contents);

        return new FileInfo(fileFullName);
    }

    protected FileInfo CreateSubTestFile(string relativeFilePath, string contents, DateTime? lastWriteTime = null, DateTime? lastWriteTimeUtc = null)
    {
        return TestFileSystemUtils.CreateSubTestFile(TestDirectory, relativeFilePath, contents, 
            lastWriteTime, lastWriteTimeUtc);
    }

    protected DirectoryInfo CreateSubTestDirectory(string relativeDirectoryPath)
    {
        return TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, relativeDirectoryPath);
    }
}