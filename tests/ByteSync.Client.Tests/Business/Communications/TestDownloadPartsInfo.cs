using System.Collections.Generic;
using ByteSync.Business.Communications.Downloading;
using ByteSync.TestsCommon;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.Business.Communications;

[TestFixture]
public class TestDownloadPartsInfo : AbstractTester
{
    [Test]
    public void Test_GetMergeableParts_Empty()
    {
        DownloadPartsInfo downloadPartsInfo = new DownloadPartsInfo();
        List<int> result = downloadPartsInfo.GetMergeableParts();

        ClassicAssert.AreEqual(0, result.Count);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_Is_Single()
    {
        DownloadPartsInfo downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        
        List<int> result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(1, result.Count);
        ClassicAssert.AreEqual(1, result[0]);
        
        downloadPartsInfo.SentToMerge.Add(1);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(0, result.Count);
    }
    
    [Test]
    public void Test_GetMergeableParts_2_Is_Single()
    {
        DownloadPartsInfo downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(2);
        
        List<int> result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(0, result.Count);
        
        downloadPartsInfo.SentToMerge.Add(1);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(1, result.Count);
        ClassicAssert.AreEqual(2, result[0]);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_2_3()
    {
        DownloadPartsInfo downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        downloadPartsInfo.DownloadedParts.Add(2);
        downloadPartsInfo.DownloadedParts.Add(3);
        
        List<int> result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(3, result.Count);
        ClassicAssert.AreEqual(1, result[0]);
        ClassicAssert.AreEqual(2, result[1]);
        ClassicAssert.AreEqual(3, result[2]);
        
        downloadPartsInfo.SentToMerge.Add(1);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(2, result.Count);
        ClassicAssert.AreEqual(2, result[0]);
        ClassicAssert.AreEqual(3, result[1]);
        
        downloadPartsInfo.SentToMerge.Add(2);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(1, result.Count);
        ClassicAssert.AreEqual(3, result[0]);
        
        downloadPartsInfo.SentToMerge.Add(3);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(0, result.Count);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_2_4()
    {
        DownloadPartsInfo downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        downloadPartsInfo.DownloadedParts.Add(2);
        downloadPartsInfo.DownloadedParts.Add(4);
        
        List<int> result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(2, result.Count);
        ClassicAssert.AreEqual(1, result[0]);
        ClassicAssert.AreEqual(2, result[1]);
        
        downloadPartsInfo.SentToMerge.Add(1);
        downloadPartsInfo.SentToMerge.Add(2);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(0, result.Count);
    }
    
    [Test]
    public void Test_GetMergeableParts_1_4_5()
    {
        DownloadPartsInfo downloadPartsInfo = new DownloadPartsInfo();
        downloadPartsInfo.DownloadedParts.Add(1);
        downloadPartsInfo.DownloadedParts.Add(4);
        downloadPartsInfo.DownloadedParts.Add(5);
        downloadPartsInfo.DownloadedParts.Add(7);
        
        List<int> result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(1, result.Count);
        ClassicAssert.AreEqual(1, result[0]);
        
        downloadPartsInfo.SentToMerge.Add(1);

        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(0, result.Count);
        
        downloadPartsInfo.DownloadedParts.Add(3);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(0, result.Count);
        
        downloadPartsInfo.DownloadedParts.Add(2);
        
        result = downloadPartsInfo.GetMergeableParts();
        ClassicAssert.AreEqual(4, result.Count);
        ClassicAssert.AreEqual(2, result[0]);
        ClassicAssert.AreEqual(3, result[1]);
        ClassicAssert.AreEqual(4, result[2]);
        ClassicAssert.AreEqual(5, result[3]);
    }
}