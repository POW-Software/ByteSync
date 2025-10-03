using ByteSync.Business.Communications.Downloading;
using ByteSync.TestsCommon;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Communications;

[TestFixture]
public class TestDownloadPartsInfo : AbstractTester
{
    [Test]
    public void Test_GetMergeableParts_Empty()
    {
        var downloadPartsInfo = new DownloadPartsInfo();
        var result = downloadPartsInfo.GetMergeableParts();
        
        result.Count.Should().Be(0);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_Is_Single()
    {
        var downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        
        var result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(1);
        result[0].Should().Be(1);
        
        downloadPartsInfo.SentToMerge.Add(1);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(0);
    }
    
    [Test]
    public void Test_GetMergeableParts_2_Is_Single()
    {
        var downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(2);
        
        var result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(0);
        
        downloadPartsInfo.SentToMerge.Add(1);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(1);
        result[0].Should().Be(2);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_2_3()
    {
        var downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        downloadPartsInfo.DownloadedParts.Add(2);
        downloadPartsInfo.DownloadedParts.Add(3);
        
        var result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(3);
        result[0].Should().Be(1);
        result[1].Should().Be(2);
        result[2].Should().Be(3);
        
        downloadPartsInfo.SentToMerge.Add(1);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(2);
        result[0].Should().Be(2);
        result[1].Should().Be(3);
        
        downloadPartsInfo.SentToMerge.Add(2);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(1);
        result[0].Should().Be(3);
        
        downloadPartsInfo.SentToMerge.Add(3);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(0);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_2_4()
    {
        var downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        downloadPartsInfo.DownloadedParts.Add(2);
        downloadPartsInfo.DownloadedParts.Add(4);
        
        var result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(2);
        result[0].Should().Be(1);
        result[1].Should().Be(2);
        
        downloadPartsInfo.SentToMerge.Add(1);
        downloadPartsInfo.SentToMerge.Add(2);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(0);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_4_5()
    {
        var downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        downloadPartsInfo.DownloadedParts.Add(4);
        downloadPartsInfo.DownloadedParts.Add(5);
        downloadPartsInfo.DownloadedParts.Add(7);
        
        var result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(1);
        result[0].Should().Be(1);
        
        downloadPartsInfo.SentToMerge.Add(1);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(0);
        
        downloadPartsInfo.DownloadedParts.Add(3);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(0);
        
        downloadPartsInfo.DownloadedParts.Add(2);
        
        result = downloadPartsInfo.GetMergeableParts();
        result.Count.Should().Be(4);
        result[0].Should().Be(2);
        result[1].Should().Be(3);
        result[2].Should().Be(4);
        result[3].Should().Be(5);
    }
}