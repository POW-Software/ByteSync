﻿using ByteSync.Common.Business.Lobbies;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ILobbyService
{
    Task<LobbyMemberInfo?> UpdateLobbyMemberStatus(string lobbyId, Client client, LobbyMemberStatuses lobbyMemberStatus);

    Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials lobbyCloudSessionCredentials, Client client); 
    
    Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo, Client client);
}