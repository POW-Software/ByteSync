using ByteSync.Common.Helpers;
using ByteSync.Services.Communications;
using ByteSync.TestsCommon;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications;

[TestFixture]
public class TestSafetyWordsComputer : AbstractTester
{
    [Test]
    public void Test_md5_1()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();

        var md5 = CryptographyUtils.ComputeMD5FromText("this_is_a_text");
        var result = safetyWordsComputer.Compute(md5);
        
        result.Length.Should().Be(12);

        var cpt = 0;
        result[cpt++].Should().Be("sultan", result.JoinToString(" "));
        result[cpt++].Should().Be("radio");
        result[cpt++].Should().Be("inside");
        result[cpt++].Should().Be("arcade");
        result[cpt++].Should().Be("small");
        result[cpt++].Should().Be("benny");
        result[cpt++].Should().Be("mike");
        result[cpt++].Should().Be("shrink");
        result[cpt++].Should().Be("program");
        result[cpt++].Should().Be("program");
        result[cpt++].Should().Be("galaxy");
        result[cpt++].Should().Be("vibrate");
        
        cpt.Should().Be(12);
    }

    [Test]
    public void Test_md5_2()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();

        var md5 = new string('0', 32);
        var result = safetyWordsComputer.Compute(md5);
        
        result.Length.Should().Be(12);

        for (var i = 0; i < 12; i++)
        {
            result[i].Should().Be("academy");
        }
    }
    
    [Test]
    public void Test_md5_3()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();

        var md5 = new string('f', 32);
        var result = safetyWordsComputer.Compute(md5);
        
        result.Length.Should().Be(12);

        var cpt = 0;
        result[cpt++].Should().Be("sincere", result.JoinToString(" "));
        result[cpt++].Should().Be("john");
        result[cpt++].Should().Be("quiet");
        result[cpt++].Should().Be("lorenzo");
        result[cpt++].Should().Be("jamaica");
        result[cpt++].Should().Be("montana");
        result[cpt++].Should().Be("gondola");
        result[cpt++].Should().Be("company");
        result[cpt++].Should().Be("ricardo");
        result[cpt++].Should().Be("forever");
        result[cpt++].Should().Be("silicon");
        result[cpt++].Should().Be("edition");
        
        cpt.Should().Be(12);
    }
    
    [Test]
    public void Test_md5_4()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();

        var md5 = new string('f', 30);
        md5 = "00" + md5;
        var result = safetyWordsComputer.Compute(md5);
        
        result.Length.Should().Be(12);

        var cpt = 0;
        result[cpt++].Should().Be("adrian", result.JoinToString(" "));
        result[cpt++].Should().Be("balance");
        result[cpt++].Should().Be("garbo");
        result[cpt++].Should().Be("speech");
        result[cpt++].Should().Be("exodus");
        result[cpt++].Should().Be("tunnel");
        result[cpt++].Should().Be("escort");
        result[cpt++].Should().Be("proxy");
        result[cpt++].Should().Be("frog");
        result[cpt++].Should().Be("initial");
        result[cpt++].Should().Be("beach");
        result[cpt++].Should().Be("venice");
        
        cpt.Should().Be(12);
    }
    
    [Test]
    public void Test_md5_5()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var md5 = new string('0', 16) + new string('f', 16);
        var result = safetyWordsComputer.Compute(md5);
        
        result.Length.Should().Be(12);

        var cpt = 0;
        result[cpt++].Should().Be("academy", result.JoinToString(" "));
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("triton");
        result[cpt++].Should().Be("bazaar");
        result[cpt++].Should().Be("sailor");
        result[cpt++].Should().Be("episode");
        result[cpt++].Should().Be("equal");
        result[cpt++].Should().Be("prime");
        
        cpt.Should().Be(12);
    }
    
    [Test]
    public void Test_md5_6()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var md5 = new string('0', 24) + new string('f', 8);
        var result = safetyWordsComputer.Compute(md5);
        
        result.Length.Should().Be(12);

        var cpt = 0;
        result[cpt++].Should().Be("academy", result.JoinToString(" "));
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("wave");
        result[cpt++].Should().Be("portal");
        result[cpt++].Should().Be("mailbox");
        
        cpt.Should().Be(12);
    }
    
    [Test]
    public void Test_4chars_1()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("0000");
        
        result.Length.Should().Be(2);

        var cpt = 0;
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");

        cpt.Should().Be(2);
    }
    
    [Test]
    public void Test_4chars_2()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("0001");
        
        result.Length.Should().Be(2);

        var cpt = 0;
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("acrobat");

        cpt.Should().Be(2);
    }

    [Test]
    public void Test_4chars_3()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();

        var result = safetyWordsComputer.Compute("0101");

        result.Length.Should().Be(2);

        var cpt = 0;
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("exit");

        cpt.Should().Be(2);
    }
    
        
    [Test]
    public void Test_4chars_4()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("1111"); // 4369 = 2 * 1633 + 1103
        
        result.Length.Should().Be(2);

        var cpt = 0;
        result[cpt++].Should().Be("active");
        result[cpt++].Should().Be("farmer");

        cpt.Should().Be(2);
    }
    
    [Test]
    public void Test_4chars_5()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("ffff"); // 65535 = 40*1633 + 215
        
        result.Length.Should().Be(2);

        var cpt = 0;
        result[cpt++].Should().Be("archive", result.JoinToString(" "));
        result[cpt].Should().Be("diesel");
        
        cpt = 0;
        result[cpt++].Should().Be(SafetyWordsValues.AVAILABLE_WORDS[40]);
        result[cpt++].Should().Be(SafetyWordsValues.AVAILABLE_WORDS[215]);

        cpt.Should().Be(2);
    }
    
    [Test]
    public void Test_8chars_1_ffffffff()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("ffffffff"); // 65535 = 40*1633 + 215
        
        result.Length.Should().Be(3);

        var cpt = 0;
        result[cpt++].Should().Be("wave", result.JoinToString(" "));
        result[cpt++].Should().Be("portal");
        result[cpt++].Should().Be("mailbox");

        cpt.Should().Be(3);
    }
    
    [Test]
    public void Test_8chars_2_00000000()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("00000000"); // 0
        
        // result.Length.Should().Be(4);

        var cpt = 0;
        result[cpt++].Should().Be("academy", result.JoinToString(" "));
        result[cpt++].Should().Be("academy");
        result[cpt++].Should().Be("academy");

        cpt.Should().Be(3);
    }
    
    [Test]
    public void Test_sha256_1()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();

        var sha256 = CryptographyUtils.ComputeSHA256FromText("this_is_a_text");
        var result = safetyWordsComputer.Compute(sha256);
        
        result.Length.Should().Be(24);

        var expected =
            "patient sunday john warning invite diagram seminar photo general money nickel diamond cactus rival forward benefit gordon novel transit binary famous peru forum siren";
        var expectedParts = expected.Split(' ');
        
        expectedParts.Length.Should().Be(24);
        for (var i = 0; i < 24; i++)
        {
            result[i].Should().Be(expectedParts[i]);
        }
        
        // int cpt = 0;
        // ClassicAssert.AreEqual(result[cpt++], "galileo", result.JoinToString(" "));
        // ClassicAssert.AreEqual(result[cpt++], "moses");
        // ClassicAssert.AreEqual(result[cpt++], "humor");
        // ClassicAssert.AreEqual(result[cpt++], "master");
        // ClassicAssert.AreEqual(result[cpt++], "garbo");
        // ClassicAssert.AreEqual(result[cpt++], "paul");
        // ClassicAssert.AreEqual(result[cpt++], "singer");
        // ClassicAssert.AreEqual(result[cpt++], "yellow");
        // ClassicAssert.AreEqual(result[cpt++], "mono");
        // ClassicAssert.AreEqual(result[cpt++], "solar");
        // ClassicAssert.AreEqual(result[cpt++], "desert");
        // ClassicAssert.AreEqual(result[cpt++], "ego");
        //
        // ClassicAssert.AreEqual(24, cpt);
    }



    [Test]
    public void Test_BadArgument()
    {
        var safetyWordsComputer = BuildSafetyWordsComputer();

        safetyWordsComputer.Invoking(x => x.Compute("")).Should().Throw<ArgumentOutOfRangeException>();
        safetyWordsComputer.Invoking(x => x.Compute("abcg")).Should().Throw<ArgumentOutOfRangeException>();
        safetyWordsComputer.Invoking(x => x.Compute("abgc")).Should().Throw<ArgumentOutOfRangeException>();
        safetyWordsComputer.Invoking(x => x.Compute("!12a")).Should().Throw<ArgumentOutOfRangeException>();
        
        safetyWordsComputer.Invoking(x => x.Compute("abcd")).Should().NotThrow();
        safetyWordsComputer.Invoking(x => x.Compute("1234")).Should().NotThrow();
        safetyWordsComputer.Invoking(x => x.Compute("12345")).Should().NotThrow();
    }
    
    private SafetyWordsComputer BuildSafetyWordsComputer()
    {
        return new SafetyWordsComputer(SafetyWordsValues.AVAILABLE_WORDS);
    }
}