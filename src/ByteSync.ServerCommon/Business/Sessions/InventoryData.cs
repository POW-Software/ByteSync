﻿namespace ByteSync.ServerCommon.Business.Sessions;

public class InventoryData
{
    public InventoryData()
    {
        InventoryMembers = new List<InventoryMemberData>();
    }

    public InventoryData(string sessionId) : this()
    {
        SessionId = sessionId;
    }

    public string SessionId { get; set; } = null!;
    
    public List<InventoryMemberData> InventoryMembers { get; set; }
    
    public bool IsInventoryStarted { get; set; }
    
    public void RecodePathItems(CloudSessionData cloudSessionData)
    {
        foreach (var inventoryMemberData in InventoryMembers)
        {
            int position = cloudSessionData.SessionMembers.FindIndex(m => m.ClientInstanceId == inventoryMemberData.ClientInstanceId);
            
            string letter = ((char)('A' + position)).ToString();

            int cpt = 1;
            foreach (var pathItem in inventoryMemberData.SharedPathItems)
            {
                pathItem.Code = letter + cpt;

                cpt += 1;
            }
        }
    }
}