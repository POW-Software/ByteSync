using ByteSync.Business.Actions.Loose;
using ByteSync.ViewModels.Lobbies;
using CloudSessionProfileDetails = ByteSync.Business.Profiles.CloudSessionProfileDetails;

namespace ByteSync.Interfaces.Lobbies;

public interface ILobbySynchronizationRuleViewModelFactory
{
    LobbySynchronizationRuleViewModel Create(LooseSynchronizationRule synchronizationRule, CloudSessionProfileDetails profileDetails);
    
    LobbySynchronizationRuleViewModel Create(LooseSynchronizationRule synchronizationRule, bool isVisible);
}