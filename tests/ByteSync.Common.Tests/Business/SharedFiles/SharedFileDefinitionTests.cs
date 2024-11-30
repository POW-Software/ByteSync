using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.SharedFiles;
using NUnit.Framework;

namespace TestingCommon.Business.SharedFiles;

[TestFixture]
public class SharedFileDefinitionTests
{
    [Test]
    public void TestFileNameForBaseInventory()
    {
        var sharedFileDefinition = new SharedFileDefinition
        {
            AdditionalName = "additionalName",
            SharedFileType = SharedFileTypes.BaseInventory
        };

        var fileName = sharedFileDefinition.GetFileName(1);

        Assert.That(fileName, Is.EqualTo("base_inventory_additionalName.part1"));
    }

    [Test]
    public void TestFileNameForFullInventory()
    {
        var sharedFileDefinition = new SharedFileDefinition
        {
            AdditionalName = "additionalName",
            SharedFileType = SharedFileTypes.FullInventory
        };

        var fileName = sharedFileDefinition.GetFileName(1);

        Assert.That(fileName, Is.EqualTo("full_inventory_additionalName.part1"));
    }

    [Test]
    public void TestFileNameForSynchronization()
    {
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "someId",
            SharedFileType = SharedFileTypes.FullSynchronization
        };

        var fileName = sharedFileDefinition.GetFileName(1);

        Assert.That(fileName, Is.EqualTo("synchronization_someId.part1"));
    }

    [Test]
    public void TestFileNameForSynchronizationStartData()
    {
        var sharedFileDefinition = new SharedFileDefinition
        {
            SharedFileType = SharedFileTypes.SynchronizationStartData
        };

        var fileName = sharedFileDefinition.GetFileName(1);

        Assert.That(fileName, Is.EqualTo("synchronization_start_data.part1"));
    }

    [Test]
    public void TestFileNameForProfileDetails()
    {
        var sharedFileDefinition = new SharedFileDefinition
        {
            AdditionalName = "additionalName",
            SharedFileType = SharedFileTypes.ProfileDetails
        };

        var fileName = sharedFileDefinition.GetFileName(1);

        Assert.That(fileName, Is.EqualTo("profile_details_additionalName.part1"));
    }

    [Test]
    public void TestEquals()
    {
        var sharedFileDefinition1 = new SharedFileDefinition
        {
            SessionId = "sessionId1",
            ClientInstanceId = "clientInstanceId1",
            SharedFileType = SharedFileTypes.BaseInventory,
            Id = "id1"
        };
        var sharedFileDefinition2 = new SharedFileDefinition
        {
            SessionId = "sessionId1",
            ClientInstanceId = "clientInstanceId1",
            SharedFileType = SharedFileTypes.BaseInventory,
            Id = "id1"
        };
        var sharedFileDefinition3 = new SharedFileDefinition
        {
            SessionId = "sessionId1",
            ClientInstanceId = "clientInstanceId1",
            SharedFileType = SharedFileTypes.BaseInventory,
            Id = "id2"
        };

        Assert.That(sharedFileDefinition1.Equals(sharedFileDefinition2), Is.True);
        Assert.That(sharedFileDefinition1.Equals(sharedFileDefinition3), Is.False);
    }

    [Test]
    public void TestIsCreatedBy()
    {
        var sharedFileDefinition = new SharedFileDefinition
        {
            ClientInstanceId = "clientId1_clientInstanceId1"
        };
        var endpoint1 = new ByteSyncEndpoint
        {
            ClientId = "clientId1",
            ClientInstanceId = "clientId1_clientInstanceId1"
        };
        var endpoint2 = new ByteSyncEndpoint
        {
            ClientId = "clientId2",
            ClientInstanceId = "clientId2_clientInstanceId2"
        };

        Assert.That(sharedFileDefinition.IsCreatedBy(endpoint1), Is.True);
        Assert.That(sharedFileDefinition.IsCreatedBy(endpoint2), Is.False);
    }
}