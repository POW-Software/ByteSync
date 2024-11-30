using System;
using System.Collections.Generic;
using System.Linq;

namespace ByteSync.Common.Business.Profiles;

public class CloudSessionProfileData
{
    public CloudSessionProfileData()
    {
        Slots = new List<CloudSessionProfileSlot>();
    }
    
    public string CloudSessionProfileId { get; set; }
    

    
    public List<CloudSessionProfileSlot> Slots { get; set; }

    public List<string> ProfileClientIds
    {
        get
        {
            return Slots.Select(x => x.ProfileClientId).ToList();
        }
    }

    public string ProfileDetailsPassword { get; set; }
    
    public DateTime CreationDateTime { get; set; }
    
    public string RequesterProfileClientId { get; set; }
    
    public string? CurrentLobbyId { get; set; }
}