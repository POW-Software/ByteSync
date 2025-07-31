using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Inventories;

public enum StartInventoryStatuses
{
    InventoryStartedSucessfully = 1,
    MoreThan5Members = 2,
    LessThan2Members = 3,
    LessThan2MembersWithDataToSynchronize = 4,
    UndefinedSettings = 5,
    SessionNotFound = 6,
    UndefinedSession = 7,
    AtLeastOneDataNodeWithNoDataSource = 8,
    LessThan2DataSources = 9,
    MoreThan5DataSources = 10,
    MoreThan5DataNodes = 11,
    LessThan2DataNodes = 12,
    Unknown_13 = 13,
    Unknown_14 = 14,
    UnknownError = 30
}


public class StartInventoryResult
{
    public StartInventoryStatuses Status { get; set; }
    
    public bool IsOK
    {
        get
        {
            return Status.In(StartInventoryStatuses.InventoryStartedSucessfully);
        }
    }
    
    public static StartInventoryResult BuildFrom(StartInventoryStatuses status)
    {
        StartInventoryResult startInventoryResult = new StartInventoryResult();

        startInventoryResult.Status = status;

        return startInventoryResult;
    }
    
    public static StartInventoryResult BuildOK()
    {
        StartInventoryResult startInventoryResult = new StartInventoryResult();

        startInventoryResult.Status = StartInventoryStatuses.InventoryStartedSucessfully;

        return startInventoryResult;
    }
}