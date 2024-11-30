using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.Profiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class CloudSessionProfileService : ICloudSessionProfileService
{
    private readonly ICloudSessionProfileRepository _cloudSessionProfileRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ILogger<CloudSessionProfileService> _logger;


    public CloudSessionProfileService(ICloudSessionProfileRepository cloudSessionProfileRepository, ICloudSessionsRepository cloudSessionsRepository,
        ILogger<CloudSessionProfileService> logger)
    {
        _cloudSessionProfileRepository = cloudSessionProfileRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _logger = logger;
        
        SyncRoot = new object();
    }

    public object SyncRoot { get; set; }

    public async Task<CreateCloudSessionProfileResult> CreateCloudSessionProfile(string sessionId, Client client)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(sessionId, client.ClientInstanceId);

        if (sessionMemberData != null)
        {
            CloudSessionProfileEntity cloudSessionProfileEntity = new CloudSessionProfileEntity();
            cloudSessionProfileEntity.CloudSessionProfileId = $"CSPID_{Guid.NewGuid()}";

            CloudSessionProfileData f;
            
            foreach (var sessionMember in sessionMemberData.CloudSessionData.SessionMembers)
            {
                string profileClientId = $"CSPCID_{Guid.NewGuid()}";

                var slot = new CloudSessionProfileSlot
                {
                    ProfileClientId = profileClientId,
                    ClientId = sessionMember.ClientId
                };
                
                cloudSessionProfileEntity.Slots.Add(slot);
            }

            cloudSessionProfileEntity.ProfileDetailsPassword = Guid.NewGuid().ToString("N");

            await _cloudSessionProfileRepository.Save(cloudSessionProfileEntity.CloudSessionProfileId, cloudSessionProfileEntity);
            
            var cloudSessionProfileCreationData =
                BuildCloudSessionProfileCreationData(sessionMemberData, cloudSessionProfileEntity);

            var createCloudSessionProfileResult = CreateCloudSessionProfileResult.BuildFrom(cloudSessionProfileCreationData);

            return createCloudSessionProfileResult;
        }
        else
        {
            var createCloudSessionProfileResult = CreateCloudSessionProfileResult.BuildFrom(CreateCloudSessionProfileStatuses.ServerError);
            
            return createCloudSessionProfileResult;
        }
    }

    public async Task<CloudSessionProfileData?> GetCloudSessionProfileData(GetCloudSessionProfileDataParameters getCloudSessionProfileDataParameters,
        Client client)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(getCloudSessionProfileDataParameters.SessionId, client.ClientInstanceId);

        if (sessionMemberData != null)
        {
            var cloudSessionProfileEntity = await _cloudSessionProfileRepository.Get(getCloudSessionProfileDataParameters.CloudSessionProfileId);

            if (cloudSessionProfileEntity != null && cloudSessionProfileEntity.Slots.Any(s => s.ClientId == sessionMemberData.ClientId))
            {
                var result = BuildCloudSessionProfileCreationData(sessionMemberData, cloudSessionProfileEntity);

                return result;
            }
        }

        return null;
    }

    public async Task<string?> GetProfileDetailsPassword(GetProfileDetailsPasswordParameters parameters, Client client)
    {
        var cloudSessionProfile = await _cloudSessionProfileRepository.Get(parameters.CloudSessionProfileId);

        if (cloudSessionProfile != null && cloudSessionProfile.Slots.Any(s => s.ProfileClientId == parameters.ProfileClientId))
        {
            _logger.LogInformation("GetProfileDetailsPassword: result OK for Client:{ClientInstanceId} with CloudSessionProfileId:{CloudSessionProfileId}",
                client.ClientInstanceId, parameters.CloudSessionProfileId);
            return cloudSessionProfile.ProfileDetailsPassword;
        }
        else
        {
            _logger.LogWarning("GetProfileDetailsPassword: not found for Client:{ClientInstanceId} with CloudSessionProfileId:{CloudSessionProfileId}",
                client.ClientInstanceId, parameters.CloudSessionProfileId);
            return null;
        }
    }

    public async Task<bool> DeleteCloudSessionProfile(DeleteCloudSessionProfileParameters parameters, Client byteSyncEndpoint)
    {
        await _cloudSessionProfileRepository.Delete(parameters.CloudSessionProfileId);

        return true;
    }

    private CloudSessionProfileData BuildCloudSessionProfileCreationData(SessionMemberData sessionMemberData, CloudSessionProfileEntity cloudSessionProfileEntity)
    {
        var cloudSessionProfileCreationData = new CloudSessionProfileData();
        
        cloudSessionProfileCreationData.CloudSessionProfileId = cloudSessionProfileEntity.CloudSessionProfileId;
        cloudSessionProfileCreationData.Slots = cloudSessionProfileEntity.Slots;
        cloudSessionProfileCreationData.ProfileDetailsPassword = cloudSessionProfileEntity.ProfileDetailsPassword;
        cloudSessionProfileCreationData.CreationDateTime = cloudSessionProfileEntity.CreationDateTime;
        cloudSessionProfileCreationData.RequesterProfileClientId =
            cloudSessionProfileEntity.Slots.Single(s => s.ClientId == sessionMemberData.ClientId).ProfileClientId;

        return cloudSessionProfileCreationData;
    }
}