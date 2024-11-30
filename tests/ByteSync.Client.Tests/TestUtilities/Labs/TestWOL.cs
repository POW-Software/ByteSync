using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ByteSync.Tests.TestUtilities.Labs;

[TestFixture]
public class TestWOL
{
    [Test]
    public void TestWOL_1()
    {
        string macAdress = WOL.GetMacAddress("192.168.1.8");
    }   
    
    [Test]
    public async Task TestWOL_2()
    {
        await WOL.WakeOnLan("00-08-9b-df-4f-b8");
    }   
    
    [Test]
    public async Task TestWOL_3()
    {
        var directoryInfo = new DirectoryInfo("\\192.168.1.8");
    }  
}