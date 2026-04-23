using ByteSync.TestsCommon;
using FastRsync.Core;
using FastRsync.Delta;
using FastRsync.Signature;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Synchronizations;

[TestFixture]
public class DeltaByteShiftToleranceTests : AbstractTester
{
    [SetUp]
    public void SetUp()
    {
        CreateTestDirectory();
    }

    [Test]
    public void BuildDelta_WithByteInsertionShift_ShouldReconstructSourceAndKeepDeltaCompact()
    {
        var destinationBytes = CreateDeterministicBytes(2 * 1024 * 1024, 1001);
        var insertedBytes = CreateDeterministicBytes(4096, 2002);
        var sourceBytes = InsertBytes(destinationBytes, insertedBytes, 512 * 1024 + 123);

        var (deltaLength, reconstructedBytes) = BuildAndApplyDelta(sourceBytes, destinationBytes);

        reconstructedBytes.Should().Equal(sourceBytes);
        deltaLength.Should().BeGreaterThan(0);
        deltaLength.Should().BeLessThan(sourceBytes.Length / 4L);
    }

    [Test]
    public void BuildDelta_WithByteDeletionShift_ShouldReconstructSourceAndKeepDeltaCompact()
    {
        var destinationBytes = CreateDeterministicBytes(2 * 1024 * 1024, 3003);
        var sourceBytes = RemoveBytes(destinationBytes, 700 * 1024 + 77, 8192);

        var (deltaLength, reconstructedBytes) = BuildAndApplyDelta(sourceBytes, destinationBytes);

        reconstructedBytes.Should().Equal(sourceBytes);
        deltaLength.Should().BeGreaterThan(0);
        deltaLength.Should().BeLessThan(sourceBytes.Length / 4L);
    }

    private (long DeltaLength, byte[] ReconstructedBytes) BuildAndApplyDelta(byte[] sourceBytes, byte[] destinationBytes)
    {
        var sourcePath = Path.Combine(TestDirectory.FullName, "source.bin");
        var destinationPath = Path.Combine(TestDirectory.FullName, "destination.bin");
        var signaturePath = Path.Combine(TestDirectory.FullName, "signature.bin");
        var deltaPath = Path.Combine(TestDirectory.FullName, "delta.bin");
        var reconstructedPath = Path.Combine(TestDirectory.FullName, "reconstructed.bin");

        File.WriteAllBytes(sourcePath, sourceBytes);
        File.WriteAllBytes(destinationPath, destinationBytes);

        var signatureBuilder = new SignatureBuilder();
        using (var destinationStream = new FileStream(destinationPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureStream = new FileStream(signaturePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            signatureBuilder.Build(destinationStream, new SignatureWriter(signatureStream));
        }

        var deltaBuilder = new DeltaBuilder();
        using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var signatureReadStream = new FileStream(signaturePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaStream = new FileStream(deltaPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            deltaBuilder.BuildDelta(sourceStream,
                new SignatureReader(signatureReadStream, deltaBuilder.ProgressReport),
                new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
        }

        var deltaLength = new FileInfo(deltaPath).Length;

        var deltaApplier = new DeltaApplier
        {
            SkipHashCheck = false
        };
        using (var basisStream = new FileStream(destinationPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var deltaReadStream = new FileStream(deltaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reconstructedStream = new FileStream(reconstructedPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaReadStream, null), reconstructedStream);
        }

        var reconstructedBytes = File.ReadAllBytes(reconstructedPath);
        return (deltaLength, reconstructedBytes);
    }

    private static byte[] CreateDeterministicBytes(int length, int seed)
    {
        var bytes = new byte[length];
        var random = new Random(seed);
        random.NextBytes(bytes);
        return bytes;
    }

    private static byte[] InsertBytes(byte[] source, byte[] insertedBytes, int offset)
    {
        var result = new byte[source.Length + insertedBytes.Length];
        Buffer.BlockCopy(source, 0, result, 0, offset);
        Buffer.BlockCopy(insertedBytes, 0, result, offset, insertedBytes.Length);
        Buffer.BlockCopy(source, offset, result, offset + insertedBytes.Length, source.Length - offset);
        return result;
    }

    private static byte[] RemoveBytes(byte[] source, int offset, int removedLength)
    {
        var result = new byte[source.Length - removedLength];
        Buffer.BlockCopy(source, 0, result, 0, offset);
        Buffer.BlockCopy(source, offset + removedLength, result, offset, source.Length - offset - removedLength);
        return result;
    }
}
