using System;
using System.Linq;
using ByteSync.TestsCommon;
using NUnit.Framework;

namespace ByteSync.Tests.TestUtilities.Misc;

[TestFixture]
public class TestDirectoryCleaner : AbstractTester
{
    [Test]
    public void ClearOlderTestDirectories()
    {
        // 11/02/2023 : pour l'instant, il n'y a pas d'autres options que de créer un TestDirectory pour récupérer le root.
        CreateTestDirectory();

        var rootDirectory = TestDirectory.Parent!;
        
        // On prend les 500 répertoires les plus anciens et créés avant hier
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