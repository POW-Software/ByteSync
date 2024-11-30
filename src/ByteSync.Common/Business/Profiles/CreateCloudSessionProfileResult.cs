using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Profiles;

public enum CreateCloudSessionProfileStatuses
{
    Success = 1,
    ServerError = 2,
}

public class CreateCloudSessionProfileResult
{
    public CreateCloudSessionProfileStatuses Status { get; set; }

    public bool IsOK
    {
        get
        {
            return Status.In(CreateCloudSessionProfileStatuses.Success);
        }
    }
    
    public CloudSessionProfileData? Data { get; set; }
    
    public static CreateCloudSessionProfileResult BuildFrom(CloudSessionProfileData cloudSessionProfileData)
    {
        CreateCloudSessionProfileResult createCloudSessionProfileResult = new CreateCloudSessionProfileResult();

        createCloudSessionProfileResult.Status = CreateCloudSessionProfileStatuses.Success;
        createCloudSessionProfileResult.Data = cloudSessionProfileData;

        return createCloudSessionProfileResult;
    }

    public static CreateCloudSessionProfileResult BuildFrom(CreateCloudSessionProfileStatuses status)
    {
        CreateCloudSessionProfileResult createCloudSessionProfileResult = new CreateCloudSessionProfileResult();

        createCloudSessionProfileResult.Status = status;

        return createCloudSessionProfileResult;
    }
}