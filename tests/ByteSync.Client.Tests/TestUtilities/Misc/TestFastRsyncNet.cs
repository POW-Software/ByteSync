using System.IO;
using System.Text;
using ByteSync.Common.Helpers;
using ByteSync.TestsCommon;
using FastRsync.Core;
using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.TestUtilities.Misc;

[TestFixture]
public class TestFastRsyncNet : AbstractTester
{
    [Test]
    public void Test_DifferentFiles()
    {
        CreateTestDirectory();

        const int oneMegaByte = 1024 * 1024;

        var sb1 = new StringBuilder();
        var sb2 = new StringBuilder();
            
        // 1er Mo indentique
        var content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        // 2ème Mo indentique
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        // 3ème Mo différent
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb2.Append(content);
            
        // 4ème Mo indentique
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        // 5ème Mo indentique
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        var sourceFile = CreateFileInDirectory(TestDirectory, "datasource.dat", sb1.ToString());
        var destFile = CreateFileInDirectory(TestDirectory, "datadest.dat", sb2.ToString());
            
            
        // On calcule la signature sur la destination
        string signatureDestPath = IOUtils.Combine(TestDirectory.FullName, "signature1.txt");
        var signatureBuilder = new SignatureBuilder();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
        }


        // On utilise la signature et la source pour calculer le delta
        string deltaPath = IOUtils.Combine(TestDirectory.FullName, "delta.txt");
        var deltaBuilder = new DeltaBuilder();
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var newFileStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureStream, deltaBuilder.ProgressReport), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
        }



        // On utilise le destFile et le delta pour calculer le fichier résultat
        string resultPath = IOUtils.Combine(TestDirectory.FullName, "result.txt");
        var deltaApplier = new DeltaApplier
        {
            SkipHashCheck = false
        };
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var newFileStream = new FileStream(resultPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, deltaBuilder.ProgressReport), newFileStream);
        }

            
        var shaSource = CryptographyUtils.ComputeSHA256(sourceFile.FullName);
        var shaDest = CryptographyUtils.ComputeSHA256(destFile.FullName);
        var shaResult = CryptographyUtils.ComputeSHA256(resultPath);
            
        ClassicAssert.AreNotEqual(shaSource, shaDest);
        ClassicAssert.AreEqual(shaSource, shaResult);
    }
        
    [Test]
    public void Test_SameFiles()
    {
        CreateTestDirectory();

        const int oneMegaByte = 1024 * 1024;

        var sb1 = new StringBuilder();
        var sb2 = new StringBuilder();
            
        // 1er Mo indentique
        var content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        // 2ème Mo indentique
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        // 3ème Mo indentique
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        // 4ème Mo indentique
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        // 5ème Mo indentique
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
        sb2.Append(content);
            
        var sourceFile = CreateFileInDirectory(TestDirectory, "datasource.dat", sb1.ToString());
        var destFile = CreateFileInDirectory(TestDirectory, "datadest.dat", sb2.ToString());
            
            
        // On calcule la signature sur la destination
        string signatureDestPath = IOUtils.Combine(TestDirectory.FullName, "signature1.txt");
        var signatureBuilder = new SignatureBuilder();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
        }


        // On utilise la signature et la source pour calculer le delta
        string deltaPath = IOUtils.Combine(TestDirectory.FullName, "delta.txt");
        var deltaBuilder = new DeltaBuilder();
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var newFileStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureStream, deltaBuilder.ProgressReport), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
        }



        // On utilise le destFile et le delta pour calculer le fichier résultat
        string resultPath = IOUtils.Combine(TestDirectory.FullName, "result.txt");
        var deltaApplier = new DeltaApplier
        {
            SkipHashCheck = false
        };
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var newFileStream = new FileStream(resultPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, deltaBuilder.ProgressReport), newFileStream);
        }

            
        var shaSource = CryptographyUtils.ComputeSHA256(sourceFile.FullName);
        var shaDest = CryptographyUtils.ComputeSHA256(destFile.FullName);
        var shaResult = CryptographyUtils.ComputeSHA256(resultPath);
            
        ClassicAssert.AreEqual(shaSource, shaDest);
        ClassicAssert.AreEqual(shaSource, shaResult);
    }
        
    [Test]
    public void Test_SourceEmpty()
    {
        CreateTestDirectory();

        const int oneMegaByte = 1024 * 1024;

        var sb1 = new StringBuilder();
        var sb2 = new StringBuilder();
            
        var content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb2.Append(content);
            
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb2.Append(content);
            
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb2.Append(content);

        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb2.Append(content);
            
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb2.Append(content);
            
        var sourceFile = CreateFileInDirectory(TestDirectory, "datasource.dat", sb1.ToString());
        var destFile = CreateFileInDirectory(TestDirectory, "datadest.dat", sb2.ToString());
            
            
        // On calcule la signature sur la destination
        string signatureDestPath = IOUtils.Combine(TestDirectory.FullName, "signature1.txt");
        var signatureBuilder = new SignatureBuilder();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
        }


        // On utilise la signature et la source pour calculer le delta
        string deltaPath = IOUtils.Combine(TestDirectory.FullName, "delta.txt");
        var deltaBuilder = new DeltaBuilder();
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var newFileStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureStream, deltaBuilder.ProgressReport), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
        }



        // On utilise le destFile et le delta pour calculer le fichier résultat
        string resultPath = IOUtils.Combine(TestDirectory.FullName, "result.txt");
        var deltaApplier = new DeltaApplier
        {
            SkipHashCheck = false
        };
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var newFileStream = new FileStream(resultPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, deltaBuilder.ProgressReport), newFileStream);
        }

            
        var shaSource = CryptographyUtils.ComputeSHA256(sourceFile.FullName);
        var shaDest = CryptographyUtils.ComputeSHA256(destFile.FullName);
        var shaResult = CryptographyUtils.ComputeSHA256(resultPath);
            
        ClassicAssert.AreNotEqual(shaSource, shaDest);
        ClassicAssert.AreEqual(shaSource, shaResult);
    }
        
    [Test]
    public void Test_DestinationEmpty()
    {
        CreateTestDirectory();

        const int oneMegaByte = 1024 * 1024;

        var sb1 = new StringBuilder();
        var sb2 = new StringBuilder();
            
        var content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
            
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
            
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
            
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);
            
        content = TestFileSystemUtils.GenerateRandomTextContent(oneMegaByte);
        sb1.Append(content);

        var sourceFile = CreateFileInDirectory(TestDirectory, "datasource.dat", sb1.ToString());
        var destFile = CreateFileInDirectory(TestDirectory, "datadest.dat", sb2.ToString());
            
            
        // On calcule la signature sur la destination
        string signatureDestPath = IOUtils.Combine(TestDirectory.FullName, "signature1.txt");
        var signatureBuilder = new SignatureBuilder();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
        }


        // On utilise la signature et la source pour calculer le delta
        string deltaPath = IOUtils.Combine(TestDirectory.FullName, "delta.txt");
        var deltaBuilder = new DeltaBuilder();
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var newFileStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signatureDestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureStream, deltaBuilder.ProgressReport), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
        }



        // On utilise le destFile et le delta pour calculer le fichier résultat
        string resultPath = IOUtils.Combine(TestDirectory.FullName, "result.txt");
        var deltaApplier = new DeltaApplier
        {
            SkipHashCheck = false
        };
        deltaBuilder.ProgressReport = new ConsoleProgressReporter();
        using (var basisStream = new FileStream(destFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var newFileStream = new FileStream(resultPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, deltaBuilder.ProgressReport), newFileStream);
        }

            
        var shaSource = CryptographyUtils.ComputeSHA256(sourceFile.FullName);
        var shaDest = CryptographyUtils.ComputeSHA256(destFile.FullName);
        var shaResult = CryptographyUtils.ComputeSHA256(resultPath);
            
        ClassicAssert.AreNotEqual(shaSource, shaDest);
        ClassicAssert.AreEqual(shaSource, shaResult);
    }
}