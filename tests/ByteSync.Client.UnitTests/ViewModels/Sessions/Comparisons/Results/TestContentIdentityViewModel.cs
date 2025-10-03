namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Results;

/*
[TestFixture]
class TestContentIdentityViewModel : AbstractTester
{
    [Test]
    public void TestSignatureHash_1()
    {
        ContentIdentityViewModel contentIdentityViewModel;
        ContentIdentity contentIdentity;
        ContentIdentityCore contentIdentityCore;

        contentIdentityCore = new ContentIdentityCore();
        contentIdentityCore.SignatureHash = "";
        contentIdentityCore.Size = 457;

        contentIdentity = new ContentIdentity(contentIdentityCore);
        contentIdentityViewModel = BuildContentIdentityViewModel(contentIdentity);
        ClassicAssert.AreEqual("", contentIdentityViewModel.SignatureHash);
    }

    [Test]
    public void TestSignatureHash_2()
    {
        ContentIdentityViewModel contentIdentityViewModel;
        ContentIdentity contentIdentity;
        ContentIdentityCore contentIdentityCore;

        contentIdentityCore = new ContentIdentityCore();
        string baseHash = "f1a73e2204a114077f988c9da98d7f8b604ab250496f25aeb3cbd153f5369c83"; // 'Signature' sur https://emn178.github.io/online-tools/sha256.html
        contentIdentityCore.SignatureHash = baseHash.ToUpper() + ".789/457";
        contentIdentityCore.Size = 457;

        contentIdentity = new ContentIdentity(contentIdentityCore);
        contentIdentityViewModel = BuildContentIdentityViewModel(contentIdentity);
        ClassicAssert.AreEqual("F1A73E22...F5369C83.789", contentIdentityViewModel.SignatureHash);
    }

    [Test]
    public void TestSignatureHash_3()
    {
        ContentIdentityViewModel contentIdentityViewModel;
        ContentIdentity contentIdentity;
        ContentIdentityCore contentIdentityCore;

        contentIdentityCore = new ContentIdentityCore();
        contentIdentityCore.SignatureHash = "TestSignature";
        contentIdentityCore.Size = 457;

        contentIdentity = new ContentIdentity(contentIdentityCore);
        contentIdentityViewModel = BuildContentIdentityViewModel(contentIdentity);
        ClassicAssert.AreEqual("TestSignature", contentIdentityViewModel.SignatureHash);
    }

    [Test]
    public void TestSignatureHash_4()
    {
        ContentIdentityViewModel contentIdentityViewModel;
        ContentIdentity contentIdentity;
        ContentIdentityCore contentIdentityCore;

        contentIdentityCore = new ContentIdentityCore();
        string baseHash = "f1a73e2204a114077f988c9da98d7f8b604ab250496f25aeb3cbd153f5369c83"; // 'Signature' sur https://emn178.github.io/online-tools/sha256.html
        contentIdentityCore.SignatureHash = baseHash.ToUpper() + ".789";
        contentIdentityCore.Size = 457;

        contentIdentity = new ContentIdentity(contentIdentityCore);
        contentIdentityViewModel = BuildContentIdentityViewModel(contentIdentity);
        ClassicAssert.AreEqual("F1A73E22...F5369C83.789", contentIdentityViewModel.SignatureHash);
    }

    private ContentIdentityViewModel BuildContentIdentityViewModel(ContentIdentity contentIdentity)
    {
        PathIdentity pathIdentity = new PathIdentity();
        pathIdentity.FileSystemType = FileSystemTypes.File;
        ComparisonItem comparisonItem = new ComparisonItem(pathIdentity);
        comparisonItem.Status = new Status(pathIdentity);
        List<Inventory> inventories = new List<Inventory>();
        ComparisonItemViewModel comparisonItemViewModel = new ComparisonItemViewModel(comparisonItem, inventories, generator.SessionDataHolder.Object);

        var contentIdentityViewModel = new ContentIdentityViewModel(
            comparisonItemViewModel, contentIdentity, null, generator.SessionDataHolder.Object);

        return contentIdentityViewModel;
    }
}*/