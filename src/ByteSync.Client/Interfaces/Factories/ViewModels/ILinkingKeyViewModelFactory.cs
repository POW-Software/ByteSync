using ByteSync.Business.Sessions;
using ByteSync.ViewModels.Sessions.Cloud.Managing;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface ILinkingKeyViewModelFactory
{
    LinkingKeyViewModel CreateLinkingKeyViewModel(LinkingKeys linkingKey);
}