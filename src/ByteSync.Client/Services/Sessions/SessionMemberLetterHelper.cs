using ByteSync.Business.SessionMembers;

namespace ByteSync.Services.Sessions;

public static class SessionMemberLetterHelper
{
    public static string GetLetter(this SessionMemberInfo sessionMemberInfo)
    {
        return ((char) ('A' + sessionMemberInfo.PositionInList)).ToString();
    }
}