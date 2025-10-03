using ByteSync.TestsCommon;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.TestUtilities.Misc;

[TestFixture]
public class TestDirectoryCleaner : AbstractTester
{
    [Test]
    public void ClearOlderTestDirectories()
    {
        // Creates a TestDirectory to obtain the root.
        CreateTestDirectory();
        
        var rootDirectory = TestDirectory.Parent!;
        
        // We take the 500 oldest directories created before yesterday
        var directoriesToDelete = rootDirectory.GetDirectories()
            .Where(d => d.CreationTime.Date < DateTime.Today.AddDays(-1))
            .OrderBy(d => d.CreationTime)
            .Take(500)
            .ToList();
        
        foreach (var directoryInfo in directoriesToDelete)
        {
            Console.WriteLine($"Deleting {directoryInfo.FullName}");
            
            directoryInfo.Delete(true);
        }
    }
}