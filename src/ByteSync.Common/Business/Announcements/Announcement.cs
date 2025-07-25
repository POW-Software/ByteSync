using System;
using System.Collections.Generic;

namespace ByteSync.Common.Business.Announcements;

public class Announcement
{
    public string Id { get; set; }
    
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Dictionary<string, string> Message { get; set; } = new();
}
