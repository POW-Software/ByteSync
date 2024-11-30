namespace ByteSync.Business.Comparisons
{
    public enum ConditionOperatorTypes
    {
        Equals = 1,
        NotEquals = 2,
        ExistsOn = 3,
        NotExistsOn = 4,
        //ExistsOnlyOnSpecificData,
        IsOlderThan = 5,
        IsNewerThan = 6,
        IsBiggerThan = 7,
        IsSmallerThan = 8,
        IsEmpty = 9,
        IsNotEmpty = 10,
    }
}