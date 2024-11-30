namespace ByteSync.Business.Comparisons;

internal class StatusGroup
{
    public StatusGroup(StatusGroupMember firstMember)
    {
        Members = new List<StatusGroupMember>();

        Members.Add(firstMember);
    }

    public List<StatusGroupMember> Members { get; set; }

    public object Link
    {
        get
        {
            if (Members.Count == 0)
            {
                return null;
            }
            else
            {
                return Members.Select(m => m.Link).ToList().ToHashSet().Single();
            }
        }
    }

    public string MinimalLetter
    {
        get
        {
            if (Members.Count == 0)
            {
                return null;
            }
            else
            {
                var letters = Members.Select(m => m.Letter).ToList();

                letters.Sort();

                return letters.First();
            }
        }
    }

    public bool IsMissing
    {
        get
        {
            return Members.Count == 1 && Members.All(m => m.IsMissing);
        }
    }
}