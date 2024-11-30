using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Helpers;

public static class LogHelper
{
    public static object BuildLog(this SharedFileDefinition sharedFileDefinition)
    {
        if (sharedFileDefinition == null)
        {
            return null;
        }

        return new
        {
            Guid = sharedFileDefinition.Id, 
            SharedFileType = sharedFileDefinition.SharedFileType,
            SharerId = sharedFileDefinition.ClientInstanceId
        };
    }

    public static object BuildLog(this CloudSession? cloudSession)
    {
        if (cloudSession == null)
        {
            return null;
        }

        return new
        {
            SessionId = cloudSession.SessionId,
            Created = cloudSession.Created
        };
    }

    public static object BuildLog(this CloudSessionData cloudSessionData)
    {
        return new
        {
            SessionId = cloudSessionData.SessionId,
        };
    }

    public static object BuildLog(this Client client)
    {
        if (client == null)
        {
            return null;
        }

        return new
        {
            CIId = client.ClientInstanceId,
            CId = client.ClientId,
            IP = client.IpAddress,
        };
    }

    public static object BuildLog(this SessionMemberData sessionMemberData)
    {
        if (sessionMemberData == null)
        {
            return null;
        }

        return new
        {
            FId = sessionMemberData.ClientInstanceId,
        };
    }

    public static object BuildLog(this ByteSyncEndpoint endpoint)
    {
        if (endpoint == null)
        {
            return null;
        }

        return new
        {
            CId = endpoint.ClientId,
            CIId = endpoint.ClientInstanceId,
        };
    }

    public static object BuildLog(this EncryptedPathItem sharedPathItem)
    {
        if (sharedPathItem == null)
        {
            return null;
        }

        return new
        {
            Code = sharedPathItem.Code,
        };
    }

    // public static object BuildLog(this ProductSerial productSerial)
    // {
    //     if (productSerial == null)
    //     {
    //         return null;
    //     }
    //
    //     return new
    //     {
    //         SerialNumber = productSerial.SerialNumber,
    //         Email = productSerial.Email,
    //         Id = productSerial.Id,
    //     };
    // }
        
    public static object BuildLog(this Lobby lobby)
    {
        if (lobby == null)
        {
            return null;
        }

        return new
        {
            LId = lobby.LobbyId,
            CSPId = lobby.CloudSessionProfileId,
            Members = lobby.ConnectedLobbyMembers.Count,
        };
    }
}