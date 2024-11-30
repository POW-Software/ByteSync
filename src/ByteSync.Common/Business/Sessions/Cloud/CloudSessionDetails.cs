using System.Collections.Generic;

namespace ByteSync.Common.Business.Sessions.Cloud;

public class CloudSessionDetails
{
    public CloudSession CloudSession { get; set; }
        
    public EncryptedSessionSettings SessionSettings { get; set; }
    
    public List<SessionMemberInfoDTO> Members { get; set; }
    
    public bool IsActivated { get; set; }
}