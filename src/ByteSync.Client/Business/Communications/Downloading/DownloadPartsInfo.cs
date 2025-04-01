namespace ByteSync.Business.Communications.Downloading;

public class DownloadPartsInfo
{
    public DownloadPartsInfo()
    {
        AvailableParts = new List<int>();
        SentToDownload = new List<int>();
        DownloadedParts = new List<int>();
        SentToMerge = new List<int>();
        MergedParts = new List<int>();
    }

    public List<int> AvailableParts { get; }
        
    public List<int> SentToDownload { get; }
        
    public List<int> DownloadedParts { get; }
        
    public List<int> SentToMerge { get; }
        
    public List<int> MergedParts { get;  }

    public List<int> GetMergeableParts()
    {
        return GetNextElements(SentToMerge, DownloadedParts);
    }

    public List<int> GetDownloadableParts()
    {
        return GetNextElements(SentToDownload, AvailableParts);
    }

    private static List<int> GetNextElements(List<int> sentToList, List<int> availableList)
    {
        // On check tout d'abord que la liste des GivenToMerger est consécutive
            
        // https://stackoverflow.com/questions/13359327/check-if-listint32-values-are-consecutive
        bool isConsecutive = !sentToList.Select((i,j) => i-j).Distinct().Skip(1).Any();
        if (!isConsecutive)
        {
            throw new Exception("GivenToMerger data is bad");
        }
            
        List<int> partsToGiveToMerger = new List<int>();
        
        int alreadyGivenToMerger = sentToList.Count;

        int index = alreadyGivenToMerger + 1;
        bool canGive = true;
        while (canGive)
        {
            if (availableList.Contains(index))
            {
                partsToGiveToMerger.Add(index);

                index += 1;
            }
            else
            {
                canGive = false;
            }
        }

        return partsToGiveToMerger;
    }

    public void Clear()
    {
        AvailableParts.Clear();
        SentToDownload.Clear();
        DownloadedParts.Clear();
        SentToMerge.Clear();
        MergedParts.Clear();
    }
}