using ByteSync.Business.SessionMembers;

namespace ByteSync.Services.Sessions;

public static class SessionMemberLetterHelper
{
    public static string GetLetter(this SessionMember sessionMember)
    {
        return ((char) ('A' + sessionMember.PositionInList)).ToString();
    }
}