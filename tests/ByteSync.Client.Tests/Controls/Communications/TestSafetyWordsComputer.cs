using ByteSync.Common.Helpers;
using ByteSync.Services.Communications;
using ByteSync.TestsCommon;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.Controls.Communications;

[TestFixture]
public class TestSafetyWordsComputer : AbstractTester
{
    [Test]
    public void Test_md5_1()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();

        string md5 = CryptographyUtils.ComputeMD5FromText("this_is_a_text");
        var result = safetyWordsComputer.Compute(md5);
        
        ClassicAssert.AreEqual(12, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "sultan", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt++], "radio");
        ClassicAssert.AreEqual(result[cpt++], "inside");
        ClassicAssert.AreEqual(result[cpt++], "arcade");
        ClassicAssert.AreEqual(result[cpt++], "small");
        ClassicAssert.AreEqual(result[cpt++], "benny");
        ClassicAssert.AreEqual(result[cpt++], "mike");
        ClassicAssert.AreEqual(result[cpt++], "shrink");
        ClassicAssert.AreEqual(result[cpt++], "program");
        ClassicAssert.AreEqual(result[cpt++], "program");
        ClassicAssert.AreEqual(result[cpt++], "galaxy");
        ClassicAssert.AreEqual(result[cpt++], "vibrate");
        
        ClassicAssert.AreEqual(12, cpt);
    }

    [Test]
    public void Test_md5_2()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();

        string md5 = new string('0', 32);
        var result = safetyWordsComputer.Compute(md5);
        
        ClassicAssert.AreEqual(12, result.Length);

        for (int i = 0; i < 12; i++)
        {
            ClassicAssert.AreEqual(result[i], "academy");
        }
    }
    
    [Test]
    public void Test_md5_3()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();

        string md5 = new string('f', 32);
        var result = safetyWordsComputer.Compute(md5);
        
        ClassicAssert.AreEqual(12, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "sincere", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt++], "john");
        ClassicAssert.AreEqual(result[cpt++], "quiet");
        ClassicAssert.AreEqual(result[cpt++], "lorenzo");
        ClassicAssert.AreEqual(result[cpt++], "jamaica");
        ClassicAssert.AreEqual(result[cpt++], "montana");
        ClassicAssert.AreEqual(result[cpt++], "gondola");
        ClassicAssert.AreEqual(result[cpt++], "company");
        ClassicAssert.AreEqual(result[cpt++], "ricardo");
        ClassicAssert.AreEqual(result[cpt++], "forever");
        ClassicAssert.AreEqual(result[cpt++], "silicon");
        ClassicAssert.AreEqual(result[cpt++], "edition");
        
        ClassicAssert.AreEqual(12, cpt);
    }
    
    [Test]
    public void Test_md5_4()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();

        string md5 = new string('f', 30);
        md5 = "00" + md5;
        var result = safetyWordsComputer.Compute(md5);
        
        ClassicAssert.AreEqual(12, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "adrian", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt++], "balance");
        ClassicAssert.AreEqual(result[cpt++], "garbo");
        ClassicAssert.AreEqual(result[cpt++], "speech");
        ClassicAssert.AreEqual(result[cpt++], "exodus");
        ClassicAssert.AreEqual(result[cpt++], "tunnel");
        ClassicAssert.AreEqual(result[cpt++], "escort");
        ClassicAssert.AreEqual(result[cpt++], "proxy");
        ClassicAssert.AreEqual(result[cpt++], "frog");
        ClassicAssert.AreEqual(result[cpt++], "initial");
        ClassicAssert.AreEqual(result[cpt++], "beach");
        ClassicAssert.AreEqual(result[cpt++], "venice");
        
        ClassicAssert.AreEqual(12, cpt);
    }
    
    [Test]
    public void Test_md5_5()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        string md5 = new string('0', 16) + new string('f', 16);
        var result = safetyWordsComputer.Compute(md5);
        
        ClassicAssert.AreEqual(12, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "academy", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "triton");
        ClassicAssert.AreEqual(result[cpt++], "bazaar");
        ClassicAssert.AreEqual(result[cpt++], "sailor");
        ClassicAssert.AreEqual(result[cpt++], "episode");
        ClassicAssert.AreEqual(result[cpt++], "equal");
        ClassicAssert.AreEqual(result[cpt++], "prime");
        
        ClassicAssert.AreEqual(12, cpt);
    }
    
    [Test]
    public void Test_md5_6()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        string md5 = new string('0', 24) + new string('f', 8);
        var result = safetyWordsComputer.Compute(md5);
        
        ClassicAssert.AreEqual(12, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "academy", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "wave");
        ClassicAssert.AreEqual(result[cpt++], "portal");
        ClassicAssert.AreEqual(result[cpt++], "mailbox");
        
        ClassicAssert.AreEqual(12, cpt);
    }
    
    [Test]
    public void Test_4chars_1()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("0000");
        
        ClassicAssert.AreEqual(2, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");

        ClassicAssert.AreEqual(2, cpt);
    }
    
    [Test]
    public void Test_4chars_2()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("0001");
        
        ClassicAssert.AreEqual(2, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "acrobat");

        ClassicAssert.AreEqual(2, cpt);
    }

    [Test]
    public void Test_4chars_3()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();

        var result = safetyWordsComputer.Compute("0101");

        ClassicAssert.AreEqual(2, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "exit");

        ClassicAssert.AreEqual(2, cpt);
    }
    
        
    [Test]
    public void Test_4chars_4()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("1111"); // 4369 = 2 * 1633 + 1103
        
        ClassicAssert.AreEqual(2, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "active");
        ClassicAssert.AreEqual(result[cpt++], "farmer");

        ClassicAssert.AreEqual(2, cpt);
    }
    
    [Test]
    public void Test_4chars_5()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("ffff"); // 65535 = 40*1633 + 215
        
        ClassicAssert.AreEqual(2, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "archive", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt], "diesel");
        
        cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], SafetyWordsValues.AVAILABLE_WORDS[40]);
        ClassicAssert.AreEqual(result[cpt++], SafetyWordsValues.AVAILABLE_WORDS[215]);

        ClassicAssert.AreEqual(2, cpt);
    }
    
    [Test]
    public void Test_8chars_1_ffffffff()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("ffffffff"); // 65535 = 40*1633 + 215
        
        ClassicAssert.AreEqual(3, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "wave", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt++], "portal");
        ClassicAssert.AreEqual(result[cpt++], "mailbox");

        ClassicAssert.AreEqual(3, cpt);
    }
    
    [Test]
    public void Test_8chars_2_00000000()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();
        
        var result = safetyWordsComputer.Compute("00000000"); // 0
        
        // ClassicAssert.AreEqual(4, result.Length);

        int cpt = 0;
        ClassicAssert.AreEqual(result[cpt++], "academy", result.JoinToString(" "));
        ClassicAssert.AreEqual(result[cpt++], "academy");
        ClassicAssert.AreEqual(result[cpt++], "academy");

        ClassicAssert.AreEqual(3, cpt);
    }
    
    [Test]
    public void Test_sha256_1()
    {
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();

        string sha256 = CryptographyUtils.ComputeSHA256FromText("this_is_a_text");
        var result = safetyWordsComputer.Compute(sha256);
        
        ClassicAssert.AreEqual(24, result.Length);

        string expected =
            "patient sunday john warning invite diagram seminar photo general money nickel diamond cactus rival forward benefit gordon novel transit binary famous peru forum siren";
        var expectedParts = expected.Split(' ');
        
        ClassicAssert.AreEqual(24, expectedParts.Length);
        for (int i = 0; i < 24; i++)
        {
            ClassicAssert.AreEqual(result[i], expectedParts[i]);
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
        SafetyWordsComputer safetyWordsComputer = BuildSafetyWordsComputer();

        ClassicAssert.Throws<ArgumentOutOfRangeException>(() => safetyWordsComputer.Compute(""));
        ClassicAssert.Throws<ArgumentOutOfRangeException>(() => safetyWordsComputer.Compute("abcg"));
        ClassicAssert.Throws<ArgumentOutOfRangeException>(() => safetyWordsComputer.Compute("abgc"));
        ClassicAssert.Throws<ArgumentOutOfRangeException>(() => safetyWordsComputer.Compute("!12a"));
        
        ClassicAssert.DoesNotThrow(() => safetyWordsComputer.Compute("abcd"));
        ClassicAssert.DoesNotThrow(() => safetyWordsComputer.Compute("1234"));
        ClassicAssert.DoesNotThrow(() => safetyWordsComputer.Compute("12345"));
    }
    
    private SafetyWordsComputer BuildSafetyWordsComputer()
    {
        return new SafetyWordsComputer(SafetyWordsValues.AVAILABLE_WORDS);
    }
}