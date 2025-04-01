using ByteSync.Services.Converters.BaseConverters;
using ByteSync.TestsCommon;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.Services.Converters.BaseConverters;

[TestFixture]
public class TestBase58Converter : AbstractTester
{
    [Test]
    [TestCase("be150223af01", "2df1V3r3S")]
    public void TestDiagnostics(string input, string expected)
    {
        Base58Converter base58Converter = new Base58Converter();
        var result = base58Converter.ConvertTo(input);
        
        ClassicAssert.AreEqual(expected, result);
    }
}